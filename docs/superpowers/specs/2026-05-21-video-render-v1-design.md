# Video Render V1 Design

Date: 2026-05-21

## Summary

Mục tiêu của `video render v1` là biến lesson đã có `slides + audio` thành `video MP4` mà learner có thể xem trực tiếp trên trang học. Kiến trúc được chốt theo hướng:

- `ASP.NET Core Web API` là lớp điều phối trung tâm
- `video-worker` là executor chuyên render video
- `ai-worker` tiếp tục phụ trách AI/TTS, không ôm thêm render video

Điểm quan trọng của version này là giữ rõ trọng tâm đồ án ở phía `ASP.NET Core`:

- quản lý domain `course / module / lesson`
- kiểm tra điều kiện render
- tạo và theo dõi background jobs
- cập nhật trạng thái render video
- phục vụ API cho admin và learner

## Goals

- Render được `1 video MP4` cho mỗi lesson từ `slideOutlineJson + audioUrl + audioSegmentsJson`
- Hỗ trợ cả `generate video cho 1 lesson` và `generate video cho toàn course`
- Hiển thị progress và trạng thái lỗi cho admin giống luồng generate content/audio
- Khi learner vào trang học, nếu lesson đã có video thì phát video thật thay vì placeholder
- Giữ `ASP.NET Core` là orchestration layer chính

## Non-Goals

- Chưa làm animation bullet phức tạp trong từng slide
- Chưa làm subtitle karaoke hoặc transcript highlight
- Chưa làm avatar giảng viên
- Chưa render theo kiểu cinematic scene hoặc motion-heavy editing
- Chưa tối ưu phân tán/scale-out cho render farm

## Current State

Hiện tại hệ thống đã có:

- `slideOutlineJson`
- `voiceoverPlanJson`
- `audioUrl`
- `audioSegmentsJson`
- background jobs cho `lesson content` và `lesson audio`

Nhưng learner chưa xem được bài học dưới dạng video vì:

- learner payload hiện vẫn dựa vào `VideoUrl`
- `Generate audio` chỉ tạo `AudioUrl`
- chưa có pipeline nào biến `slides + audio` thành `VideoUrl`

## Proposed Architecture

### 1. ASP.NET Core Web API

`ASP.NET Core` là nơi nắm nghiệp vụ và trạng thái hệ thống:

- tạo job `GenerateLessonVideo`
- tạo job `GenerateCourseVideo`
- xác thực user/admin được phép gọi
- kiểm tra lesson đủ điều kiện render
- gọi `video-worker`
- ghi nhận progress, error, kết quả cuối
- lưu `VideoUrl` và trạng thái video vào lesson
- trả payload learner/admin đã có `VideoUrl`

### 2. Video Worker

`video-worker` là service chuyên xử lý render nặng. Nó không nắm domain nghiệp vụ của course; nó chỉ nhận payload đầy đủ từ backend và trả về artifact render.

Input chính:

- `lessonId`
- `lessonTitle`
- `slideOutlineJson`
- `audioUrl`
- `audioSegmentsJson`

Output chính:

- `videoUrl`
- `durationSeconds`
- metadata timing từng slide nếu cần

### 3. Storage

Video output được lưu vào thư mục storage chia sẻ, cùng chiến lược với audio:

- ví dụ: `/storage/video/{lessonId}.mp4`

Backend chỉ lưu `VideoUrl` trỏ tới asset nội bộ để learner/admin dùng lại.

## Rendering Pipeline

### Step 1. Validate Preconditions

Backend chỉ cho render khi lesson có đủ:

- `SlideOutlineJson` hợp lệ
- `AudioUrl` tồn tại
- `AudioSegmentsJson` hợp lệ
- `AudioGenerationStatus == Completed`

Nếu thiếu một trong các điều kiện trên, request bị từ chối sớm với message rõ ràng.

### Step 2. Build Slide Frames

`video-worker` parse `slideOutlineJson` và render từng slide thành ảnh PNG tĩnh.

Version đầu chỉ cần render:

- slide number
- title
- bullet points

`speakerNotes` không cần hiện trên video frame, vì phần này đã đi vào audio.

Rendering strategy:

- tạo layout đồng nhất cho toàn bộ lesson
- mỗi slide thành `1 PNG`
- ưu tiên chất lượng ổn định, ít moving parts

### Step 3. Resolve Timeline

Timeline của video được tính từ `audioSegmentsJson`.

Nguyên tắc:

- mỗi slide map với đúng segment audio của slide đó
- thời lượng hiển thị slide = thời lượng segment audio tương ứng
- hệ thống cho phép thêm padding sau này, nhưng `v1` mặc định bám sát audio segment

Kết quả là mỗi slide có:

- `startTime`
- `duration`
- `endTime`

### Step 4. Assemble Video

`video-worker` ghép chuỗi PNG thành video timeline rồi ghép với audio lesson hoàn chỉnh.

Pipeline logical:

1. render PNG từng slide
2. dựng video tạm từ chuỗi ảnh theo duration từng slide
3. ghép `audioUrl` của lesson vào video
4. xuất `mp4`

Format đầu ra:

- `mp4`
- H.264 video
- AAC audio nếu tool render yêu cầu transcode phù hợp

### Step 5. Return Result

Khi render xong, `video-worker` trả:

- `videoUrl`
- `durationSeconds`
- optional `slidesTiming`

Backend dùng kết quả này để cập nhật lesson.

## Data Model Changes

Thêm metadata video vào `Lesson`:

- `VideoGenerationStatus`
- `VideoGenerationError`
- `VideoGeneratedAt`

`VideoUrl` tiếp tục là trường output chính cho learner.

Trạng thái đề xuất:

- `NotGenerated`
- `GeneratingFrames`
- `RenderingVideo`
- `Completed`
- `Failed`

Không cần thêm entity riêng cho video trong `v1`.

## Background Jobs

Thêm 2 job type mới:

- `GenerateLessonVideo`
- `GenerateCourseVideo`

Hành vi:

- `GenerateLessonVideo`: render một lesson cụ thể
- `GenerateCourseVideo`: duyệt toàn bộ lesson đủ điều kiện trong course

Admin có thể:

- generate video toàn course
- generate lại video cho lesson lỗi
- generate lại video cho lesson đã có video nếu muốn refresh output

## Admin Experience

Trang `/admin/courses/:id` được mở rộng thêm:

- nút `Generate video khóa học`
- nút `Generate video` / `Generate lại video` cho từng lesson
- badge trạng thái video
- panel progress cho job video
- preview video khi lesson đã có `VideoUrl`

Hành vi hiển thị:

- nếu đang chạy job course video thì hiện progress bar như content/audio
- nếu lesson fail thì hiện lỗi ngay trên lesson card
- nếu lesson đã có video thì hiện player preview

## Learner Experience

Trang learner sẽ thay `video placeholder` bằng video thật nếu `VideoUrl` có mặt.

Rules:

- course phải `Published` như hiện tại
- nếu lesson có `VideoUrl`, learner xem video
- nếu lesson chưa có `VideoUrl`, learner thấy trạng thái `Bài học đang được chuẩn bị video`

`v1` không bắt learner fallback sang audio-only trong cùng player. Nếu cần audio-only sau này, đó là feature riêng.

## Invalidations

Khi dữ liệu nguồn thay đổi, video cũ phải bị invalid:

- `teachingScript` đổi
- `slideOutlineJson` đổi
- `voiceoverPlanJson` đổi dẫn đến audio re-generated
- `audioUrl` đổi
- `audioSegmentsJson` đổi

Khi invalid:

- `VideoUrl = null`
- `VideoGenerationStatus = NotGenerated`
- `VideoGenerationError = null`
- `VideoGeneratedAt = null`

Điều này tránh learner xem nhầm video cũ không còn khớp với nội dung mới.

## APIs

### Course-level

- `POST /api/courses/{id}/generate-lesson-video`
- `POST /api/courses/{courseId}/lessons/{lessonId}/regenerate-lesson-video`

### Lesson-level

- `GET /api/lessons/{id}/video`

### Worker internal

- `POST /jobs/generate-lesson-video`

Worker request payload:

- `lesson_id`
- `lesson_title`
- `slide_outline_json`
- `audio_url`
- `audio_segments_json`

Worker response payload:

- `video_url`
- `duration_seconds`
- `error_message`
- optional `slide_timings`

## Failure Handling

Các failure mode chính:

1. Thiếu `audioUrl`
2. `audioSegmentsJson` lỗi schema
3. `slideOutlineJson` lỗi schema
4. render tool thất bại
5. asset output không ghi được vào storage

Nguyên tắc xử lý:

- validate sớm ở backend cho lỗi dữ liệu đầu vào
- worker trả lỗi ngắn gọn, đủ chẩn đoán
- lesson fail không làm hỏng toàn bộ hệ thống
- course job vẫn tiếp tục xử lý lesson khác nếu một lesson fail

## Tooling Direction

Spec này cố ý chưa chốt công cụ render cuối cùng ở mức implementation chi tiết, nhưng worker phải hỗ trợ pipeline:

- render slide -> PNG
- build image timeline
- merge audio -> MP4

Ở plan triển khai sẽ chốt toolchain cụ thể sao cho:

- ít phụ thuộc UI runtime
- reproducible trong Docker
- output ổn định

## Testing Strategy

### Backend

- validate preconditions cho render video
- job service chuyển trạng thái lesson/job đúng
- invalidate video khi source content/audio thay đổi

### Worker

- parse payload thành công
- render timeline từ sample slide/audio metadata
- tạo ra `mp4` file và metadata đúng
- lỗi đầu vào được trả rõ ràng

### Frontend

- admin thấy nút generate/regenerate video
- job progress video hiển thị đúng
- learner render video player khi `VideoUrl` tồn tại
- learner render trạng thái chưa sẵn sàng khi chưa có video

## Rollout Plan

1. Thêm video status vào model + DTO + API
2. Tạo `video-worker`
3. Nối backend video jobs
4. Thêm admin video controls + progress + preview
5. Cập nhật learner page dùng `VideoUrl`
6. Verify end-to-end với một lesson thật

## Open Decisions Already Resolved

- `ASP.NET Core` là orchestration layer chính
- `video-worker` là executor riêng
- render source dùng `PNG từng slide`
- timeline mặc định bám `audio segment`
- output learner là `video MP4`

## Success Criteria

Feature được coi là hoàn tất khi:

- admin generate được video cho một lesson thành công
- admin generate được video cho toàn course bằng job nền
- lesson hoàn tất có `VideoUrl`
- learner mở course published và xem được video thật trên trang học
- khi content/audio đổi, video cũ bị invalid đúng cách
