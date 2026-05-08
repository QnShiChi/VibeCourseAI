# Course Video System Scaffold Design

## Mục tiêu

Thiết lập bộ khung ban đầu cho dự án "tạo video khóa học từ đề cương" dựa trên tài liệu `tailieuhethong.md`, tập trung vào:

- Backend `ASP.NET Core Web API` theo `Layered Architecture`
- Frontend `React`
- `MySQL` chạy bằng Docker
- `ai-worker` Python ở mức placeholder
- Kết nối database bằng `EF Core Code First`
- Có thể khởi động toàn bộ hệ thống bằng `docker-compose`

Phạm vi hiện tại là scaffold nền tảng và cấu hình hạ tầng, chưa triển khai đầy đủ logic nghiệp vụ tạo video.

## Quyết định thiết kế

### 1. Backend

Sử dụng một project `ASP.NET Core Web API` duy nhất, tổ chức theo lớp:

- `Controllers`
- `Services`
- `Repositories`
- `DTOs`
- `Models`
- `Data`
- `Configurations`
- `Middlewares`

Module nghiệp vụ ban đầu được scaffold ở mức cơ bản:

- `Auth`
- `Users`
- `Courses`

Database access dùng:

- `Entity Framework Core`
- `Pomelo.EntityFrameworkCore.MySql`
- `Code First Migration`

Seed dữ liệu tối thiểu:

- `Role`: `Admin`, `User`

### 2. Frontend

Tạo skeleton `React` để hệ thống có giao diện chạy được ngay, nhưng chưa làm UI chi tiết.

Cấu trúc chính:

- `src/pages`
- `src/components`
- `src/routes`
- `src/api`
- `src/stores`

Route cơ bản:

- `login`
- `register`
- `dashboard`
- `courses`

### 3. AI Worker

Tạo service Python ở mức placeholder để hoàn thiện topology Docker:

- có `Dockerfile`
- có app entrypoint tối thiểu
- có health endpoint hoặc placeholder endpoint

Worker chưa xử lý `VibeVoice` hay `FFmpeg` ở giai đoạn này.

### 4. Docker và môi trường chạy

Hệ thống được tổ chức thành các service:

- `mysql`
- `backend`
- `frontend`
- `ai-worker`

Thư mục dùng chung:

- `storage/syllabuses`
- `storage/slides`
- `storage/audio`
- `storage/videos`
- `storage/thumbnails`

Thiết lập thêm:

- `.env.example`
- `docker-compose.yml`
- volume cho `mysql`
- volume mount cho `storage`

### 5. Kết nối database

Backend đọc connection string từ environment variable trong Docker.

Thiết kế ban đầu:

- `AppDbContext`
- entity cơ bản cho `Role`, `User`, `Course`
- migration đầu tiên

Khi container backend khởi động, ứng dụng cần có khả năng kết nối MySQL ổn định trong môi trường Docker.

## Cấu trúc thư mục dự kiến

```text
VibeCourseAI/
├── backend/
│   ├── CourseVideo.API/
│   │   ├── Controllers/
│   │   ├── DTOs/
│   │   ├── Models/
│   │   ├── Services/
│   │   ├── Repositories/
│   │   ├── Data/
│   │   ├── Configurations/
│   │   ├── Middlewares/
│   │   ├── Properties/
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── Program.cs
│   │   └── CourseVideo.API.csproj
│   └── CourseVideo.sln
├── frontend/
│   ├── src/
│   │   ├── api/
│   │   ├── components/
│   │   ├── pages/
│   │   ├── routes/
│   │   ├── stores/
│   │   ├── App.jsx
│   │   └── main.jsx
│   ├── public/
│   ├── package.json
│   ├── Dockerfile
│   └── nginx.conf
├── ai-worker/
│   ├── app/
│   │   └── main.py
│   ├── requirements.txt
│   └── Dockerfile
├── storage/
│   ├── syllabuses/
│   ├── slides/
│   ├── audio/
│   ├── videos/
│   └── thumbnails/
├── docs/
│   └── superpowers/
│       └── specs/
├── docker-compose.yml
└── .env.example
```

## Luồng khởi tạo ban đầu

1. `docker-compose up --build`
2. `mysql` khởi động với database mặc định
3. `backend` đọc connection string và kết nối MySQL
4. `frontend` chạy và gọi được API backend
5. `ai-worker` chạy placeholder service

## Rủi ro và giới hạn hiện tại

- Chưa tích hợp `VibeVoice`
- Chưa có pipeline tạo slide, audio, video
- Chưa có auth hoàn chỉnh, chỉ scaffold module và cấu trúc
- Chưa có production hardening như reverse proxy đầy đủ, CI/CD, secret manager

## Phạm vi triển khai ngay sau spec

Sau khi spec được duyệt, bước triển khai sẽ:

1. Scaffold cấu trúc thư mục
2. Tạo backend Web API và cấu hình EF Core MySQL
3. Tạo frontend React skeleton
4. Tạo `ai-worker` placeholder
5. Tạo Dockerfile và `docker-compose.yml`
6. Tạo migration đầu tiên và cấu hình seed dữ liệu cơ bản
