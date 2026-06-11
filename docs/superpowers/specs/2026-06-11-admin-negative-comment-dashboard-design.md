# Admin Negative Comment Dashboard Design

## Mục tiêu

Hiển thị các bình luận có cảm xúc tiêu cực trên dashboard quản trị để admin phát hiện nhanh và ẩn ngay các bình luận cần xử lý.

Phạm vi của thay đổi này:

- thêm dữ liệu comment tiêu cực vào dashboard admin
- chỉ lấy các comment:
  - `Sentiment = 'negative'`
  - `IsHidden = false`
  - `DeletedAt = null`
- cho phép admin ẩn comment trực tiếp từ dashboard

Ngoài phạm vi:

- không thêm hệ thống cảnh cáo user riêng
- không thêm trạng thái moderation mới như `reviewed`, `dismissed`
- không tạo trang moderation riêng

## Bối cảnh hiện tại

- `LessonComments` đã có cột `Sentiment`
- sentiment được phân tích bất đồng bộ bởi `ai-worker`
- admin đã có API hide/unhide comment ở luồng bài học hiện tại
- dashboard admin hiện chỉ hiển thị thống kê khóa học, user, job và activity feed

Điều này cho phép triển khai moderation queue trên dashboard mà không cần đổi schema.

## Thiết kế backend

### Dữ liệu cần trả về

Dashboard sẽ nhận thêm một danh sách `negativeComments`.

Mỗi item gồm:

- `commentId`
- `lessonId`
- `lessonTitle`
- `courseId`
- `courseTitle`
- `authorName`
- `authorUserId`
- `content`
- `createdAt`
- `sentiment`

### Truy vấn

Backend thêm một truy vấn admin-only lấy comment tiêu cực chưa xử lý:

- lọc `Sentiment == "negative"`
- lọc `IsHidden == false`
- lọc `DeletedAt == null`
- sắp xếp `CreatedAt DESC`
- giới hạn mặc định 5 item cho dashboard

### API

Ưu tiên mở rộng API dashboard stats hiện có thay vì tạo endpoint moderation riêng.

Đề xuất:

- endpoint dashboard hiện tại trả thêm:
  - `negativeCommentsCount`
  - `negativeComments`

Lý do:

- dashboard chỉ cần một payload tổng hợp
- tránh thêm một request riêng trong lần tải đầu
- bám sát mục tiêu dashboard-only moderation queue

Nếu service dashboard hiện tại đang tách biệt cứng khỏi comments, có thể thêm một method service nội bộ mới và compose vào response hiện tại.

### Hành động ẩn comment

Dashboard không tạo API mới cho moderation action.

Nó sẽ tái sử dụng API admin hide comment đang có. Sau khi hide thành công:

- comment bị loại khỏi queue trên UI
- `negativeCommentsCount` giảm ngay trên client

## Thiết kế frontend

### Vị trí hiển thị

Thêm một panel mới trên `DashboardPage` với tiêu đề kiểu:

- `Cảnh báo bình luận tiêu cực`

Panel này nằm cùng tầng thông tin quản trị hiện tại, không thay đổi layout tổng thể của dashboard.

### Nội dung panel

Panel hiển thị:

- tổng số comment tiêu cực chưa ẩn
- tối đa 5 comment mới nhất

Mỗi item hiển thị:

- tên người viết
- tên khóa học hoặc bài học
- thời gian tạo
- nội dung comment, có cắt bớt nếu dài
- badge `Tiêu cực`
- nút `Ẩn bình luận`
- link điều hướng đến bài học liên quan nếu route hiện có hỗ trợ

### Hành vi UI

Khi admin bấm `Ẩn bình luận`:

- disable nút của item đang xử lý
- gọi API hide comment hiện có
- nếu thành công:
  - xóa item khỏi danh sách hiện tại
  - cập nhật số lượng queue trên UI
- nếu lỗi:
  - hiển thị lỗi inline ngắn gọn
  - giữ nguyên item

Nếu queue rỗng:

- hiển thị empty state kiểu `Chưa có bình luận tiêu cực cần xử lý`

## Dữ liệu và ràng buộc

- Chỉ coi `negative` là tiêu cực cần cảnh báo
- Không đưa comment đã ẩn vào queue
- Không đưa comment đã xóa vào queue
- Không thêm cờ `reviewed`, vì quyết định hiện tại là dashboard chỉ cần action `Ẩn`

## Bảo mật và phân quyền

- dữ liệu queue chỉ trả cho admin
- action hide comment vẫn giữ nguyên guard admin hiện có

## Kiểm thử

### Backend

- test truy vấn chỉ lấy comment `negative`
- test loại trừ comment hidden
- test loại trừ comment deleted
- test sắp xếp mới nhất trước
- test giới hạn số lượng item trả về

### Frontend

- test dashboard render panel negative comments khi có dữ liệu
- test render empty state khi không có dữ liệu
- test bấm `Ẩn bình luận` gọi đúng API và loại item khỏi UI
- test lỗi hide comment không làm mất item

## Rủi ro

- sentiment hiện đang là nhãn đơn giản `negative/normal/positive`, nên có thể có false positive
- vì chưa có trạng thái `dismissed`, comment tiêu cực sẽ tiếp tục hiện cho tới khi admin ẩn nó

## Quyết định chốt

- triển khai moderation queue trực tiếp trên dashboard admin
- chỉ lấy comment `negative`, chưa hidden, chưa deleted
- chỉ cung cấp action nhanh `Ẩn bình luận`
- không đổi schema ở giai đoạn này
