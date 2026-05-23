# Course Learn Page Redesign

Date: 2026-05-23

## Goal

Thiết kế lại trang `frontend/src/pages/CourseLearnPage.jsx` để bám sát mockup trang học mới, đồng thời giữ lại đầy đủ hành vi học tập hiện có:

- chọn bài học từ sidebar
- phát video nếu lesson có `videoUrl`
- hiển thị trạng thái placeholder nếu lesson chưa có video
- hiển thị bình luận theo lesson hiện tại
- điều hướng bài trước và bài tiếp theo
- hiển thị tiến độ học tập theo lesson đang chọn

Trang mới cũng phải giải quyết vấn đề sidebar quá dài bằng cách giữ sidebar vừa trong viewport và chỉ cuộn nội bộ phần danh sách module/bài học.

## Scope

Trong phạm vi thay đổi:

- cập nhật layout và visual treatment của trang học để khớp mockup
- bổ sung logic dẫn xuất tiến độ và điều hướng trước/sau từ dữ liệu `modules`
- giữ nguyên API `getCourseLearnPayload(courseId)`
- cập nhật test của trang học để phản ánh layout và hành vi mới

Ngoài phạm vi thay đổi:

- không thay đổi schema backend
- không thêm cơ chế khóa lesson mới nếu payload chưa hỗ trợ
- không thay đổi hệ thống comment backend
- không thay đổi global site header ngoài những gì trang học đang kế thừa

## Existing Context

Trang hiện tại đã có:

- gọi API `getCourseLearnPayload(courseId)`
- state cho `course`, `selectedLessonId`, `expandedModules`, `isLoading`, `errorMessage`
- render video hoặc placeholder
- component `LessonComments`
- accordion module cơ bản ở sidebar

Vấn đề của trạng thái hiện tại:

- bố cục và thứ bậc thị giác chưa khớp mockup
- sidebar kéo dài theo số module thay vì ở gọn trong viewport
- chưa có footer điều hướng bài trước/sau
- chưa có chỉ báo tiến độ học tập rõ ràng theo bài đang học
- mobile chưa có cấu trúc ưu tiên việc xem bài trước rồi mới duyệt syllabus

## Recommended Approach

Chọn phương án desktop hai cột bám sát mockup:

- cột trái là khu vực học chính
- cột phải là sidebar sticky theo viewport
- phần đầu sidebar chứa tiêu đề và tiến độ
- phần danh sách module là vùng cuộn nội bộ

Trên mobile:

- chuyển sang một cột
- ưu tiên player, tiêu đề bài, mô tả, nội dung và bình luận trước
- sidebar nội dung khóa học được đặt sau phần bài học
- sticky sidebar bị tắt trên mobile để tránh chiếm không gian nhìn

Lý do chọn:

- khớp trực tiếp với mockup
- phù hợp hành vi học tập thực tế, nơi người dùng xem video và phần mô tả trước, sau đó mới mở danh sách bài
- xử lý triệt để vấn đề nhiều module bằng internal scroll

## Information Architecture

### Desktop Layout

Trang học được chia thành 2 cột:

1. Main column
   - tiêu đề khóa học
   - card player
   - tiêu đề bài học hiện tại và mô tả
   - card nội dung bài học
   - card bình luận
   - thanh điều hướng bài trước/sau
2. Sidebar column
   - tiêu đề `Nội dung khóa học`
   - thanh tiến độ + phần trăm
   - accordion module
   - lesson list trong từng module

### Mobile Layout

Thứ tự hiển thị:

1. tiêu đề khóa học
2. player
3. tiêu đề bài học + mô tả
4. nội dung bài học
5. sidebar nội dung khóa học
6. bình luận
7. điều hướng bài trước/sau

Sidebar trên mobile vẫn có `max-height` cho phần module list, nhưng không sticky.

## UI Specification

### Course Header

- Dùng tiêu đề lớn, uppercase, đậm, bám tinh thần mockup
- Giữ dữ liệu từ `course.courseTitle`
- Dòng mô tả phụ dùng `course.courseDescription`

### Player Card

- Card sáng, viền đen, shadow lệch nhẹ theo design language hiện có của dự án
- Vùng media riêng nền tối
- Nếu có `selectedLesson.videoUrl`, render `<video controls preload="metadata">`
- Nếu không có `videoUrl`, render placeholder trong cùng một khung player
- Placeholder hiển thị:
  - tên lesson
  - trạng thái `đang chuẩn bị video` hoặc `video lỗi`

### Current Lesson Summary

- Tiêu đề lesson ngay dưới player
- Mô tả lesson là `selectedLesson.description`
- Nếu cần hiển thị nhãn lesson hiện tại, dùng badge nhỏ trong card player hoặc phía trên title

### Lesson Content Card

- Khối `Nội dung bài học`
- Hiển thị `selectedLesson.contentSeed`
- Nếu nội dung dài, card vẫn dùng scroll tự nhiên của trang, không tạo nested scroll không cần thiết

### Comments Card

- Giữ `LessonComments`
- Tất cả hành vi comment phụ thuộc `lessonId` đang chọn
- Visual cập nhật để hòa vào layout mới nhưng không thay đổi contract với component comment

### Progress Panel

- Tiến độ tính từ vị trí của lesson đang chọn trên tổng số lesson trong toàn khóa
- Công thức:
  `progressPercent = round(((currentLessonIndex + 1) / totalLessons) * 100)`
- Hiển thị dạng thanh ngang và text phần trăm
- Nếu khóa học không có lesson, tiến độ bằng `0%`

### Module Accordion

- Mỗi module là một card accordion
- Header module hiển thị:
  - số thứ tự module
  - tên module
  - biểu tượng expand/collapse
- Module chứa `selectedLessonId` tự mở
- Khi chọn lesson, module cha luôn được giữ mở

### Lesson Item

- Hiển thị:
  - số thứ tự lesson
  - tiêu đề lesson
  - duration nếu payload có trong tương lai; hiện tại không phụ thuộc field này
- Lesson active có trạng thái nổi bật mạnh hơn
- Nếu tương lai payload có field khóa bài học, cấu trúc class và layout phải đủ để thêm icon khóa mà không cần refactor lớn

### Previous/Next Navigation

- Thanh điều hướng ở cuối cột trái
- Nút trái: `Bài trước`
- Nút phải: `Tiếp tục bài học`
- Ở giữa hiển thị lesson hiện tại
- Nếu đang ở bài đầu hoặc bài cuối, nút tương ứng bị disabled

## Data Flow

Tiếp tục dùng `getCourseLearnPayload(courseId)` làm nguồn dữ liệu duy nhất.

State local của page:

- `course`
- `selectedLessonId`
- `expandedModules`
- `isLoading`
- `errorMessage`

Derived data:

- `selectedLesson`
- `selectedModule`
- `flatLessons`
- `currentLessonIndex`
- `totalLessons`
- `progressPercent`
- `previousLesson`
- `nextLesson`

Luồng tương tác:

1. Trang tải payload từ API
2. Chọn `selectedLessonId` mặc định từ payload
3. Mở module chứa lesson đó
4. Khi user click lesson khác:
   - cập nhật `selectedLessonId`
   - mở module chứa lesson vừa chọn
   - rerender player, summary, content, comments, progress, nav
5. Khi user bấm `Bài trước` hoặc `Tiếp tục bài học`:
   - tìm lesson liền trước hoặc liền sau trong `flatLessons`
   - cập nhật `selectedLessonId`
   - đảm bảo module cha mở ra nếu lesson mới thuộc module khác

## Sidebar Height and Scrolling

Desktop sidebar phải thỏa các nguyên tắc sau:

- sidebar sticky theo viewport, dùng `top` offset phù hợp với site header hiện có
- chiều cao hiệu dụng của panel không vượt quá viewport trừ đi khoảng đệm trên/dưới
- panel chia thành:
  - phần đầu cố định: title + progress
  - phần list cuộn nội bộ

Thực thi CSS dự kiến:

- outer sidebar dùng `position: sticky`
- inner panel dùng `display: grid`
- panel height dùng dạng `calc(100vh - offset)`
- module list dùng `overflow-y: auto`

Kết quả mong muốn:

- toàn bộ trang vẫn cuộn bình thường theo nội dung trái
- sidebar không kéo dài vô hạn
- khi module nhiều, chỉ vùng module list có scrollbar

## Responsive Behavior

### Desktop

- giữ hai cột
- sidebar sticky
- module list scroll nội bộ

### Tablet and Mobile

- chuyển một cột
- sidebar không sticky
- module list có `max-height` hợp lý để tránh chiếm toàn màn hình
- thứ tự nội dung ưu tiên xem bài trước

## Error and Empty States

- Loading:
  - hiển thị card loading đơn giản
- API error:
  - hiển thị thông báo `Không thể tải trang học của khóa học này.`
- Không có lesson:
  - hiển thị empty state gọn
- Lesson không có video:
  - hiển thị placeholder trong media frame
- Video failed:
  - placeholder hiển thị trạng thái lỗi rõ ràng

## Testing Strategy

Cập nhật `frontend/src/pages/CourseLearnPage.test.jsx` để kiểm tra:

- render lesson mặc định từ payload
- đổi lesson từ sidebar cập nhật nội dung chính
- render video player khi có `videoUrl`
- comment render theo `lessonId` đang chọn
- điều hướng `Bài trước` và `Tiếp tục bài học` cập nhật lesson đúng
- panel sidebar hiển thị tiêu đề và text tiến độ
- module chứa lesson hiện tại được mở mặc định

Không cần snapshot test. Ưu tiên test theo hành vi và khả năng truy cập bằng role/text.

## Implementation Notes

- Giữ nguyên API service hiện có
- Có thể tách helper nhỏ trong page hoặc file util nếu logic dẫn xuất lesson index và navigation làm component quá nặng
- Không đụng tới các file user đang sửa ngoài phạm vi trang học, test trang học và style liên quan
- Nếu theme hiện tại trong `theme.css` gây khó bảo trì, có thể thêm một block style riêng, nhưng vẫn ưu tiên tái sử dụng token đang có

## Acceptance Criteria

- Trang `courses/:courseId/learn` có bố cục mới gần với mockup đã duyệt
- Sidebar desktop nằm gọn trong viewport và chỉ cuộn phần danh sách module
- Chọn lesson vẫn cập nhật video, mô tả, nội dung và bình luận
- Có tiến độ học tập hiển thị theo lesson đang chọn
- Có điều hướng bài trước và bài tiếp theo
- Mobile vẫn dùng được, ưu tiên nội dung học chính trước sidebar
- Test của trang học phản ánh hành vi mới và chạy pass

## Risks

- Nếu `LessonComments` có layout cứng, phần visual mới có thể cần thêm wrapper CSS để không phá nhịp của trang
- Nếu site header cao hơn dự đoán, sticky offset sidebar cần tinh chỉnh sau khi chạy thực tế
- Nếu payload về sau thay đổi thứ tự lesson không ổn định, logic `flatLessons` phải luôn dựa vào `orderIndex` rõ ràng

## Open Decisions Resolved

- Chọn mobile layout ưu tiên player và nội dung bài trước sidebar
- Chọn sidebar desktop sticky với internal scroll cho module list
- Chọn điều hướng lesson theo thứ tự phẳng toàn khóa, không giới hạn trong một module
