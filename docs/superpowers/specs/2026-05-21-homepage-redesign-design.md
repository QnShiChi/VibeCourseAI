# VibeCourseAI Homepage Redesign Design

## Goal

Thiết kế lại trang chủ VibeCourseAI để thể hiện rõ giá trị khác biệt của nền tảng AI-powered course video generation, đồng thời nâng cấp layout toàn hệ thống theo hướng thoáng hơn: `header` full-width, `content container` rộng hơn, và có `footer` hoàn chỉnh.

Scope của spec này bao gồm:

- Redesign `HomePage`
- Tạo các section component mới cho homepage
- Bổ sung `Footer` toàn site
- Mở rộng `content container` toàn hệ thống lên `1360px`
- Chuyển `header` sang full-bleed trên tất cả trang
- `Carousel v1` dùng ảnh hardcode local, chưa đụng backend

Ngoài scope:

- Upload/quản trị ảnh carousel từ admin
- API backend cho carousel
- Localization hoàn chỉnh
- Refactor toàn bộ design system

## Problem Statement

Trang chủ hiện tại quá cơ bản:

- hero quá tĩnh
- phần dưới chỉ có vài card đơn giản
- không kể được câu chuyện sản phẩm từ `syllabus -> AI generation -> audio -> video -> learner experience`
- thiếu visual hierarchy và motion
- container quá hẹp, khiến toàn bộ giao diện có cảm giác bí
- header đang bị ràng buộc bởi container chung thay vì chiếm trọn chiều ngang viewport

Kết quả là homepage chưa tạo được cảm giác một nền tảng công nghệ giáo dục có AI workflow rõ ràng và khác biệt.

## Design Objectives

1. Tạo first impression mạnh hơn về:
   - AI
   - video learning
   - course production workflow
2. Làm homepage “sống” hơn bằng visual sections, carousel, motion nhẹ và CTA rõ ràng.
3. Giữ đúng Brainfish language:
   - sáng
   - playful
   - năng suất
   - chuyên nghiệp
4. Chuẩn hóa layout toàn hệ thống:
   - header full-width
   - content rộng hơn
   - footer hoàn chỉnh
5. Không phụ thuộc backend cho `carousel v1`.

## Users

### Visitor / Learner

Cần thấy ngay:

- sản phẩm này làm gì
- khác gì với LMS thông thường
- có thể học/xem khóa học thế nào
- có động lực bấm `Bắt đầu ngay` hoặc `Xem khóa học`

### Admin / Product Operator

Cần thấy:

- workflow tạo khóa học bằng AI
- khả năng quản trị, generate, theo dõi
- cảm giác nền tảng đủ mạnh để dùng thật

## Information Architecture

Homepage mới sẽ đi theo flow:

1. `Hero`
2. `Showcase Carousel`
3. `Feature Section A` – tạo khóa học từ syllabus
4. `Feature Section B` – AI narration + video pipeline
5. `Feature Section C` – quản trị hệ thống
6. `Feature Section D` – trải nghiệm học tập
7. `Stats Section`
8. `Bottom CTA Section`
9. `Footer`

Thứ tự này được chọn để kể câu chuyện từ:

`value proposition -> visual proof -> capabilities -> credibility -> action`

## Global Layout Changes

### Header

`header` sẽ trở thành full-bleed, tức là nền và border của header chạy trọn chiều ngang viewport trên mọi trang.

Header mới có 2 lớp:

- `outer shell`: full-width
- `inner content`: bọc logo, nav, user actions trong container rộng hơn

Nguyên tắc:

- full-width background
- nav không dính mép màn hình
- giữ alignment ổn định với content body
- dùng chung cho tất cả trang

### Content Container

Container hiện tại khoảng `1120px` là quá hẹp cho visual landing page và cả các màn admin rộng.

Container mới:

- `max-width: 1360px`
- áp dụng toàn hệ thống
- responsive padding theo breakpoint

Đề xuất spacing ngang:

- `320px+`: `16px`
- `768px+`: `24px`
- `1024px+`: `32px`
- `1440px+`: `40px`

Mục tiêu:

- thoáng hơn trên desktop
- không vỡ nhịp trên mobile
- tránh cảm giác “bị bó vào giữa”

### Footer

Footer sẽ là thành phần toàn site, không chỉ riêng homepage.

Footer mới cần:

- chốt trang chắc hơn về mặt thị giác
- tăng cảm giác sản phẩm hoàn chỉnh
- cung cấp điều hướng phụ

Footer sẽ có:

- brand + tagline
- nhóm link chính
- nhóm link admin/learner
- khu vực contact / social placeholder
- copyright line

## Homepage Section Design

### 1. Hero Section

#### Purpose

Tạo ấn tượng đầu tiên rõ ràng về:

- AI-powered course creation
- video learning workflow
- nền tảng all-in-one

#### Layout

Hero 2 cột:

- trái: content
- phải: visual composition

Desktop:

- content chiếm khoảng `55-60%`
- visual chiếm `40-45%`

Mobile:

- stack dọc
- content lên trước
- visual xuống dưới

#### Content

Bao gồm:

- badge ngắn
- headline lớn, rõ về AI + course + video
- đoạn mô tả 2-3 câu
- CTA primary: `Bắt đầu ngay`
- CTA secondary: `Xem khóa học`

#### Visual

Không dùng ảnh thật ở hero.

Visual hero sẽ là tổ hợp UI mock + floating chips:

- main pipeline card
- chips như:
  - `Syllabus`
  - `Lesson Script`
  - `Voiceover`
  - `Video Ready`
- abstract glow / gradient layer

#### Tone

- background dùng `Sky Breeze`
- accent green cho CTA chính
- border/shadow giữ đúng system hiện tại

#### Motion

- stagger reveal cho text
- floating motion rất nhẹ cho chips
- hover lift cho CTA

### 2. Showcase Carousel

#### Purpose

Đây là section ưu tiên cao nhất sau hero, để homepage có visual proof rõ hơn thay vì chỉ là text.

#### Data Source

`Carousel v1` dùng ảnh hardcode local, không gọi backend.

Nguồn ảnh:

- thư mục local trong frontend, ví dụ `frontend/src/assets/images/home-carousel`
- metadata đi kèm trong mảng cấu hình:
  - `id`
  - `image`
  - `title`
  - `caption`
  - `tag`

#### UX

- auto rotate
- previous / next
- dots indicator
- keyboard navigation
- pause on hover
- pause khi tab/browser mất focus nếu cần

#### Layout

- ảnh chính lớn
- overlay caption ở góc dưới
- desktop có thể có thumbnail strip hoặc compact indicators
- mobile giữ ảnh lớn là chính, thumbnail có thể chuyển thành dots

#### Style

- frame border theo accent green
- overlay gradient nhẹ để text dễ đọc
- rounded corners + subtle shadow

#### Accessibility

- button có `aria-label`
- indicator có trạng thái active rõ
- ảnh có `alt`
- keyboard nav qua arrows/tab

### 3. Feature Sections

Tất cả feature section sẽ dùng một component reusable thay vì viết rời từng block.

Component nhận:

- `eyebrow`
- `title`
- `description`
- `bullets`
- `cta`
- `tone`
- `layout`
- `visual`

#### Section A: Tạo khóa học từ syllabus trong 1 nút

Layout:

- content trái
- visual phải

Visual:

- mock upload zone
- file badge
- flow arrow / progress chips

Tone:

- nền sáng, nghiêng về `Honey Dew` hoặc `Canvas White` + texture nhẹ

CTA:

- `Bắt đầu tải đề cương`

#### Section B: AI tự động tạo Video + Narration

Layout:

- visual trái
- content phải

Visual:

- process 3 bước:
  - script
  - narration
  - video

Tone:

- `Highlight Yellow` hoặc `Lime Spritz`

Highlight:

- `Tiết kiệm 90% thời gian sản xuất video`

CTA:

- `Xem demo`

#### Section C: Quản lý, như chuyên gia

Layout:

- content center with strong visual panel

Visual:

- dashboard/admin mockup
- batch generation
- monitoring
- trạng thái jobs

Tone:

- `Mint`

CTA:

- `Truy cập Admin Dashboard`

#### Section D: Trải nghiệm học tập hoàn hảo

Layout:

- learner-centric
- visual + copy cân bằng

Visual:

- learner course card
- progress indicators
- playback/progress metaphors

Tone:

- `Saffron`

CTA:

- `Đồng ý làm học viên`

### 4. Stats Section

#### Purpose

Tăng độ tin cậy và nhịp đọc trước khi xuống CTA cuối.

#### Content

`v1` có thể dùng số hardcode hoặc số derived nhẹ từ app state/config:

- số khóa học
- số video generated
- số learner / workflow metric

#### Interaction

- counter animation nhẹ từ `0 -> target`
- không dùng animation quá phô

#### Style

- tile grid
- nền `Pale Ash`
- cards trắng hoặc pastel nhẹ

### 5. Bottom CTA Section

#### Purpose

Chốt conversion cuối trang.

#### Content

- title mạnh
- tagline ngắn
- 2 CTA:
  - `Đăng ký miễn phí`
  - `Liên hệ`

#### Style

- gradient `Lime Spritz` hoặc `Sky Breeze`
- trọng tâm vào clarity thay vì thêm quá nhiều chi tiết

## Footer Design

Footer áp dụng toàn site.

### Structure

4 vùng:

1. `Brand`
   - logo / wordmark
   - short tagline

2. `Khám phá`
   - Trang chủ
   - Khóa học
   - Dashboard

3. `Nền tảng`
   - Tạo khóa học
   - AI video workflow
   - Trải nghiệm học tập

4. `Liên hệ`
   - email placeholder
   - social links placeholder

### Visual

- nền đậm hơn main body một chút hoặc gradient nhẹ
- text contrast đủ tốt
- spacing rộng
- border-top rõ

### Responsive

- desktop: multi-column
- mobile: stack dọc

## Motion Strategy

Motion phải nhẹ, purposeful, không biến homepage thành “AI slop”.

Áp dụng:

- hero stagger reveal
- feature section fade/slide-in on scroll
- carousel transitions
- hover lift cho button/card nhỏ
- stats counter animation

Yêu cầu:

- duration `0.3s - 0.5s`
- easing `ease-in-out` hoặc cubic-bezier mềm
- tôn trọng `prefers-reduced-motion`

## Accessibility

Homepage phải đạt mức thực dụng gần `WCAG 2.1 AA`.

Yêu cầu:

- alt text cho toàn bộ ảnh carousel
- CTA/button có focus state rõ
- keyboard support cho carousel
- contrast đủ cho overlay text
- semantic headings đúng thứ tự
- motion giảm hoặc tắt khi `prefers-reduced-motion`

## Performance

### Constraints

- ảnh carousel phải tối ưu kích thước
- tránh JS animation nặng
- ưu tiên CSS transform/opacity
- không lạm dụng library nếu custom code đơn giản đủ dùng

### `v1` Recommendation

- tự build carousel hook/component thay vì kéo thêm `Swiper` ngay
- dùng image set nhỏ, chủ động kiểm soát load tốt hơn

## Technical Design

## Frontend Files

### New / Updated Files

- Update: `frontend/src/pages/HomePage.jsx`
- Create: `frontend/src/components/sections/HeroSection.jsx`
- Create: `frontend/src/components/sections/CarouselSection.jsx`
- Create: `frontend/src/components/sections/FeatureSection.jsx`
- Create: `frontend/src/components/sections/StatsSection.jsx`
- Create: `frontend/src/components/layout/Footer.jsx`
- Create: `frontend/src/hooks/useCarousel.js`
- Create: `frontend/src/data/homeCarousel.js`
- Create: `frontend/src/styles/HomePage.module.css`

### Likely shared layout/style files

- Modify: `frontend/src/components/layout/MainLayout.jsx`
- Modify: `frontend/src/styles/theme.css`

## Component Responsibilities

### `HomePage.jsx`

- compose sections
- map config data into reusable sections
- keep page orchestration thin

### `HeroSection.jsx`

- render hero content and hero visual
- no global layout responsibility

### `CarouselSection.jsx`

- render carousel shell
- wire controls to `useCarousel`
- manage accessibility attributes

### `FeatureSection.jsx`

- reusable alternating section template
- support tone/layout variants

### `StatsSection.jsx`

- render stats grid
- handle simple in-view counter animation

### `Footer.jsx`

- site-wide footer
- reused in main layout

### `useCarousel.js`

- active slide state
- auto-rotate logic
- previous/next actions
- pause behavior

## Layout Integration

`MainLayout` sẽ cần tách rõ:

- `header shell` full-width
- `page main` vẫn bọc bởi content container mới
- `footer` full-width nhưng có inner container

Điều này cho phép:

- header/footer chạy full width
- body content rộng hơn nhưng vẫn canh đều

## Visual Language Rules

Để tránh rơi về homepage “template chung chung”, các quy tắc sau cần giữ:

- không dùng card grid lặp lại một kiểu cho toàn bộ section
- có nhịp thay đổi layout trái-phải-center
- mỗi section có một visual identity riêng
- gradient chỉ dùng có chủ đích, không phủ kín mọi nơi
- typography phải bold, rõ, hơi playful nhưng vẫn chuyên nghiệp

## Responsive Rules

### Mobile (`320px+`)

- hero stack dọc
- carousel full width
- feature sections stack dọc
- stats grid 1-2 cột
- footer stack dọc

### Tablet (`768px+`)

- hero vẫn ưu tiên stack hoặc 2 cột mềm
- feature sections bắt đầu có stagger
- stats 2-3 cột

### Desktop (`1024px+`)

- hero 2 cột đầy đủ
- carousel có caption thoải mái hơn
- feature sections alternating rõ

### Wide (`1440px+`)

- container vẫn cap ở `1360px`
- tăng breathing room, không kéo content quá dài

## Success Criteria

Redesign được coi là đạt nếu:

- homepage tạo cảm giác mạnh hơn về AI + video + learning
- visual hierarchy rõ
- carousel hoạt động mượt, accessible
- layout toàn site thoáng hơn nhờ container `1360px`
- header full-width trên mọi trang
- footer hiện diện ổn định trên toàn site
- code chia component rõ, không dồn logic vào `HomePage.jsx`

## Risks

1. Nếu animation quá nhiều, homepage sẽ mất cảm giác “productive”.
2. Nếu container mở rộng nhưng spacing trong card/section không chỉnh theo, giao diện sẽ bị rỗng thay vì thoáng.
3. Nếu carousel hardcode ảnh quá nặng, LCP sẽ xấu.
4. Nếu header full-width nhưng inner alignment không đúng, các trang cũ sẽ lệch nhịp.

## Decisions Locked In

- `Carousel v1` frontend-only, dùng local images hardcode
- không làm backend carousel ở giai đoạn này
- `content container max-width = 1360px`
- `header` full-width toàn site
- `footer` toàn site
- homepage flow:
  - hero
  - carousel
  - 4 feature sections
  - stats
  - bottom CTA
  - footer
