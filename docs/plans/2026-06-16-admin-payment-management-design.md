# Admin Payment Management Design

## Goal

Thêm một khu vực `Quản lý hóa đơn` riêng trong sidebar admin để admin có thể tra soát chi tiết các đơn thanh toán, độc lập với màn `Báo cáo hệ thống`.

Scope bản đầu:

- thêm mục sidebar `Quản lý hóa đơn`
- thêm route danh sách `/admin/payments`
- thêm route chi tiết `/admin/payments/:id`
- danh sách lấy hóa đơn làm trọng tâm
- hiển thị đầy đủ trạng thái: `Pending`, `Paid`, `LatePaid`, `Expired`, `Failed`
- trang chi tiết chỉ phục vụ tra soát, chưa hỗ trợ đổi trạng thái thủ công

Ngoài scope bản đầu:

- export CSV/Excel
- bulk actions
- manual override trạng thái
- webhook/event timeline nâng cao
- audit log cho thao tác admin

## Navigation

Sidebar admin giữ nguyên mục `Báo cáo hệ thống` và thêm một mục mới:

- label: `Quản lý hóa đơn`
- route: `/admin/payments`
- placement: ngay sau `Báo cáo hệ thống` hoặc cạnh cụm admin/reporting hiện có

Lý do:

- `Báo cáo hệ thống` là nhu cầu tổng quan
- `Quản lý hóa đơn` là nhu cầu vận hành và tra soát từng đơn
- tách hai mục giúp người dùng admin hiểu rõ ý nghĩa từng màn

## Information Architecture

### 1. Payment List Page

Route: `/admin/payments`

Mục tiêu:

- cho admin tìm nhanh hóa đơn theo mã đơn hoặc người mua
- lọc theo trạng thái
- xem các mốc thời gian chính
- đi vào chi tiết từng hóa đơn

Header:

- title: `Quản lý hóa đơn`
- description: `Tra soát trạng thái thanh toán, người mua và thời gian xử lý của từng đơn hàng.`

Toolbar:

- ô tìm kiếm theo `orderCode`, `userFullName`, `userEmail`
- filter trạng thái
- filter khoảng ngày
- nút `Làm mới dữ liệu`

Bảng danh sách:

- Mã đơn
- Người mua
- Khóa học
- Số tiền
- Trạng thái
- Tạo lúc
- Thanh toán lúc
- Hành động: `Xem chi tiết`

Sort mặc định:

- mới nhất trước
- ưu tiên `PaidAt ?? CreatedAt` giảm dần

Empty state:

- `Không có hóa đơn nào khớp bộ lọc hiện tại.`

Error state:

- alert lỗi ở phía trên bảng
- giữ toolbar để admin có thể đổi filter hoặc tải lại

### 2. Payment Detail Page

Route: `/admin/payments/:id`

Mục tiêu:

- hiển thị đầy đủ thông tin một hóa đơn
- cho admin copy thông tin quan trọng để tra soát
- cung cấp ngữ cảnh rõ ràng về trạng thái và timeline

Khối chính:

- Mã đơn
- Trạng thái
- Số tiền
- Người mua
- Email
- Khóa học

Khối thanh toán:

- Ngân hàng
- Số tài khoản
- Chủ tài khoản
- Nội dung chuyển khoản
- SePay transaction id nếu có

Khối thời gian:

- Tạo lúc
- Hết hạn lúc
- Thanh toán lúc

Hành động nhanh:

- quay lại danh sách
- copy mã đơn
- copy nội dung chuyển khoản
- mở khóa học liên quan nếu cần

## Data Flow

Không tái sử dụng payload dashboard cho màn quản lý chi tiết. Tạo API admin riêng để màn này không bị phụ thuộc vào logic rút gọn của dashboard.

### Suggested APIs

#### List

`GET /api/admin/payment-orders`

Query params:

- `query`
- `status`
- `dateFrom`
- `dateTo`
- `page`
- `pageSize`

Response item fields:

- `paymentOrderId`
- `orderCode`
- `userId`
- `userFullName`
- `userEmail`
- `courseId`
- `courseTitle`
- `amount`
- `status`
- `createdAt`
- `expiresAt`
- `paidAt`

#### Detail

`GET /api/admin/payment-orders/{id}`

Response fields:

- toàn bộ field của list item
- `bankCode`
- `bankName`
- `bankAccountNumber`
- `accountHolderName`
- `transferContent`
- `sepayTransactionId`

## Timezone Rules

Toàn bộ thời gian trong DB và API phải được chuẩn hóa ở UTC.

Frontend admin format thời gian cố định theo:

- locale: `vi-VN`
- timezone: `Asia/Ho_Chi_Minh`

Lý do:

- tránh lệch theo timezone của máy client
- đồng bộ với thực tế vận hành tại Việt Nam
- khắc phục triệt để nhóm lỗi vừa xuất hiện ở dashboard payment overview

## Status Rules

Màn `Quản lý hóa đơn` phải nhìn thấy đủ trạng thái:

- `Pending`
- `Paid`
- `LatePaid`
- `Expired`
- `Failed`

Badge mapping nên giữ nhất quán với phần payment trên dashboard:

- `Pending`: vàng
- `Paid` / `LatePaid`: xanh lá
- `Expired` / `Failed`: đỏ

Dashboard có thể ẩn `Pending` để gọn hơn.

Màn quản lý chi tiết thì không được ẩn `Pending`, vì đây là thông tin vận hành quan trọng.

## UI Behavior

### List behavior

- thay đổi filter -> refetch danh sách
- clear filter -> quay về danh sách mặc định
- click dòng hoặc nút `Xem chi tiết` -> vào `/admin/payments/:id`

### Detail behavior

- load trực tiếp theo `id`, không phụ thuộc dữ liệu list
- nếu không tìm thấy hóa đơn -> hiển thị not found state
- nếu API lỗi -> hiển thị alert lỗi + nút tải lại

## Error Handling

List page:

- API lỗi: `Không thể tải danh sách hóa đơn.`
- giữ lại filter hiện tại

Detail page:

- API lỗi: `Không thể tải chi tiết hóa đơn.`
- nếu `404`: `Hóa đơn không tồn tại hoặc đã bị xóa.`

## Testing

Frontend:

- route `/admin/payments` render đúng
- sidebar có mục `Quản lý hóa đơn`
- filter đổi trạng thái trigger fetch đúng
- `Pending` hiển thị ở payment management list
- click `Xem chi tiết` điều hướng đúng
- detail page render đúng thông tin chính
- formatter thời gian dùng `Asia/Ho_Chi_Minh`

Backend:

- list API filter theo status đúng
- list API filter theo query đúng
- detail API trả đủ field
- sort mới nhất trước đúng
- thời gian serialize đúng UTC source và không bị lệch 7 giờ ở frontend

## Recommended Implementation Order

1. thêm route và nav item sidebar
2. thêm admin payment list API
3. dựng `AdminPaymentsPage`
4. thêm admin payment detail API
5. dựng `AdminPaymentDetailPage`
6. thêm test frontend/backend
7. polish trạng thái, empty/error states

## Notes

- không đổi hoặc gộp vào `Báo cáo hệ thống`
- không thêm edit status bằng tay ở bản đầu
- không dùng modal/drawer cho chi tiết ở bản đầu
- route chi tiết riêng là lựa chọn bền hơn cho scope payment sau này
