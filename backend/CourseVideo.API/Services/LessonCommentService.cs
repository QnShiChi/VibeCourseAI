using System.Text;
using System.Text.Json;
using CourseVideo.API.DTOs.Comments;
using CourseVideo.API.Models;
using CourseVideo.API.Repositories.Interfaces;
using CourseVideo.API.Services.Interfaces;

namespace CourseVideo.API.Services;

public class LessonCommentService : ILessonCommentService
{
    private readonly ILessonCommentRepository _lessonCommentRepository;
    private readonly ILessonRepository _lessonRepository;
    private readonly IUserRepository _userRepository;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public LessonCommentService(
        ILessonCommentRepository lessonCommentRepository,
        ILessonRepository lessonRepository,
        IUserRepository userRepository,
        IServiceScopeFactory serviceScopeFactory)
    {
        _lessonCommentRepository = lessonCommentRepository;
        _lessonRepository = lessonRepository;
        _userRepository = userRepository;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<LessonCommentListResponse> GetCommentsAsync(
        Guid lessonId,
        Guid currentUserId,
        bool isAdmin,
        string sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await RequireAccessibleLessonAsync(lessonId, isAdmin);

        var normalizedSort = NormalizeSort(sort);
        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedPageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 50);

        var rootComments = await _lessonCommentRepository.GetRootCommentsByLessonIdAsync(lessonId, true, cancellationToken);
        var replyLookup = await LoadRepliesByParentIdAsync(rootComments.Select(comment => comment.Id).ToArray(), cancellationToken);

        var orderedRootComments = normalizedSort == "featured"
            ? rootComments
                .OrderByDescending(comment => CalculateFeaturedScore(comment, replyLookup))
                .ThenByDescending(comment => comment.CreatedAt)
            : rootComments
                .OrderByDescending(comment => comment.CreatedAt);

        var totalCount = orderedRootComments.Count();
        var pagedRootComments = orderedRootComments
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToList();

        var items = pagedRootComments
            .Select(comment => MapThread(comment, replyLookup, currentUserId, isAdmin))
            .ToList();

        return new LessonCommentListResponse
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = totalCount,
            HasMore = normalizedPage * normalizedPageSize < totalCount,
            Sort = normalizedSort
        };
    }

    public async Task<LessonCommentThreadResponse> CreateCommentAsync(
        Guid lessonId,
        Guid currentUserId,
        bool isAdmin,
        CreateLessonCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var lesson = await RequireAccessibleLessonAsync(lessonId, isAdmin);
        var content = NormalizeContent(request.Content);
        var currentUser = await RequireUserAsync(currentUserId);

        var comment = new LessonComment
        {
            LessonId = lesson.Id,
            UserId = currentUserId,
            Content = content
        };

        await _lessonCommentRepository.AddAsync(comment, cancellationToken);
        await _lessonCommentRepository.SaveChangesAsync(cancellationToken);

        TriggerSentimentAnalysis(comment.Id);

        comment.User = currentUser;
        return new LessonCommentThreadResponse
        {
            Comment = MapComment(comment, currentUserId, isAdmin),
            Replies = []
        };
    }

    public async Task<LessonCommentThreadResponse> CreateReplyAsync(
        Guid lessonId,
        Guid commentId,
        Guid currentUserId,
        bool isAdmin,
        CreateLessonReplyRequest request,
        CancellationToken cancellationToken = default)
    {
        await RequireAccessibleLessonAsync(lessonId, isAdmin);
        var currentUser = await RequireUserAsync(currentUserId);
        var targetComment = await RequireCommentAsync(commentId, lessonId, cancellationToken);
        var rootComment = targetComment.ParentCommentId.HasValue
            ? await RequireCommentAsync(targetComment.ParentCommentId.Value, lessonId, cancellationToken)
            : targetComment;

        var replyToUserId = targetComment.UserId;
        var content = NormalizeContent(request.Content);

        var reply = new LessonComment
        {
            LessonId = lessonId,
            UserId = currentUserId,
            ParentCommentId = rootComment.Id,
            ReplyToUserId = replyToUserId,
            Content = content
        };

        await _lessonCommentRepository.AddAsync(reply, cancellationToken);
        await _lessonCommentRepository.SaveChangesAsync(cancellationToken);

        TriggerSentimentAnalysis(reply.Id);

        reply.User = currentUser;
        if (replyToUserId != Guid.Empty)
        {
            reply.ReplyToUser = await _userRepository.GetByIdAsync(replyToUserId);
        }

        var replyLookup = new Dictionary<Guid, IReadOnlyList<LessonComment>>
        {
            [rootComment.Id] = [reply]
        };

        return new LessonCommentThreadResponse
        {
            Comment = MapComment(rootComment, currentUserId, isAdmin),
            Replies = replyLookup[rootComment.Id].Select(item => MapComment(item, currentUserId, isAdmin)).ToList()
        };
    }

    public async Task AddReactionAsync(
        Guid lessonId,
        Guid commentId,
        Guid currentUserId,
        bool isAdmin,
        string emoji,
        CancellationToken cancellationToken = default)
    {
        await RequireAccessibleLessonAsync(lessonId, isAdmin);
        await RequireCommentAsync(commentId, lessonId, cancellationToken);

        var normalizedEmoji = NormalizeEmoji(emoji);
        var existingReaction = await _lessonCommentRepository.GetReactionAsync(commentId, currentUserId, normalizedEmoji, cancellationToken);

        if (existingReaction is not null)
        {
            return;
        }

        await _lessonCommentRepository.AddReactionAsync(new LessonCommentReaction
        {
            CommentId = commentId,
            UserId = currentUserId,
            Emoji = normalizedEmoji
        }, cancellationToken);

        await _lessonCommentRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveReactionAsync(
        Guid lessonId,
        Guid commentId,
        Guid currentUserId,
        bool isAdmin,
        string emoji,
        CancellationToken cancellationToken = default)
    {
        await RequireAccessibleLessonAsync(lessonId, isAdmin);
        await RequireCommentAsync(commentId, lessonId, cancellationToken);

        var normalizedEmoji = NormalizeEmoji(emoji);
        var existingReaction = await _lessonCommentRepository.GetReactionAsync(commentId, currentUserId, normalizedEmoji, cancellationToken);
        if (existingReaction is null)
        {
            return;
        }

        _lessonCommentRepository.RemoveReaction(existingReaction);
        await _lessonCommentRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCommentAsync(
        Guid lessonId,
        Guid commentId,
        Guid currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var comment = await RequireCommentAsync(commentId, lessonId, cancellationToken);

        if (!isAdmin && comment.UserId != currentUserId)
        {
            throw new InvalidOperationException("Bạn không có quyền xóa bình luận này.");
        }

        comment.DeletedAt = DateTime.UtcNow;
        comment.UpdatedAt = DateTime.UtcNow;
        await _lessonCommentRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task HideCommentAsync(Guid lessonId, Guid commentId, CancellationToken cancellationToken = default)
    {
        var comment = await RequireCommentAsync(commentId, lessonId, cancellationToken);
        comment.IsHidden = true;
        comment.UpdatedAt = DateTime.UtcNow;
        await _lessonCommentRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task UnhideCommentAsync(Guid lessonId, Guid commentId, CancellationToken cancellationToken = default)
    {
        var comment = await RequireCommentAsync(commentId, lessonId, cancellationToken);
        comment.IsHidden = false;
        comment.UpdatedAt = DateTime.UtcNow;
        await _lessonCommentRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> RequireUserAsync(Guid currentUserId)
    {
        return await _userRepository.GetByIdAsync(currentUserId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng hiện tại.");
    }

    private async Task<Lesson> RequireAccessibleLessonAsync(Guid lessonId, bool isAdmin)
    {
        var lesson = await _lessonRepository.GetByIdWithModuleAndCourseAsync(lessonId);
        if (lesson?.Module?.Course is null)
        {
            throw new KeyNotFoundException("Không tìm thấy lesson.");
        }

        if (!lesson.Module.Course.IsPublished && !isAdmin)
        {
            throw new InvalidOperationException("Bạn không có quyền bình luận lesson này.");
        }

        return lesson;
    }

    private async Task<LessonComment> RequireCommentAsync(Guid commentId, Guid lessonId, CancellationToken cancellationToken)
    {
        var comment = await _lessonCommentRepository.GetByIdAsync(commentId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy bình luận.");

        if (comment.LessonId != lessonId)
        {
            throw new InvalidOperationException("Bình luận không thuộc lesson này.");
        }

        return comment;
    }

    private async Task<Dictionary<Guid, IReadOnlyList<LessonComment>>> LoadRepliesByParentIdAsync(IReadOnlyCollection<Guid> parentIds, CancellationToken cancellationToken)
    {
        var replies = await _lessonCommentRepository.GetRepliesByParentIdsAsync(parentIds, true, cancellationToken);
        return replies
            .Where(reply => reply.ParentCommentId.HasValue)
            .GroupBy(reply => reply.ParentCommentId!.Value)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<LessonComment>)group.OrderBy(item => item.CreatedAt).ToList());
    }

    private static LessonCommentThreadResponse MapThread(
        LessonComment comment,
        IReadOnlyDictionary<Guid, IReadOnlyList<LessonComment>> replyLookup,
        Guid currentUserId,
        bool isAdmin)
    {
        replyLookup.TryGetValue(comment.Id, out var replies);

        return new LessonCommentThreadResponse
        {
            Comment = MapComment(comment, currentUserId, isAdmin),
            Replies = (replies ?? []).Select(reply => MapComment(reply, currentUserId, isAdmin)).ToList()
        };
    }

    private static LessonCommentItemResponse MapComment(LessonComment comment, Guid currentUserId, bool isAdmin)
    {
        var isDeleted = comment.DeletedAt.HasValue;
        var content = isDeleted
            ? "Bình luận này đã bị xóa."
            : comment.IsHidden && !isAdmin
                ? "Bình luận này đã bị ẩn."
                : comment.Content;

        return new LessonCommentItemResponse
        {
            Id = comment.Id,
            UserId = comment.UserId,
            AuthorName = comment.User?.FullName ?? "Người dùng",
            AuthorAvatarUrl = comment.User?.AvatarUrl,
            ReplyToUserId = comment.ReplyToUserId,
            ReplyToUserName = comment.ReplyToUser?.FullName,
            Content = content,
            Sentiment = comment.Sentiment,
            IsHidden = comment.IsHidden,
            IsDeleted = isDeleted,
            CanDelete = !isDeleted && (isAdmin || comment.UserId == currentUserId),
            CanModerate = isAdmin,
            CreatedAt = comment.CreatedAt,
            Reactions = MapReactions(comment.Reactions, currentUserId)
        };
    }

    private static IReadOnlyList<LessonCommentReactionResponse> MapReactions(IEnumerable<LessonCommentReaction> reactions, Guid currentUserId)
    {
        return reactions
            .GroupBy(reaction => reaction.Emoji)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => new LessonCommentReactionResponse
            {
                Emoji = group.Key,
                Count = group.Count(),
                ReactedByCurrentUser = group.Any(reaction => reaction.UserId == currentUserId)
            })
            .ToList();
    }

    private static string NormalizeSort(string sort)
    {
        return string.Equals(sort, "featured", StringComparison.OrdinalIgnoreCase) ? "featured" : "newest";
    }

    private static string NormalizeContent(string content)
    {
        var normalized = content.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Nội dung bình luận không được để trống.");
        }

        if (normalized.Length > 4000)
        {
            throw new InvalidOperationException("Nội dung bình luận vượt quá giới hạn cho phép.");
        }

        return normalized;
    }

    private static string NormalizeEmoji(string emoji)
    {
        var normalized = emoji.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Emoji reaction không hợp lệ.");
        }

        if (normalized.Length > 32)
        {
            throw new InvalidOperationException("Emoji reaction không hợp lệ.");
        }

        return normalized;
    }

    private static double CalculateFeaturedScore(LessonComment comment, IReadOnlyDictionary<Guid, IReadOnlyList<LessonComment>> replyLookup)
    {
        var reactionCount = comment.Reactions.Count;
        var replyCount = replyLookup.TryGetValue(comment.Id, out var replies) ? replies.Count : 0;
        var ageInDays = Math.Max(0, (DateTime.UtcNow - comment.CreatedAt).TotalDays);
        var freshnessBonus = Math.Max(0, 5 - ageInDays);
        return reactionCount * 3 + replyCount * 2 + freshnessBonus;
    }

    private void TriggerSentimentAnalysis(Guid commentId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<ILessonCommentRepository>();
                
                var comment = await repo.GetByIdAsync(commentId, CancellationToken.None);
                if (comment == null || string.IsNullOrWhiteSpace(comment.Content)) return;

                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("http://course_ai_worker:8000");

                var requestData = new { text = comment.Content };
                var jsonContent = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("/jobs/analyze-sentiment", jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var resultDoc = JsonDocument.Parse(responseString);
                    if (resultDoc.RootElement.TryGetProperty("pred_label", out var labelProp))
                    {
                        comment.Sentiment = labelProp.GetString();
                        await repo.SaveChangesAsync(CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error analyzing sentiment for comment {commentId}: {ex.Message}");
            }
        });
    }
}
