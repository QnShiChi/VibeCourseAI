# VibeCourseAI

Nền tảng tạo, quản lý và học khóa học video với AI.

Hệ thống này cho phép:

- tải lên đề cương/syllabus
- sinh cấu trúc khóa học, module, lesson
- generate nội dung bài học bằng AI
- chỉnh sửa `slides`, `voiceover`
- generate `audio` và `video` cho từng lesson
- học khóa học ở phía learner
- bình luận trực tiếp dưới mỗi lesson video

## Kiến trúc

Repo hiện tại gồm 5 service chính:

- `sqlserver`: Microsoft SQL Server 2022
- `backend`: ASP.NET Core Web API (`.NET 8`)
- `frontend`: React + Vite
- `ai-worker`: worker Python cho AI/TTS
- `video-worker`: worker Python cho render video

Luồng tổng quát:

1. Admin tải syllabus
2. Backend sinh course/module/lesson
3. Backend gọi AI worker để generate content/audio
4. Backend gọi video worker để render video
5. Learner xem video lesson và tương tác qua comment

## Công nghệ chính

- Backend: ASP.NET Core Web API, Entity Framework Core, SQL Server, JWT Auth
- Frontend: React 18, Vite, React Router
- AI/TTS Worker: Python
- Video Worker: Python + FFmpeg
- Infra local: Docker Compose

## Cấu trúc thư mục

```text
backend/CourseVideo.API   ASP.NET Core API
frontend                  React app
ai-worker                 AI/TTS worker
video-worker              Video render worker
storage                   file sinh ra runtime: audio, video, uploads...
backups                   backup database
docs                      spec/plan kỹ thuật
```

## Yêu cầu trước khi chạy

Cần cài sẵn:

- Docker
- Docker Compose plugin (`docker compose`)
- GNU Make

Kiểm tra nhanh:

```bash
docker --version
docker compose version
make --version
```

## Cách chạy hệ thống khi mới kéo repo về

### 1. Clone repo

```bash
git clone <repo-url>
cd VibeCourseAI
```

### 2. Tạo file môi trường

Copy file mẫu:

```bash
cp .env.example .env
```

### 3. Cập nhật biến môi trường nếu cần

Các biến quan trọng trong `.env`:

```env
SQLSERVER_DATABASE=vibe_course_ai_db
SQLSERVER_SA_PASSWORD=VibeCourse@123
SQLSERVER_PORT=1434
BACKEND_PORT=5000
FRONTEND_PORT=3000
AI_WORKER_PORT=8000
VIDEO_WORKER_PORT=8001
VITE_API_BASE_URL=http://localhost:5000/api
```

Biến dùng cho AI:

```env
OPENROUTER__APIKEY=
OPENROUTER__MODEL=
OPENAI_API_KEY=
OPENAI_TTS_MODEL=gpt-4o-mini-tts
OPENAI_TTS_VOICE=alloy
OPENAI_TTS_FORMAT=wav
```

Lưu ý:

- không có `OPENROUTER__APIKEY` thì không generate được lesson content bằng OpenRouter
- không có `OPENAI_API_KEY` thì không generate được audio bằng OpenAI TTS
- các chức năng CRUD, learner, comment, layout, DB vẫn chạy bình thường nếu chưa cấu hình key AI

### 4. Khởi động toàn bộ hệ thống

```bash
make up
```

Hoặc:

```bash
docker compose up -d
```

### 5. Kiểm tra trạng thái container

```bash
make ps
```

Hoặc:

```bash
docker compose ps
```

### 6. Truy cập hệ thống

- Frontend: `http://localhost:3000`
- Backend API: `http://localhost:5000`
- Health check: `http://localhost:5000/api/health`

## Tài khoản mặc định

Backend sẽ seed admin từ biến môi trường:

```env
ADMINSEED__FULLNAME=Quản trị viên hệ thống
ADMINSEED__EMAIL=admin@vibecourse.local
ADMINSEED__PASSWORD=ChangeMe@123
```

Nếu giữ nguyên `.env.example`, có thể đăng nhập bằng:

- Email: `admin@vibecourse.local`
- Password: `ChangeMe@123`

Nên đổi lại password seed khi dùng thật.

## Lệnh vận hành nhanh

### Xem log

```bash
make logs
```

### Dừng hệ thống

```bash
make down
```

### Rebuild một service

Ví dụ rebuild frontend:

```bash
docker compose up -d --build frontend
```

Ví dụ rebuild backend:

```bash
docker compose up -d --build backend
```

## Database backup/restore

### Backup database

```bash
make backup-db
```

File backup sẽ nằm trong thư mục `backups/`.

Thông thường sẽ có:

- `*.bak`: bản backup SQL Server đầy đủ
- `*.sql`: file helper rất nhỏ, chỉ để chỉ dẫn restore

### Restore database

Dùng file `.bak`:

```bash
make restore FILE=backups/<ten-file>.bak
```

Ví dụ:

```bash
make restore FILE=backups/vibe_course_ai_db_20260521_230915.bak
```

Lưu ý:

- lệnh này sẽ ghi đè database hiện tại
- container `sqlserver` phải đang chạy

### Hướng dẫn restore database từng bước

Nếu vừa kéo repo về hoặc muốn phục hồi một bản DB cũ, nên làm theo thứ tự này:

1. Khởi động riêng SQL Server hoặc toàn bộ hệ thống:

```bash
docker compose up -d sqlserver
```

Hoặc:

```bash
make up
```

2. Kiểm tra file backup đang có:

```bash
ls -lah backups
```

3. Chạy restore bằng file `.bak`:

```bash
make restore FILE=backups/vibe_course_ai_db_20260521_230915.bak
```

4. Sau khi restore xong, nếu backend đang chạy thì backend sẽ dùng lại DB đó ngay. Nếu cần chắc chắn sạch trạng thái app, có thể restart lại backend:

```bash
docker compose up -d --force-recreate backend
```

5. Kiểm tra hệ thống:

```bash
docker compose ps
curl http://localhost:5000/api/health
```

Lưu ý quan trọng:

- file nên dùng để restore là `.bak`
- file `.sql` trong `backups/` hiện chỉ là file helper, không phải full dump dữ liệu
- `make restore` sẽ `drop` database hiện tại rồi restore lại từ đầu
- nếu backup được tạo từ một thời điểm cũ, dữ liệu hiện tại trong DB sẽ mất sau khi restore

## Storage runtime

Thư mục `storage/` được mount vào container và dùng để lưu:

- syllabus upload
- audio đã generate
- video đã render
- các asset runtime khác

Nếu muốn làm sạch hoàn toàn môi trường local, ngoài việc xóa DB còn phải dọn cả `storage/`.

## Phát triển cục bộ

Repo này đang thiên về chạy đồng bộ bằng Docker Compose. Nếu muốn chạy từng service thủ công thì vẫn được, nhưng luồng chính hiện tại là:

```bash
make up
```

Sau đó code thay đổi có thể rebuild riêng từng service bằng `docker compose up -d --build <service>`.

## Một số lưu ý triển khai

- `DbInitializer` trong backend sẽ tự migrate/ensure schema và seed dữ liệu nền khi app khởi động
- comment dưới lesson video hiện đã có:
  - comment
  - 1 tầng reply
  - reaction emoji
  - moderation admin
- learner chỉ xem được video nếu lesson đã có `VideoUrl`
- render video không gọi OpenAI/OpenRouter trực tiếp
- generate content dùng OpenRouter
- generate audio dùng OpenAI TTS

## Troubleshooting nhanh

### Frontend vào được nhưng API lỗi

Kiểm tra:

```bash
docker compose ps
curl http://localhost:5000/api/health
```

### Generate content bị fail

Kiểm tra:

- `OPENROUTER__APIKEY`
- `OPENROUTER__MODEL`
- log backend và `ai-worker`

### Generate audio bị fail

Kiểm tra:

- `OPENAI_API_KEY`
- `OPENAI_TTS_MODEL`
- log `ai-worker`

### Learner không xem được video

Nguyên nhân thường là:

- lesson chưa `Generate video`
- audio/video worker chưa chạy
- lesson chưa có `VideoUrl`

## Gợi ý quy trình dùng hệ thống

1. Đăng nhập admin
2. Tải syllabus
3. Sinh course structure
4. Generate lesson content
5. Rà và chỉnh `slides` / `voiceover`
6. Generate audio
7. Generate video
8. Publish course
9. Đăng nhập learner để học và bình luận

## Ghi chú

- Repo hiện dùng Docker-first workflow
- README này mô tả đúng cách chạy ở thời điểm hiện tại của dự án
- nếu sau này đổi kiến trúc worker hoặc env vars, README cần cập nhật cùng lúc
