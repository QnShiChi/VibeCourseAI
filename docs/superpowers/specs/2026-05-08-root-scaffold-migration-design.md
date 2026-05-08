# Root Scaffold Migration Design

## Mục tiêu

Chuyển toàn bộ scaffold hiện đang được sử dụng trong `.worktrees/course-video-system-scaffold/` lên thư mục gốc của repo `VibeCourseAI`, để từ nay:

- source chính chỉ nằm ở `root`
- không cần `cd` vào `.worktrees/...` để chạy dự án
- có thể vận hành toàn bộ hệ thống từ `root`
- worktree hiện tại không còn là workspace chạy thường ngày

## Phạm vi

Thay đổi này bao gồm:

- đồng bộ `backend/`, `frontend/`, `ai-worker/`, `storage/`, `docker-compose.yml`, `.env.example` từ worktree lên root
- đồng bộ hoặc tạo mới `.env` ở root
- dọn phần code trùng/nhầm hiện có ở root
- tạo `Makefile` tại root để chạy stack và thao tác backup/restore DB
- tạo thư mục `backups/` ở root
- verify lại stack SQL Server sau khi chuyển

Không bao gồm:

- phát triển thêm tính năng nghiệp vụ
- thay đổi kiến trúc backend/frontend
- setup CI/CD

## Quyết định thiết kế

### 1. Root là nguồn chính duy nhất

Sau khi hoàn tất, mọi file vận hành chính sẽ ở root:

- `backend/`
- `frontend/`
- `ai-worker/`
- `storage/`
- `docker-compose.yml`
- `.env.example`
- `.env`
- `Makefile`
- `backups/`

Thư mục `.worktrees/course-video-system-scaffold/` không còn là nơi chạy dự án hằng ngày.

### 2. Đồng bộ theo hướng “worktree thắng”

Nội dung trong `.worktrees/course-video-system-scaffold/` được xem là phiên bản đúng hơn vì:

- đó là nơi đã được verify lại gần nhất
- có cấu hình SQL Server hiện tại
- có `.env` và `docker-compose.yml` khớp với stack đang chạy

Do đó khi root và worktree khác nhau, nội dung từ worktree sẽ ghi đè nội dung trùng ở root.

### 3. Makefile tại root

`Makefile` sẽ là entrypoint vận hành chuẩn, ít nhất gồm:

- `make up`
- `make down`
- `make backup-db`
- `make restore FILE=...`

Hành vi mong muốn:

- `make up`: dựng toàn bộ stack từ root bằng `docker compose up -d`
- `make down`: tắt toàn bộ stack từ root bằng `docker compose down`
- `make backup-db`: tạo cả `SQL script` và `BACPAC` trong `backups/`
- `make restore FILE=...`: khôi phục từ file do người dùng chỉ định

### 4. Backup và restore

Thư mục backup:

- `backups/`

Quy ước:

- backup SQL script và BACPAC đều được tạo từ root
- restore nhận tham số `FILE=...`
- logic restore phân nhánh theo đuôi file:
  - `.sql` thì restore bằng command phù hợp với SQL Server
  - `.bacpac` thì import bằng công cụ phù hợp cho SQL Server container workflow

### 5. Dọn worktree sau khi root ổn định

Sau khi:

- source đã được chuyển hoàn toàn lên root
- stack ở root chạy ổn
- các lệnh Makefile hoạt động đúng

thì worktree `course-video-system-scaffold` có thể được bỏ khỏi quy trình vận hành thường ngày. Việc xóa hẳn worktree vật lý sẽ được thực hiện như bước cleanup cuối cùng, sau khi xác nhận root đã là nguồn duy nhất.

## Rủi ro

- Root hiện có file trùng với worktree, nên nếu đồng bộ bất cẩn sẽ giữ lại trạng thái nửa cũ nửa mới.
- `.env` hiện là file vận hành thật, nên cần đảm bảo root nhận đúng bản đang chạy với SQL Server.
- Restore BACPAC trong môi trường container có thể cần thêm utility chuyên biệt; phần này phải được kiểm tra trong lúc triển khai.

## Kết quả mong muốn

Sau khi hoàn tất:

- người dùng đứng ở `~/workspace/VibeCourseAI` là chạy được dự án
- không cần dùng path `.worktrees/course-video-system-scaffold`
- DBeaver kết nối dựa trên cấu hình ở root
- có workflow vận hành và backup/restore rõ ràng bằng `Makefile`
