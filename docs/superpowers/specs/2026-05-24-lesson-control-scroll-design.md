# Lesson control scroll interaction design

## Summary
Khi admin bấm nút `Điều khiển` ở một lesson card trong danh sách bên dưới, trang phải đưa người dùng lên bảng điều khiển lesson tập trung ở phía trên và đồng bộ lesson vừa chọn vào panel đó.

## Goals
- Giảm thao tác tay khi admin muốn điều khiển lesson từ danh sách lesson.
- Làm rõ lesson nào đang được điều khiển sau khi bấm nút.
- Giữ thay đổi nhỏ, không làm lại layout hiện có.

## Non-goals
- Không thay đổi cấu trúc tổng thể của `CourseStructurePage`.
- Không đổi logic generate audio/video/content hiện có.
- Không thêm tab mới hoặc thay đổi thứ tự tab.

## UX flow
1. Admin bấm `Điều khiển` tại một lesson card.
2. Hệ thống đặt `selectedLessonId` thành lesson tương ứng.
3. Trang cuộn mượt lên phần `#centralized-lesson-action-panel`.
4. Panel tạm thời được highlight để người dùng nhận ra vùng vừa được điều khiển.
5. Panel hiển thị đúng lesson đã chọn; tab hiện tại được giữ nguyên.

## Component impact
### `frontend/src/pages/CourseStructurePage.jsx`
- Giữ `handleControlLesson(lessonId)` làm entry point cho interaction này.
- Bổ sung state ngắn hạn để bật/tắt trạng thái focus của panel.
- Sau khi chọn lesson, gọi scroll tới panel và kích hoạt highlight tạm thời.
- Không reset `activeTab` khi điều khiển lesson từ card.

### `frontend/src/styles/theme.css`
- Dùng hoặc hoàn thiện class focus cho centralized panel để tạo hiệu ứng highlight ngắn.
- Hiệu ứng cần đủ rõ để thấy thay đổi nhưng không gây khó chịu.

### `frontend/src/pages/CourseStructurePage.test.jsx`
- Thêm test cho hành vi bấm `Điều khiển`.
- Xác nhận lesson được chọn trong panel sau khi click.
- Xác nhận có gọi scroll tới panel bằng cách mock `scrollIntoView`.

## Data flow
- Lesson card phát sự kiện click.
- `handleControlLesson` cập nhật selection state.
- Render kế tiếp làm panel hiển thị lesson mới.
- DOM panel nhận hành vi scroll và class highlight tạm thời.

## Error handling
- Nếu panel element không tìm thấy trong DOM, vẫn chọn lesson bình thường và không crash.
- Nếu lesson không hợp lệ, panel tiếp tục giữ behavior hiện tại.

## Testing
- Viết test trước cho case click nút `Điều khiển`.
- Verify test fail đúng vì chưa có đầy đủ behavior mong muốn.
- Sau đó cập nhật implementation tối thiểu để test pass.
- Chạy lại test của file `CourseStructurePage.test.jsx`.

## Acceptance criteria
- Bấm `Điều khiển` từ một lesson card làm panel phía trên cuộn vào tầm nhìn.
- Lesson vừa bấm trở thành lesson đang được điều khiển trong panel.
- UI thể hiện rõ panel đã được focus bằng highlight ngắn hạn.
- Không làm hỏng các thao tác lesson khác đang có trên trang.
