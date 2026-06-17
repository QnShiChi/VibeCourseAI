using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Globalization;
using CourseVideo.API.Configuration;
using CourseVideo.API.Data;
using CourseVideo.API.DTOs.Carts;
using CourseVideo.API.DTOs.Payments;
using CourseVideo.API.Models;
using CourseVideo.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CourseVideo.API.Services;

public class PaymentService : IPaymentService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    private readonly AppDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SepayOptions _sepayOptions;

    public PaymentService(
        AppDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        IOptions<SepayOptions> sepayOptions)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
        _sepayOptions = sepayOptions.Value;
    }

    public async Task<CartResponse> GetCartAsync(Guid? userId, string? guestCartToken, CancellationToken cancellationToken = default)
    {
        var normalizedToken = NormalizeGuestToken(guestCartToken);
        var query = _dbContext.CartItems
            .Include(item => item.Course)
            .ThenInclude(course => course!.Category)
            .AsQueryable();

        query = userId.HasValue
            ? query.Where(item => item.UserId == userId.Value)
            : query.Where(item => item.GuestCartToken == normalizedToken);

        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        var courseIds = items.Select(item => item.CourseId).Distinct().ToList();
        HashSet<Guid> ownedCourseIds = [];

        if (userId.HasValue && courseIds.Count > 0)
        {
            ownedCourseIds = (await _dbContext.CourseEnrollments
                .Where(enrollment => enrollment.UserId == userId.Value && courseIds.Contains(enrollment.CourseId))
                .Select(enrollment => enrollment.CourseId)
                .ToListAsync(cancellationToken))
                .ToHashSet();

            var staleOwnedCartItems = items
                .Where(item => ownedCourseIds.Contains(item.CourseId))
                .ToList();

            if (staleOwnedCartItems.Count > 0)
            {
                _dbContext.CartItems.RemoveRange(staleOwnedCartItems);
                await _dbContext.SaveChangesAsync(cancellationToken);
                items = items
                    .Where(item => !ownedCourseIds.Contains(item.CourseId))
                    .ToList();
            }
        }

        return new CartResponse
        {
            GuestCartToken = normalizedToken,
            Items = items
                .Where(item => item.Course is not null && item.Course.IsPublished)
                .Select(item => MapCartItem(item.Course!, false))
                .ToList()
        };
    }

    public async Task<CartResponse> AddCartItemAsync(Guid? userId, AddCartItemRequest request, CancellationToken cancellationToken = default)
    {
        var course = await _dbContext.Courses
            .FirstOrDefaultAsync(item => item.Id == request.CourseId && item.IsPublished, cancellationToken);

        if (course is null)
        {
            throw new KeyNotFoundException("Khóa học không tồn tại hoặc chưa publish.");
        }

        var normalizedToken = NormalizeGuestToken(request.GuestCartToken);

        if (!userId.HasValue && string.IsNullOrWhiteSpace(normalizedToken))
        {
            normalizedToken = $"guest_{Guid.NewGuid():N}";
        }

        if (userId.HasValue)
        {
            var hasAccess = await HasCourseAccessAsync(userId.Value, request.CourseId, cancellationToken);
            if (hasAccess)
            {
                throw new InvalidOperationException("Bạn đã sở hữu khóa học này.");
            }

            var exists = await _dbContext.CartItems.AnyAsync(
                item => item.UserId == userId.Value && item.CourseId == request.CourseId,
                cancellationToken);

            if (!exists)
            {
                _dbContext.CartItems.Add(new CartItem
                {
                    UserId = userId.Value,
                    CourseId = request.CourseId
                });
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            var exists = await _dbContext.CartItems.AnyAsync(
                item => item.GuestCartToken == normalizedToken && item.CourseId == request.CourseId,
                cancellationToken);

            if (!exists)
            {
                _dbContext.CartItems.Add(new CartItem
                {
                    GuestCartToken = normalizedToken,
                    CourseId = request.CourseId
                });
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        return await GetCartAsync(userId, normalizedToken, cancellationToken);
    }

    public async Task<CartResponse> RemoveCartItemAsync(Guid? userId, Guid courseId, string? guestCartToken, CancellationToken cancellationToken = default)
    {
        var normalizedToken = NormalizeGuestToken(guestCartToken);
        CartItem? item = null;

        if (userId.HasValue)
        {
            item = await _dbContext.CartItems.FirstOrDefaultAsync(
                current => current.UserId == userId.Value && current.CourseId == courseId,
                cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(normalizedToken))
        {
            item = await _dbContext.CartItems.FirstOrDefaultAsync(
                current => current.GuestCartToken == normalizedToken && current.CourseId == courseId,
                cancellationToken);
        }

        if (item is not null)
        {
            _dbContext.CartItems.Remove(item);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return await GetCartAsync(userId, normalizedToken, cancellationToken);
    }

    public async Task<CartResponse> MergeCartAsync(Guid userId, string guestCartToken, CancellationToken cancellationToken = default)
    {
        var normalizedToken = NormalizeGuestToken(guestCartToken);
        if (string.IsNullOrWhiteSpace(normalizedToken))
        {
            return await GetCartAsync(userId, null, cancellationToken);
        }

        var guestItems = await _dbContext.CartItems
            .Where(item => item.GuestCartToken == normalizedToken)
            .ToListAsync(cancellationToken);

        var ownedCourseIds = (await _dbContext.CourseEnrollments
            .Where(item => item.UserId == userId)
            .Select(item => item.CourseId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        foreach (var item in guestItems)
        {
            if (ownedCourseIds.Contains(item.CourseId))
            {
                _dbContext.CartItems.Remove(item);
                continue;
            }

            var userItemExists = await _dbContext.CartItems.AnyAsync(
                current => current.UserId == userId && current.CourseId == item.CourseId,
                cancellationToken);

            if (userItemExists)
            {
                _dbContext.CartItems.Remove(item);
                continue;
            }

            item.UserId = userId;
            item.GuestCartToken = null;
            item.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetCartAsync(userId, null, cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentOrderResponse>> CreateOrdersAsync(Guid userId, IReadOnlyList<Guid> courseIds, CancellationToken cancellationToken = default)
    {
        var distinctCourseIds = courseIds
            .Where(item => item != Guid.Empty)
            .Distinct()
            .ToList();

        if (distinctCourseIds.Count == 0)
        {
            throw new InvalidOperationException("Không có khóa học hợp lệ để checkout.");
        }

        var ownedCourseIds = (await _dbContext.CourseEnrollments
            .Where(item => item.UserId == userId && distinctCourseIds.Contains(item.CourseId))
            .Select(item => item.CourseId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var courses = await _dbContext.Courses
            .Where(item => distinctCourseIds.Contains(item.Id) && item.IsPublished)
            .ToListAsync(cancellationToken);

        if (courses.Count == 0)
        {
            throw new InvalidOperationException("Không tìm thấy khóa học hợp lệ để thanh toán.");
        }

        var activeBankAccount = await GetActiveBankAccountAsync(cancellationToken);
        var responses = new List<PaymentOrderResponse>();
        var now = DateTime.UtcNow;

        foreach (var course in courses)
        {
            if (ownedCourseIds.Contains(course.Id))
            {
                continue;
            }

            var existingPendingOrder = await _dbContext.PaymentOrders
                .FirstOrDefaultAsync(
                    item => item.UserId == userId
                        && item.CourseId == course.Id
                        && item.Status == "Pending"
                        && item.ExpiresAt > now,
                    cancellationToken);

            if (existingPendingOrder is not null)
            {
                responses.Add(MapPaymentOrder(existingPendingOrder, course.Title));
                continue;
            }

            var orderCode = BuildOrderCode();
            var order = new PaymentOrder
            {
                UserId = userId,
                CourseId = course.Id,
                Amount = course.Price,
                OrderCode = orderCode,
                Status = "Pending",
                ExpiresAt = now.AddMinutes(_sepayOptions.OrderExpiryMinutes > 0 ? _sepayOptions.OrderExpiryMinutes : 5),
                BankCode = activeBankAccount.BankCode,
                BankName = activeBankAccount.BankName,
                BankAccountNumber = activeBankAccount.AccountNumber,
                AccountHolderName = activeBankAccount.AccountHolderName,
                TransferContent = orderCode
            };

            _dbContext.PaymentOrders.Add(order);
            responses.Add(MapPaymentOrder(order, course.Title));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return responses
            .Select(item =>
            {
                item.IsExpired = item.Status == "Pending" && item.ExpiresAt <= DateTime.UtcNow;
                return item;
            })
            .ToList();
    }

    public async Task<PaymentOrderResponse?> GetOrderAsync(Guid userId, Guid orderId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.PaymentOrders
            .Include(item => item.Course)
            .FirstOrDefaultAsync(item => item.Id == orderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        if (!isAdmin && order.UserId != userId)
        {
            return null;
        }

        if (order.Status == "Pending" || order.Status == "Expired")
        {
            await TrySyncOrderWithSepayAsync(order, cancellationToken);
        }

        if (order.Status == "Pending" && order.ExpiresAt <= DateTime.UtcNow)
        {
            order.Status = "Expired";
            order.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapPaymentOrder(order, order.Course?.Title ?? string.Empty);
    }

    public async Task<PaymentOrderResponse?> CancelOrderAsync(Guid userId, Guid orderId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.PaymentOrders
            .Include(item => item.Course)
            .FirstOrDefaultAsync(item => item.Id == orderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        if (!isAdmin && order.UserId != userId)
        {
            return null;
        }

        if (order.Status == "Pending" && order.ExpiresAt <= DateTime.UtcNow)
        {
            order.Status = "Expired";
            order.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Đơn hàng đã hết hạn, không thể hủy thanh toán nữa.");
        }

        if (order.Status != "Pending")
        {
            throw new InvalidOperationException("Chỉ có thể hủy thanh toán khi đơn hàng đang chờ thanh toán.");
        }

        order.Status = "Cancelled";
        order.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapPaymentOrder(order, order.Course?.Title ?? string.Empty);
    }

    public async Task<IReadOnlyList<PurchaseHistoryItemResponse>> GetPurchaseHistoryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CourseEnrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.UserId == userId)
            .Include(enrollment => enrollment.Course)
            .Include(enrollment => enrollment.PaymentOrder)
            .OrderByDescending(enrollment => enrollment.GrantedAt)
            .Select(enrollment => new PurchaseHistoryItemResponse
            {
                PaymentOrderId = enrollment.PaymentOrderId,
                OrderCode = enrollment.PaymentOrder != null ? enrollment.PaymentOrder.OrderCode : string.Empty,
                CourseId = enrollment.CourseId,
                CourseTitle = enrollment.Course != null ? enrollment.Course.Title : string.Empty,
                CourseThumbnailUrl = enrollment.Course != null ? enrollment.Course.ThumbnailUrl : null,
                Amount = enrollment.PaymentOrder != null ? enrollment.PaymentOrder.Amount : 0,
                Status = enrollment.PaymentOrder != null ? enrollment.PaymentOrder.Status : "Paid",
                PurchasedAt = enrollment.GrantedAt,
                PaidAt = enrollment.PaymentOrder != null ? enrollment.PaymentOrder.PaidAt : null
            })
            .ToListAsync(cancellationToken);
    }

    public async Task HandleSepayWebhookAsync(
        SepayWebhookPayload payload,
        string rawPayload,
        string? apiKeyHeader,
        CancellationToken cancellationToken = default,
        bool validateWebhookCredential = true)
    {
        if (validateWebhookCredential
            && !string.IsNullOrWhiteSpace(_sepayOptions.WebhookApiKey)
            && !IsValidWebhookCredential(apiKeyHeader))
        {
            throw new UnauthorizedAccessException("Webhook API key không hợp lệ.");
        }

        var existingLog = await _dbContext.PaymentTransactionLogs
            .FirstOrDefaultAsync(item => item.SepayTransactionId == payload.Id, cancellationToken);

        if (existingLog is not null)
        {
            existingLog.IsDuplicate = true;
            existingLog.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var transactionLog = new PaymentTransactionLog
        {
            SepayTransactionId = payload.Id,
            Gateway = payload.Gateway,
            TransactionDateText = payload.TransactionDate,
            AccountNumber = payload.AccountNumber,
            SubAccount = payload.SubAccount,
            Code = payload.Code,
            Content = payload.Content,
            TransferType = payload.TransferType,
            Description = payload.Description,
            TransferAmount = payload.TransferAmount,
            Accumulated = payload.Accumulated,
            ReferenceCode = payload.ReferenceCode,
            RawPayload = rawPayload,
            ProcessedAt = DateTime.UtcNow
        };

        _dbContext.PaymentTransactionLogs.Add(transactionLog);

        if (!string.Equals(payload.TransferType, "in", StringComparison.OrdinalIgnoreCase))
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var normalizedContent = payload.Content.Trim();
        var order = await _dbContext.PaymentOrders
            .Include(item => item.Course)
            .FirstOrDefaultAsync(
                item => normalizedContent.Contains(item.OrderCode)
                    && item.Amount == payload.TransferAmount
                    && (item.Status == "Pending" || item.Status == "Expired"),
                cancellationToken);

        if (order is null)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        transactionLog.MatchedPaymentOrderId = order.Id;

        var paidAt = ParseVietnamTime(payload.TransactionDate);
        var wasExpired = paidAt > order.ExpiresAt;
        order.Status = wasExpired ? "LatePaid" : "Paid";
        order.PaidAt = paidAt;
        order.SepayTransactionId = payload.Id;
        order.UpdatedAt = DateTime.UtcNow;

        var enrollmentExists = await _dbContext.CourseEnrollments.AnyAsync(
            item => item.UserId == order.UserId && item.CourseId == order.CourseId,
            cancellationToken);

        if (!enrollmentExists)
        {
            _dbContext.CourseEnrollments.Add(new CourseEnrollment
            {
                UserId = order.UserId,
                CourseId = order.CourseId,
                PaymentOrderId = order.Id,
                GrantedAt = paidAt
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> HasCourseAccessAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default)
    {
        return _dbContext.CourseEnrollments.AnyAsync(
            item => item.UserId == userId && item.CourseId == courseId,
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, DateTime>> GetOwnedCourseGrantedAtLookupAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CourseEnrollments
            .Where(item => item.UserId == userId)
            .GroupBy(item => item.CourseId)
            .Select(group => new
            {
                CourseId = group.Key,
                GrantedAt = group.Max(item => item.GrantedAt)
            })
            .ToDictionaryAsync(item => item.CourseId, item => item.GrantedAt, cancellationToken);
    }

    private async Task TrySyncOrderWithSepayAsync(PaymentOrder order, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_sepayOptions.ApiToken) || string.IsNullOrWhiteSpace(order.BankAccountNumber))
        {
            return;
        }

        var client = _httpClientFactory.CreateClient();
        var isSandbox = string.Equals(_sepayOptions.Environment, "Sandbox", StringComparison.OrdinalIgnoreCase);
        client.BaseAddress = new Uri(isSandbox ? "https://userapi-sandbox.sepay.vn/v2/" : "https://userapi.sepay.vn/v2/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _sepayOptions.ApiToken);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

        var accountNumber = Uri.EscapeDataString(order.BankAccountNumber);
        using var response = await client.GetAsync($"transactions?account_number={accountNumber}&per_page=20", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var transactions = JsonSerializer.Deserialize<SepayTransactionsResponse>(body, JsonOptions);
        var matchedTransaction = transactions?.Data.FirstOrDefault(item =>
            string.Equals(item.TransferType, "in", StringComparison.OrdinalIgnoreCase)
            && item.AmountIn == order.Amount
            && string.Equals(item.AccountNumber, order.BankAccountNumber, StringComparison.OrdinalIgnoreCase)
            && item.TransactionContent.Contains(order.OrderCode, StringComparison.OrdinalIgnoreCase));

        if (matchedTransaction is null)
        {
            return;
        }

        var payload = new SepayWebhookPayload
        {
            Id = CreateSyntheticTransactionId(matchedTransaction.ReferenceNumber ?? matchedTransaction.Id),
            Gateway = matchedTransaction.BankBrandName ?? order.BankName ?? string.Empty,
            TransactionDate = matchedTransaction.TransactionDate,
            AccountNumber = matchedTransaction.AccountNumber,
            Content = matchedTransaction.TransactionContent,
            TransferType = matchedTransaction.TransferType,
            TransferAmount = matchedTransaction.AmountIn,
            Accumulated = matchedTransaction.Accumulated,
            ReferenceCode = matchedTransaction.ReferenceNumber
        };

        await HandleSepayWebhookAsync(payload, body, null, cancellationToken, validateWebhookCredential: false);
    }

    private async Task<(string BankCode, string BankName, string AccountNumber, string AccountHolderName)> GetActiveBankAccountAsync(CancellationToken cancellationToken)
    {
        var isSandbox = string.Equals(_sepayOptions.Environment, "Sandbox", StringComparison.OrdinalIgnoreCase);
        var configuredAdminPaymentProfile = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.IsActive
                && user.RoleId == 1
                && !string.IsNullOrWhiteSpace(user.PaymentBankCode)
                && !string.IsNullOrWhiteSpace(user.PaymentBankAccountNumber)
                && !string.IsNullOrWhiteSpace(user.PaymentAccountHolderName))
            .OrderByDescending(user => user.PaymentSettingsUpdatedAt ?? user.UpdatedAt ?? user.CreatedAt)
            .Select(user => new
            {
                BankCode = user.PaymentBankCode!,
                BankName = user.PaymentBankName,
                AccountNumber = user.PaymentBankAccountNumber!,
                AccountHolderName = user.PaymentAccountHolderName!
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (configuredAdminPaymentProfile is not null)
        {
            return (
                configuredAdminPaymentProfile.BankCode.Trim(),
                string.IsNullOrWhiteSpace(configuredAdminPaymentProfile.BankName)
                    ? configuredAdminPaymentProfile.BankCode.Trim()
                    : configuredAdminPaymentProfile.BankName.Trim(),
                configuredAdminPaymentProfile.AccountNumber.Trim(),
                configuredAdminPaymentProfile.AccountHolderName.Trim()
            );
        }

        if (!string.IsNullOrWhiteSpace(_sepayOptions.BankCode)
            && !string.IsNullOrWhiteSpace(_sepayOptions.BankAccountNumber)
            && !string.IsNullOrWhiteSpace(_sepayOptions.AccountHolderName))
        {
            return (
                _sepayOptions.BankCode,
                string.IsNullOrWhiteSpace(_sepayOptions.BankName) ? _sepayOptions.BankCode : _sepayOptions.BankName,
                _sepayOptions.BankAccountNumber,
                _sepayOptions.AccountHolderName
            );
        }

        if (string.IsNullOrWhiteSpace(_sepayOptions.ApiToken))
        {
            if (isSandbox)
            {
                return (
                    "MB",
                    "MB Bank",
                    "970422000000001",
                    "SEPAY SANDBOX"
                );
            }

            throw new InvalidOperationException("Thiếu cấu hình SePay API token hoặc thông tin tài khoản ngân hàng.");
        }

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(isSandbox ? "https://userapi-sandbox.sepay.vn/v2/" : "https://userapi.sepay.vn/v2/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _sepayOptions.ApiToken);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

        using var response = await client.GetAsync("bank-accounts?active=1&per_page=20", cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var bankAccounts = JsonSerializer.Deserialize<SepayBankAccountsResponse>(body, JsonOptions);
        var account = bankAccounts?.Data.FirstOrDefault(item => item.Active == 1)
            ?? throw new InvalidOperationException("Không tìm thấy tài khoản ngân hàng hoạt động trên SePay.");

        return (
            string.IsNullOrWhiteSpace(account.BankCode) ? account.BankShortName : account.BankCode,
            string.IsNullOrWhiteSpace(account.BankFullName) ? account.BankShortName : account.BankFullName,
            account.AccountNumber,
            account.AccountHolderName
        );
    }

    private PaymentOrderResponse MapPaymentOrder(PaymentOrder order, string courseTitle)
    {
        return new PaymentOrderResponse
        {
            Id = order.Id,
            OrderCode = order.OrderCode,
            CourseId = order.CourseId,
            CourseTitle = courseTitle,
            Amount = order.Amount,
            Status = order.Status,
            ExpiresAt = order.ExpiresAt,
            PaidAt = order.PaidAt,
            BankCode = order.BankCode ?? string.Empty,
            BankName = order.BankName ?? order.BankCode ?? string.Empty,
            BankAccountNumber = order.BankAccountNumber ?? string.Empty,
            AccountHolderName = order.AccountHolderName ?? string.Empty,
            TransferContent = order.TransferContent,
            QrImageUrl = SepayQrImageBuilder.Build(order, _sepayOptions.StoreName, _sepayOptions.Environment),
            IsExpired = order.Status == "Expired" || (order.Status == "Pending" && order.ExpiresAt <= DateTime.UtcNow)
        };
    }

    private CartItemResponse MapCartItem(Course course, bool alreadyOwned)
    {
        return new CartItemResponse
        {
            CourseId = course.Id,
            CourseTitle = course.Title,
            CourseDescription = course.Description,
            ThumbnailUrl = course.ThumbnailUrl,
            Category = course.Category?.Name ?? "Chưa phân loại",
            Price = course.Price,
            AlreadyOwned = alreadyOwned
        };
    }

    private static string NormalizeGuestToken(string? guestCartToken)
    {
        return string.IsNullOrWhiteSpace(guestCartToken) ? string.Empty : guestCartToken.Trim();
    }

    private static string BuildOrderCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(6);
        return $"VC{Convert.ToHexString(bytes)}";
    }

    private static int CreateSyntheticTransactionId(string seed)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(seed));
        var value = BitConverter.ToInt32(bytes, 0) & int.MaxValue;
        return value == 0 ? 1 : value;
    }

    internal static DateTime ParseVietnamTime(string? transactionDate)
    {
        if (!DateTime.TryParse(transactionDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return DateTime.UtcNow;
        }

        var vietnamLocalTime = DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(vietnamLocalTime, VietnamTimeZone);
    }

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }

    private bool IsValidWebhookCredential(string? providedHeader)
    {
        if (string.IsNullOrWhiteSpace(providedHeader))
        {
            return false;
        }

        if (string.Equals(providedHeader, _sepayOptions.WebhookApiKey, StringComparison.Ordinal))
        {
            return true;
        }

        return string.Equals(providedHeader, $"Apikey {_sepayOptions.WebhookApiKey}", StringComparison.Ordinal);
    }
}
