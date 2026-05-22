# Courses Page Redesign And Course Presentation Design

## Goal

Thiết kế lại trang `/courses` để bám sát layout của ảnh mẫu do người dùng cung cấp, đồng thời thay thế thumbnail hardcoded/fake bằng dữ liệu thật được admin quản lý từ trang course admin. Phần category cũng phải trở thành dữ liệu thật của `Course` để search/filter trên `/courses` dựa vào backend data chứ không map tạm ở frontend.

## Scope

In scope:
- Redesign body của trang `/courses` theo visual direction của ảnh mẫu
- Search theo title và description của course
- Filter chip theo category thật của course
- Featured course card + grid course cards dùng dữ liệu thật
- Admin quản lý `thumbnail` và `category` trong `/admin/courses/:courseId`
- Backend/API trả `thumbnailUrl` và `category` trong course list payload
- File upload flow cho course thumbnail
- Frontend/backend tests cho luồng mới

Out of scope:
- Redesign header/footer hệ thống
- Thay đổi luồng học `/courses/:courseId/learn`
- Tạo AI-generated thumbnails
- Tạo taxonomy category phức tạp nhiều cấp
- Search/filter server-side trong iteration này

## Current Context

`/courses` hiện tại là một grid card tương đối đơn giản trong `frontend/src/pages/CoursesPage.jsx`. Trang này dùng:
- `getAdminCourses()` cho admin
- `getPublishedCourses()` cho learner

Hiện tại course cards dùng cover giả lập bằng khối gradient cứng trong frontend thay vì ảnh thật. Backend model `Course` đã có sẵn trường `ThumbnailUrl`, nhưng DTO response và admin UI hiện chưa khai thác trường này. `Category` vẫn chưa tồn tại trên `Course`.

Trang admin quản lý course đang nằm ở `/admin/courses/:courseId` với file chính là `frontend/src/pages/CourseStructurePage.jsx`. Đây là nơi phù hợp nhất để admin gắn category và thumbnail cho course.

## Product Intent

Người dùng muốn `/courses` trông giống ảnh mẫu: một discovery page có hierarchy rõ ràng hơn, nhiều cảm giác editorial hơn thay vì chỉ là “list card”. Đồng thời người dùng không muốn course thumbnail tiếp tục là visual hardcoded; admin phải có khả năng upload thumbnail thật từ trang quản lý course.

## Data Model Changes

### 1. Course Category

Thêm trường `Category` thật vào `Course`.

Thiết kế đề xuất:
- lưu dưới dạng string hoặc enum-backed string dễ expose ra API
- các giá trị ban đầu bám theo ảnh mẫu và use case hiện tại:
  - `All` không phải dữ liệu lưu trong DB, chỉ là filter tổng hợp ở frontend
  - `UiUxDesign`
  - `AiAndData`
  - `Development`

Khuyến nghị dùng enum ở backend + string response ở API để tránh typo nội bộ và vẫn dễ dùng ở frontend.

### 2. Course Thumbnail

Không cần thêm trường mới vì `Course.ThumbnailUrl` đã tồn tại. Tuy nhiên cần chuẩn hóa cách dùng:
- `ThumbnailUrl` là URL hoặc path public mà frontend có thể render trực tiếp
- Nếu chưa có thumbnail, response vẫn trả `null`

## API Design

### 1. Course List Responses

Cập nhật cả:
- `AdminCourseListItemResponse`
- `PublishedCourseListItemResponse`

Để trả thêm:
- `thumbnailUrl`
- `category`

Như vậy `/courses` có đủ dữ liệu để render hoàn chỉnh mà không phải fetch chi tiết từng course.

### 2. Course Structure / Admin Detail Response

Payload dùng cho `/admin/courses/:courseId` cũng cần trả về:
- `thumbnailUrl`
- `category`

Admin page cần biết dữ liệu hiện tại để render preview và form edit.

### 3. Admin Update Endpoints

Thêm endpoint để admin cập nhật presentation metadata cho course. Có 2 hướng hợp lý:

Option A:
- một endpoint upload thumbnail riêng
- một endpoint update category riêng

Option B:
- một endpoint upload/update course presentation gồm cả category + thumbnail nếu có file

Khuyến nghị iteration này:
- endpoint upload thumbnail riêng
- endpoint update category riêng

Lý do:
- đơn giản hơn để test
- tránh payload multipart phức tạp cho cả metadata + file cùng lúc
- dễ retry từng phần trong admin UI

### 4. Thumbnail Storage

Thumbnail file nên được lưu trong storage nội bộ của hệ thống tương tự cách repo đang xử lý file học liệu. Backend chịu trách nhiệm:
- validate file là ảnh hợp lệ
- giới hạn kích thước cơ bản
- sinh tên file an toàn
- trả path/URL public về `ThumbnailUrl`

## Courses Page Layout

Giữ nguyên header/footer hiện tại. Chỉ redesign phần body của `/courses`.

### 1. Hero Discovery Header

Phần đầu trang gồm:
- heading lớn theo tinh thần ảnh mẫu
- mô tả ngắn
- search bar lớn ở giữa
- row chip filter category phía dưới

Search là local client-side search trên danh sách course đã tải.

### 2. Filter Model

Frontend state:
- `searchTerm`
- `activeCategory`

Behavior:
- `All Courses` hiển thị toàn bộ
- chip category lọc theo `course.category`
- search tiếp tục lọc trên tập kết quả sau category filter hoặc ngược lại; miễn giao logic là AND
- match trên `title` và `description`, case-insensitive

### 3. Featured Card

Course đầu tiên trong tập kết quả đã lọc sẽ trở thành featured card lớn.

Card này cần:
- thumbnail lớn
- category badge / state badge nếu cần
- metadata ngắn (module, lesson, hoặc publish state với admin)
- title
- description
- CTA chính
- admin action phụ nếu đang là admin

Nếu không có course nào, featured block không hiện; thay bằng empty state đồng nhất với layout mới.

### 4. Course Grid

Các course còn lại render thành card grid bên dưới.

Mỗi card có:
- thumbnail thật nếu có
- fallback visual nhẹ nếu chưa có thumbnail
- title
- mô tả ngắn
- module/lesson counts
- instructor-like footer không dùng vì hiện không có dữ liệu instructor thật; thay bằng metadata thật của hệ thống hoặc bỏ hẳn
- CTA học/xem course
- admin publish/unpublish button nếu phù hợp

### 5. Empty State

Nếu search/filter không ra kết quả:
- giữ search bar và chip filter visible
- hiển thị empty state theo visual style mới
- có CTA reset filter/search

## Admin Course Page Changes

### 1. Presentation Block

Trong `/admin/courses/:courseId`, thêm một block quản lý presentation metadata với:
- thumbnail preview hiện tại
- file input upload thumbnail mới
- category select
- nút lưu category
- nút upload/update thumbnail
- tùy chọn xóa thumbnail nếu muốn hỗ trợ ngay iteration này

### 2. UX Requirements

- admin phải thấy ngay thumbnail hiện tại nếu có
- upload thành công thì preview cập nhật ngay
- category hiện tại phải reflect đúng từ backend
- lỗi upload/category update cần có message rõ ràng
- không được làm hỏng các action generate lesson/audio/video hiện có trên cùng trang

## Visual Direction For `/courses`

Bám sát ảnh mẫu ở mức layout direction, không cần copy nguyên văn nội dung:
- nền sáng hơi ngả vàng/xanh nhẹ
- heading lớn center-aligned
- search bar lớn có border rõ
- chip filter nhiều màu nhẹ
- featured card lớn ở hàng đầu
- card grid dưới có border đậm, shadow nhẹ, cảm giác editorial/productized
- thumbnail thật là yếu tố chính của card, không dùng cover gradient cứng như hiện tại

## Frontend File Impact

Likely files:
- `frontend/src/pages/CoursesPage.jsx`
- `frontend/src/pages/CoursesPage.test.jsx` hoặc test file tương ứng nếu chưa có
- `frontend/src/styles/...` hoặc style module mới cho courses page
- `frontend/src/pages/CourseStructurePage.jsx`
- `frontend/src/api/courseService.js`

## Backend File Impact

Likely files:
- `backend/CourseVideo.API/Models/Course.cs`
- DTOs course list / course structure
- `CoursesController`
- `CourseService`
- repository queries if projection changes are needed
- migration for `Category`
- storage/service code for thumbnail upload handling

## Testing Expectations

Backend tests:
- course list responses include `thumbnailUrl` and `category`
- admin can update category
- admin can upload thumbnail
- invalid file upload rejected properly

Frontend tests:
- `/courses` renders hero/search/filter layout
- search filters by title/description
- category chips filter correctly
- featured course uses first filtered item
- course cards render thumbnail image when available
- admin course page shows presentation block and updates category/thumbnail actions correctly

## Risks And Constraints

- Search/filter is client-side, so performance depends on list size; acceptable for current scope
- Category taxonomy needs to stay small for now; otherwise UI and backend governance get more complex quickly
- Thumbnail upload must not conflict with any existing storage conventions in the repo
- `/admin/courses/:courseId` is already a heavy page, so the new presentation block should be isolated and not tangle with generation logic

## Final Design Decision

Thiết kế được chốt theo các nguyên tắc sau:
- `/courses` được redesign theo layout direction của ảnh mẫu
- search và filter hoạt động thật trên dữ liệu course thật
- `category` là dữ liệu thật trong backend
- `thumbnail` là dữ liệu thật do admin upload từ trang quản lý course
- course cards không còn phụ thuộc vào visual hardcoded/gradient cứng như hiện tại
- header/footer giữ nguyên hệ thống hiện tại
