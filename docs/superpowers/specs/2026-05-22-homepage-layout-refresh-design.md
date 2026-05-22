# Homepage Layout Refresh Design

## Goal

Thiết kế lại phần thân trang chủ để bám sát bố cục của ảnh mẫu landing page mà người dùng cung cấp, đồng thời giữ nguyên header và footer hiện tại của hệ thống. Carousel hiện có phải được giữ lại và đặt ngay phía dưới hero.

## Scope

In scope:
- Redesign phần body của `HomePage`
- Hero mới theo layout 2 cột giống ảnh mẫu
- Carousel vẫn tồn tại ngay dưới hero
- Một section nội dung dạng heading giữa trang + grid card bất đối xứng
- Một stats band nền đậm chạy ngang
- Một CTA panel lớn gần cuối trang
- Placeholder media blocks thay cho ảnh thật hoặc SVG minh họa
- Responsive cho desktop và mobile
- Cập nhật test cho homepage

Out of scope:
- Redesign header
- Redesign footer
- Tạo ảnh minh họa thật
- Thay đổi logic carousel hiện có ngoài phần trình bày cần thiết để hòa vào layout mới
- Thay đổi route hoặc data flow backend

## Current Context

Homepage hiện tại dùng các section marketing xếp dọc gồm carousel trước, sau đó là nhiều feature block, stats và CTA. Layout này không giống ảnh mẫu. Ảnh mẫu dùng một landing page tập trung hơn: hero mạnh ở đầu trang, tiếp theo là content grid rõ khối, sau đó là stats band và CTA panel.

Repo hiện đã có:
- `frontend/src/pages/HomePage.jsx`
- `frontend/src/styles/HomePage.module.css`
- `frontend/src/components/sections/CarouselSection.jsx`
- `frontend/src/pages/HomePage.test.jsx`

Hướng redesign nên tận dụng carousel component hiện có thay vì viết lại slider mới.

## Layout Direction

### 1. Header and Footer

Giữ nguyên header và footer hiện tại của hệ thống, không thay đổi markup hay visual language ở hai vùng này.

### 2. Hero Section

Hero là khối đầu tiên của body homepage, bố cục 2 cột trên desktop:
- Cột trái:
  - eyebrow nhỏ
  - headline lớn 2 đến 3 dòng
  - đoạn mô tả ngắn
  - 2 CTA chính
- Cột phải:
  - một media frame lớn đóng vai trò placeholder ảnh
  - có thể có 1 đến 2 accent blocks đơn giản để gợi chiều sâu layout, nhưng không dùng SVG custom

Tone của hero nên sạch, sáng, rõ nhịp giống ảnh mẫu. Phần media chỉ cần là khung ảnh hoặc card placeholder đủ đẹp để người dùng thay ảnh thật sau này.

Trên mobile, hero chuyển thành 1 cột với content trước, media sau.

### 3. Carousel Placement

Carousel phải nằm ngay dưới hero. Đây là ràng buộc bắt buộc từ người dùng.

Carousel vẫn dùng component hiện có nhưng phần spacing, wrapper và visual framing có thể được chỉnh lại để đồng bộ với hero mới. Không đổi bản chất hành vi carousel nếu không cần.

### 4. Content Grid Section

Sau carousel là một section có:
- heading căn giữa
- đoạn mô tả phụ ngắn
- grid card bất đối xứng lấy cảm hứng trực tiếp từ ảnh mẫu

Grid nên có 4 card chính:
- 1 card lớn bên trái hoặc trên cùng để nói về import syllabus / dựng course structure
- 1 card vừa về AI video generation pipeline
- 1 card nhỏ về learner experience hoặc progress visibility
- 1 card ngang về admin workflow / publishing / course operations

Mỗi card cần:
- title rõ
- mô tả ngắn
- visual xử lý bằng shape, badge hoặc placeholder block đơn giản, không cần ảnh thật

Mục tiêu là tạo cảm giác giống bố cục ảnh mẫu mà vẫn nói đúng về VibeCourseAI, không copy text của ảnh.

### 5. Stats Band

Một dải ngang nền tối hoặc nền tương phản mạnh hơn phần còn lại của trang.

Bên trái là intro ngắn cho nhóm số liệu. Bên phải là các stat tiles nhỏ xếp hàng. Số liệu có thể tiếp tục dùng data marketing giả định hiện có, nhưng wording nên bám domain của hệ thống học tập/video course.

### 6. Bottom CTA Panel

Một panel CTA lớn nền xanh sáng gần cuối trang, bám tinh thần ảnh mẫu:
- headline lớn ngắn gọn
- mô tả 1 câu
- 2 CTA

CTA này là khối kêu gọi hành động cuối cùng trước footer.

## Content Mapping

Vì người dùng yêu cầu giữ layout giống ảnh nhưng không yêu cầu copy nội dung, text cần được map lại theo domain của sản phẩm:
- Hero: AI course creation / video learning workflow
- Grid cards: syllabus import, generation pipeline, learner flow, admin operations
- Stats: course count, video generation capacity, workflow visibility, learner completion momentum
- CTA: đăng ký hoặc bắt đầu tạo khóa học

## Visual Rules

- Không redesign header/footer
- Không dùng SVG minh họa tự vẽ
- Các vùng ảnh dùng placeholder frame hoặc simple decorated blocks
- Typography phải rõ cấp bậc, headline lớn, body gọn
- Card cần có border, shadow và spacing rõ để tránh cảm giác phẳng
- Giữ ngôn ngữ thiết kế gần với ảnh mẫu hơn homepage hiện tại
- Không chuyển sang dark mode toàn trang

## Responsive Behavior

Desktop:
- hero 2 cột
- content grid bất đối xứng 2 cột hoặc 12-column feel
- stats band xếp ngang
- CTA panel rộng

Tablet/mobile:
- hero về 1 cột
- carousel full width dưới hero
- content grid xếp 1 cột hoặc 2 cột đơn giản tùy breakpoint
- stats tiles xuống dòng hợp lý
- CTA buttons stack khi thiếu chiều ngang

## Implementation Notes

- `HomePage.jsx` sẽ cần được tổ chức lại section order
- `HomePage.module.css` sẽ là file chính cho redesign này
- `CarouselSection` nên được giữ nguyên logic, chỉ điều chỉnh props/class wrapper nếu cần
- Test homepage cần đổi từ assertions của layout cũ sang layout mới
- Không đụng tới `MainLayout` ngoài việc homepage vẫn render bên trong layout hiện tại

## Testing Expectations

Cần có test cho:
- hero headline và CTA mới xuất hiện
- carousel vẫn tồn tại ngay dưới hero
- content grid section xuất hiện
- bottom CTA mới xuất hiện
- homepage wrapper vẫn dùng dedicated landing styles chứ không rơi về generic section layout

## Risks and Constraints

- Nếu bám ảnh quá sát mà giữ nguyên content cũ, giao diện có thể lệch ngữ cảnh sản phẩm; vì vậy phải map content lại theo domain của repo.
- Nếu thêm quá nhiều visual decoration khi chưa có ảnh thật, layout có thể bị giả tạo; nên ưu tiên placeholder blocks vừa đủ.
- Carousel phải giữ vị trí ngay dưới hero, nên các section khác phải được sắp lại xung quanh ràng buộc này.

## Final Design Decision

Thiết kế được chốt theo các nguyên tắc sau:
- Giữ nguyên header hiện tại
- Giữ nguyên footer hiện tại
- Redesign toàn bộ body homepage theo layout direction của ảnh mẫu
- Carousel nằm ngay dưới hero
- Không dùng SVG minh họa tùy biến
- Các khung ảnh dùng placeholder để người dùng thay bằng ảnh thật sau
- Nội dung text được viết lại cho đúng domain VibeCourseAI thay vì sao chép text trong ảnh mẫu
