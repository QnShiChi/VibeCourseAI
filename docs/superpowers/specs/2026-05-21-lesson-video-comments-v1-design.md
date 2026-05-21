# Lesson Video Comments V1 Design

Date: 2026-05-21

## Summary

Mục tiêu của `lesson video comments v1` là thêm một khu vực thảo luận ngay dưới mỗi lesson video để learner có thể trao đổi trực tiếp trong ngữ cảnh của bài học đang xem.

Version đầu tiên được chốt theo hướng:

- bình luận gắn với `mỗi lesson video`
- chỉ learner đã đăng nhập và có quyền học lesson đó mới được tương tác
- có `comment + reply 1 tầng`
- reply theo kiểu TikTok:
  - mọi reply đều nằm dưới comment gốc
  - nếu reply vào một reply khác thì phần nhập sẽ prefill `@username`
  - backend lưu `reply target` riêng để biết đang trả lời ai
- có `emoji reaction` tự do
- có sort `Mới nhất` và `Nổi bật`
- có `Load more`
- comment hiện ngay
- admin có thể `ẩn/xóa` mọi bình luận

## Goals

- Hiển thị comment feed ngay dưới lesson video trên trang learner.
- Cho phép learner đủ quyền:
  - tạo comment mới
  - reply vào comment hoặc reply khác
  - thả và bỏ emoji reaction
- Hỗ trợ sort:
  - `Mới nhất`
  - `Nổi bật`
- Hỗ trợ phân trang kiểu `Load more`.
- Hỗ trợ moderation cơ bản cho admin:
  - ẩn comment
  - bỏ ẩn comment
  - xóa comment
- Giữ kiến trúc đủ sạch để sau này mở rộng sang notification, realtime hoặc analytics.

## Non-Goals

- Chưa làm notification khi có reply hoặc reaction.
- Chưa làm websocket/realtime live updates.
- Chưa làm nhiều tầng thread lồng nhau.
- Chưa làm media attachment, image, file hoặc GIF.
- Chưa làm mention autocomplete nhiều người.
- Chưa làm edit history cho comment.
- Chưa làm reaction summary analytics hoặc anti-spam phức tạp.

## Current State

Trang learner hiện đã có:

- lesson video player
- lesson metadata
- course/module/lesson navigation

Nhưng chưa có lớp thảo luận nào ngay tại lesson. Điều này tạo khoảng trống rõ ràng:

- learner không có nơi để hỏi trong ngữ cảnh bài học
- course chỉ dừng ở việc xem video, chưa có tương tác cộng đồng
- admin chưa có công cụ moderation theo lesson content

## Domain Model

### LessonComment

Entity chính đại diện cho comment hoặc reply:

- `Id`
- `LessonId`
- `UserId`
- `ParentCommentId` nullable
- `ReplyToUserId` nullable
- `Content`
- `IsHidden`
- `CreatedAt`
- `UpdatedAt`
- `DeletedAt` nullable

Quy ước:

- `ParentCommentId = null` nghĩa là comment gốc
- `ParentCommentId != null` nghĩa là reply
- `ReplyToUserId` dùng để lưu người đang được trả lời trong UX kiểu `@username`

### LessonCommentReaction

Entity cho emoji reaction:

- `Id`
- `CommentId`
- `UserId`
- `Emoji`
- `CreatedAt`

Ràng buộc:

- unique theo `CommentId + UserId + Emoji`

Điều này cho phép:

- một user có thể thả nhiều loại emoji khác nhau trên cùng một comment nếu muốn
- nhưng không thể spam cùng một emoji lặp lại vô hạn

## Threading Model

Version này chỉ hỗ trợ `1 tầng reply`.

### Comment gốc

- hiển thị ở cấp đầu
- có danh sách replies bên dưới

### Reply vào comment gốc

- tạo row mới với:
  - `ParentCommentId = comment gốc`
  - `ReplyToUserId = user của comment gốc`

### Reply vào một reply khác

- vẫn tạo row mới dưới comment gốc
- `ParentCommentId` vẫn trỏ tới comment gốc
- `ReplyToUserId` trỏ tới user của reply đang được trả lời

Frontend sẽ prefill composer với `@username` của người được trả lời, nhưng backend không phụ thuộc vào việc parse mention trong text để hiểu cấu trúc thread.

## Permissions

### Learner permissions

Chỉ user đã đăng nhập và có quyền học lesson đó mới được:

- xem comment feed
- đăng comment
- reply
- thả reaction
- bỏ reaction

Version đầu nên cho owner:

- xóa comment của chính mình

Không cần cho learner ẩn comment người khác.

### Admin permissions

Admin có thể:

- xem toàn bộ comment kể cả comment ẩn
- ẩn comment
- bỏ ẩn comment
- xóa mọi comment

Admin moderation là immediate, không có queue duyệt.

## Visibility Rules

### Với learner thường

- comment `IsHidden = true` không hiển thị nội dung gốc
- comment bị xóa hiển thị placeholder:
  - `Bình luận này đã bị xóa.`
- comment bị admin ẩn hiển thị placeholder:
  - `Bình luận này đã bị ẩn.`

Nếu một comment gốc bị ẩn hoặc xóa:

- thread vẫn giữ cấu trúc
- replies bên dưới vẫn có thể tồn tại nếu chưa bị ẩn/xóa

### Với admin

- vẫn thấy comment trong trạng thái moderation để thao tác
- UI nên có dấu hiệu rõ ràng comment nào đang hidden/deleted

## Sorting

### 1. Mới nhất

Sắp xếp theo:

- `CreatedAt desc`

### 2. Nổi bật

Version đầu dùng heuristic đơn giản:

- tổng reaction count
- số lượng replies
- cộng điểm nhỏ cho độ mới

Không cần thuật toán ranking phức tạp. Mục tiêu chỉ là tạo một thứ tự “hợp lý” hơn `Newest`.

Ví dụ conceptual score:

- `reactionCount * 3 + replyCount * 2 + freshnessBonus`

Chi tiết exact formula có thể chốt ở implementation plan, nhưng spec này chốt rõ:

- `Featured` là ranking heuristic
- không phải chronological sort

## Pagination

Feed comments dùng `Load more`, không dùng infinite scroll trong `v1`.

Nguyên tắc:

- API trả page đầu của comment gốc
- mỗi comment gốc đi kèm một phần replies tương ứng
- frontend có nút `Load more`

Replies trong một thread có thể:

- hiển thị luôn toàn bộ nếu số lượng nhỏ
- hoặc có mini `Load more replies` sau này

`v1` nên ưu tiên đơn giản:

- page theo comment gốc
- replies cho comment gốc được load cùng payload đó

## Frontend UX

### Placement

Khối `Bình luận` nằm ngay dưới lesson video trong trang learner.

Thứ tự đề xuất:

1. video player
2. lesson title / description
3. comment section
4. phần nội dung lesson chi tiết nếu cần giữ lại phía dưới

### Composer

Composer comment mới gồm:

- avatar user
- textarea hoặc input multiline
- nút `Gửi`

Composer reply:

- mở inline dưới comment gốc
- nếu reply vào reply khác thì prefill `@username`
- vẫn submit vào thread của comment gốc

Validation:

- không cho gửi rỗng
- trim khoảng trắng đầu cuối
- giới hạn ký tự nên có, nhưng `v1` chỉ cần giới hạn thực dụng

### Comment Card

Mỗi comment/reply hiển thị:

- avatar
- tên user
- timestamp tương đối hoặc rõ ràng
- nội dung
- row actions:
  - `Reply`
  - emoji reaction trigger
  - `Xóa` nếu là owner
  - moderation actions nếu là admin

Reaction UX:

- hiển thị reaction pills ngay dưới comment
- bấm lại emoji đã chọn để bỏ reaction
- có một nút hoặc picker đơn giản để thêm emoji mới

### Sorting Control

Ngay trên feed có control chuyển:

- `Mới nhất`
- `Nổi bật`

UI có thể là segmented control hoặc dropdown tùy layout hiện có.

### Empty State

Nếu lesson chưa có comment:

- hiển thị message kiểu:
  - `Chưa có bình luận nào. Hãy bắt đầu cuộc thảo luận cho bài học này.`

## API Design

### Learner APIs

`GET /api/lessons/{lessonId}/comments?sort=newest|featured&page=1&pageSize=10`

Trả:

- danh sách comment gốc
- nested replies cho từng comment
- reaction summary
- user reaction state
- paging metadata

`POST /api/lessons/{lessonId}/comments`

Body:

- `content`

Tạo comment gốc.

`POST /api/comments/{commentId}/replies`

Body:

- `content`
- `replyToUserId` optional nhưng nên gửi khi reply vào reply

Backend sẽ resolve:

- `ParentCommentId`
- `ReplyToUserId`

theo quy tắc 1 tầng.

`POST /api/comments/{commentId}/reactions`

Body:

- `emoji`

`DELETE /api/comments/{commentId}/reactions/{emoji}`

Xóa reaction của current user với emoji đó.

`DELETE /api/comments/{commentId}`

- owner: xóa comment của chính mình
- admin: xóa mọi comment

### Admin moderation APIs

`PATCH /api/admin/comments/{commentId}/hide`

`PATCH /api/admin/comments/{commentId}/unhide`

Nếu codebase hiện tại đã có pattern admin route trong domain service khác, implementation nên bám pattern đó thay vì áp đặt controller mới lạ.

## Response Shape

Frontend learner page cần một shape đủ giàu để không phải gọi quá nhiều round-trip.

Mỗi root comment nên trả:

- comment info
- author display info
- reaction aggregates
- current user reaction list
- replies array

Mỗi reply nên trả:

- reply info
- author display info
- `replyToUser` display info nếu có
- reaction aggregates
- current user reaction list

## Moderation Semantics

### Hide

- comment vẫn còn trong DB
- learner thấy placeholder hidden
- admin có thể unhide

### Delete

- có thể dùng soft delete để giữ thread integrity
- learner thấy placeholder deleted
- hệ thống không mất cấu trúc thread

Spec này nghiêng về `soft delete` vì phù hợp với thread/reply model hơn hard delete.

## Backend Architecture

Nên tách module riêng cho lesson discussion thay vì nhúng logic rải rác vào lesson service hiện tại.

Đề xuất trách nhiệm:

- comment repository/query layer
- comment service cho business rules:
  - permission
  - threading
  - reaction uniqueness
  - sort mode
  - moderation
- controller endpoints cho learner/admin

ASP.NET Core tiếp tục là lớp điều phối chính:

- xác thực user
- xác thực quyền học lesson
- xác thực quyền admin
- trả payload đã aggregate cho frontend

## Frontend Architecture

Tạo module UI riêng cho comments dưới lesson page.

Các component dự kiến:

- `LessonComments`
- `CommentComposer`
- `CommentList`
- `CommentItem`
- `CommentReplyList`
- `CommentReactionBar`
- `CommentSortControl`

Hook hoặc service phụ trợ:

- fetch comments theo lesson
- optimistic update có thể cân nhắc ở `v1`, nhưng không bắt buộc

Version đầu có thể dùng refresh cục bộ sau thao tác để giảm rủi ro, miễn UX vẫn mượt chấp nhận được.

## Data Integrity Rules

- Reply không được reply sang lesson khác.
- `ParentCommentId` phải thuộc cùng `LessonId`.
- Nếu reply vào reply, backend phải resolve về root comment đúng.
- Reaction chỉ được tạo trên comment thuộc lesson mà user có quyền học.
- Một user không được tạo trùng cùng emoji trên cùng comment nhiều lần.

## Performance Notes

- Feed nên page theo root comments để tránh payload quá lớn.
- Query `Featured` nên được thiết kế đủ thực dụng; chưa cần tối ưu extreme scale.
- Cần tránh N+1 queries khi load author, replies và reaction aggregates.

## Accessibility

- Composer có label rõ ràng.
- Buttons `Reply`, `Like/emoji`, `Load more` có accessible name.
- Reaction picker phải thao tác được bằng keyboard.
- Placeholder hidden/deleted phải dễ hiểu với screen reader.

## Rollout Strategy

Version đầu chỉ cần xuất hiện ở learner page lesson video.

Admin moderation UI có thể bắt đầu ở mức tối thiểu:

- action buttons ngay trong cùng feed nếu admin đang xem lesson

Không bắt buộc phải có một trang quản trị comment riêng trong `v1`.

## Open Decisions Already Resolved

Các điểm đã chốt với user:

- `comment + reply + emoji reaction`
- chỉ learner có quyền học mới được tương tác
- comment gắn với `lesson`
- moderation immediate, admin có thể hide/delete
- emoji reaction tự do
- chỉ `1 tầng reply`
- reply kiểu TikTok với `@username`
- sort `Mới nhất` / `Nổi bật`
- pagination kiểu `Load more`
- chưa làm notification
