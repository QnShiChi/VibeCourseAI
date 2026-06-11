# Admin Negative Comment Dashboard Design

## Mục tiêu

Hiển thị số lượng bình luận tiêu cực trên dashboard quản trị và điều hướng admin sang một trang moderation riêng để xem chi tiết và xử lý.

Phạm vi của thay đổi này:

- dashboard chỉ hiển thị số lượng comment tiêu cực cần xử lý
- thêm trang admin moderation riêng cho comment tiêu cực
- trang moderation cho phép:
  - xem người viết, khóa học, bài học, nội dung comment, thời gian tạo
  - xóa bình luận
  - khóa tài khoản người viết bằng cách đặt `Users.IsActive = false`

Ngoài phạm vi:

- không thêm hệ thống ban tạm thời có hạn
- không thêm moderation status kiểu `reviewed` hay `dismissed`
- không thêm bulk actions ở giai đoạn này

## Bối cảnh hiện tại

- `LessonComments` đã có cột `Sentiment`
- sentiment được phân tích bất đồng bộ bởi `ai-worker`
- admin đã có action hide/unhide comment ở luồng bài học
- user đã có cờ `IsActive`
- dashboard admin hiện đang hiển thị thống kê tổng hợp

Điều này cho phép triển khai moderation flow mà không cần đổi schema.

## Thiết kế trải nghiệm

### Dashboard admin

Dashboard không hiển thị danh sách comment tiêu cực nữa.

Nó chỉ hiển thị một card summary:

- tiêu đề kiểu `Cảnh báo bình luận tiêu cực`
- số lượng comment tiêu cực chưa xử lý
- CTA `Xem chi tiết`

Khi admin bấm vào CTA, hệ thống điều hướng sang trang moderation riêng.

### Trang moderation riêng

Thêm route admin mới:

- `/admin/comment-moderation`

Trang này hiển thị danh sách các comment:

- `Sentiment = 'negative'`
- `IsHidden = false`
- `DeletedAt = null`

Mỗi item hiển thị:

- tên người viết
- email hoặc thông tin nhận diện cơ bản nếu cần
- tên khóa học
- tên bài học
- thời gian tạo
- nội dung bình luận
- badge `Tiêu cực`

Mỗi item có 2 action:

- `Xóa bình luận`
- `Khóa tài khoản`

## Thiết kế backend

### Dashboard stats

`GET /api/dashboard/stats` sẽ trả thêm:

- `negativeCommentsCount`

Dashboard không cần `negativeComments` list nữa.

### Moderation list API

Thêm endpoint admin-only mới để lấy danh sách moderation chi tiết.

Đề xuất:

- `GET /api/admin/comments/negative`

Response mỗi item gồm:

- `commentId`
- `lessonId`
- `lessonTitle`
- `courseId`
- `courseTitle`
- `authorUserId`
- `authorName`
- `authorEmail`
- `content`
- `createdAt`
- `sentiment`

Sắp xếp:

- mới nhất trước

### Delete comment

Ưu tiên tái sử dụng action xóa comment hiện có nếu đã đủ quyền admin và đủ thông tin `lessonId`.

Nếu action hiện tại không thuận tiện cho moderation page, có thể thêm một admin endpoint wrapper rõ nghĩa hơn, nhưng không thay đổi semantics xóa.

### Ban user

`Khóa tài khoản` sẽ là soft ban:

- set `Users.IsActive = false`

Hệ quả mong muốn:

- user không thể tiếp tục đăng nhập
- nếu đang có refresh token/session, hệ thống nên revoke toàn bộ refresh tokens của user đó

Vì repo hiện đã có logic admin update active user, ưu tiên tái sử dụng flow đó thay vì tạo mô hình ban mới.

## Thiết kế frontend

### Dashboard page

Panel moderation trên dashboard sẽ được rút gọn thành summary card:

- số lượng comment tiêu cực
- CTA điều hướng sang `/admin/comment-moderation`

Không hiển thị tên người viết, bài học hay nội dung comment trực tiếp ở dashboard.

### Comment moderation page

Trang mới sẽ:

- load danh sách negative comments từ API moderation
- render list/card/table tùy theo pattern admin hiện có
- cho phép:
  - `Xóa bình luận`
  - `Khóa tài khoản`

Hành vi UI:

- disable button trong lúc action đang chạy
- nếu xóa comment thành công:
  - remove item khỏi list
  - giảm tổng số lượng nếu đang giữ state local summary
- nếu khóa tài khoản thành công:
  - remove item khỏi list
  - hiển thị success feedback ngắn
- nếu lỗi:
  - giữ nguyên item
  - hiển thị lỗi inline hoặc alert ngắn gọn

## Dữ liệu và ràng buộc

- Chỉ coi `negative` là tiêu cực cần moderation
- Không hiển thị comment đã ẩn
- Không hiển thị comment đã xóa
- Không hiển thị comment `normal` hoặc `positive`
- Khóa tài khoản là soft lock qua `IsActive = false`

## Bảo mật và phân quyền

- dashboard summary moderation chỉ dành cho admin
- moderation list API chỉ dành cho admin
- delete comment action chỉ dành cho admin
- khóa tài khoản chỉ dành cho admin

## Kiểm thử

### Backend

- test dashboard stats trả đúng `negativeCommentsCount`
- test moderation list chỉ lấy comment `negative`
- test loại trừ comment hidden và deleted
- test ban user set `IsActive = false`
- test revoke refresh tokens nếu flow đó được tái dùng

### Frontend

- test dashboard chỉ render số lượng + CTA, không render chi tiết comment
- test moderation page render list chi tiết
- test xóa comment remove item khỏi list
- test khóa tài khoản remove item khỏi list
- test empty state khi không có comment tiêu cực

## Rủi ro

- sentiment hiện là nhãn đơn giản nên có thể có false positive
- khóa tài khoản là thao tác mạnh, cần label rõ ràng trên UI
- nếu không revoke session hiện tại thì user bị khóa có thể còn phiên sống tạm thời

## Quyết định chốt

- dashboard chỉ là summary
- chi tiết moderation nằm ở route riêng `/admin/comment-moderation`
- action moderation gồm `Xóa bình luận` và `Khóa tài khoản`
- khóa tài khoản dùng `IsActive = false`
- không đổi schema ở giai đoạn này
