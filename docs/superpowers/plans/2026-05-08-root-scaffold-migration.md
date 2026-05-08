# Root Scaffold Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Chuyển scaffold SQL Server đang dùng từ `.worktrees/course-video-system-scaffold/` lên thư mục gốc repo, thêm `Makefile` vận hành tại root, rồi loại bỏ sự phụ thuộc vào worktree.

**Architecture:** Root sẽ trở thành nguồn chính duy nhất cho `backend`, `frontend`, `ai-worker`, `storage`, `docker-compose.yml`, `.env`, `.env.example`, `backups`, và `Makefile`. Nội dung trong worktree được xem là bản chuẩn để đồng bộ lên root, sau đó root sẽ được verify bằng Docker Compose và backup/restore commands.

**Tech Stack:** ASP.NET Core Web API, EF Core SQL Server, React/Vite, Python FastAPI, Docker Compose, GNU Make, SQL Server 2022.

---

### Task 1: Đồng bộ scaffold từ worktree lên root

**Files:**
- Modify: `backend/**`
- Modify: `frontend/**`
- Create: `ai-worker/**`
- Modify: `storage/**`
- Create: `docker-compose.yml`
- Create: `.env.example`
- Create: `.env`

- [ ] **Step 1: Xác nhận trạng thái root và worktree trước khi đồng bộ**

Run:

```bash
find /home/phan-duong-quoc-nhat/workspace/VibeCourseAI -maxdepth 2 \( -path '*/.git' -o -path '*/.worktrees' \) -prune -o \( -name 'docker-compose.yml' -o -name '.env' -o -name '.env.example' -o -path '*/backend/*' -o -path '*/frontend/*' -o -path '*/ai-worker/*' -o -path '*/storage/*' \) -print | sort
```

Expected: thấy root có phần scaffold trùng, và worktree có đủ scaffold SQL Server mới hơn.

- [ ] **Step 2: Ghi đè root bằng nội dung từ worktree**

Run:

```bash
rsync -a --delete \
  /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/.worktrees/course-video-system-scaffold/backend/ \
  /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/backend/

rsync -a --delete \
  /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/.worktrees/course-video-system-scaffold/frontend/ \
  /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/frontend/

rsync -a --delete \
  /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/.worktrees/course-video-system-scaffold/ai-worker/ \
  /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/ai-worker/

rsync -a --delete \
  /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/.worktrees/course-video-system-scaffold/storage/ \
  /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/storage/
```

Expected: root nhận đúng source từ worktree.

- [ ] **Step 3: Đồng bộ file cấu hình root**

Run:

```bash
cp /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/.worktrees/course-video-system-scaffold/docker-compose.yml /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/docker-compose.yml
cp /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/.worktrees/course-video-system-scaffold/.env.example /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/.env.example
cp /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/.worktrees/course-video-system-scaffold/.env /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/.env
```

Expected: root có `docker-compose.yml`, `.env.example`, `.env` đúng cấu hình SQL Server hiện tại.

- [ ] **Step 4: Kiểm tra root đã có đầy đủ scaffold**

Run:

```bash
find /home/phan-duong-quoc-nhat/workspace/VibeCourseAI -maxdepth 2 \( -path '*/.git' -o -path '*/.worktrees' \) -prune -o \( -name 'docker-compose.yml' -o -name '.env' -o -name '.env.example' -o -path '*/backend/*' -o -path '*/frontend/*' -o -path '*/ai-worker/*' -o -path '*/storage/*' \) -print | sort
```

Expected: root có đầy đủ các thành phần vận hành chính, không cần phụ thuộc path `.worktrees/...`.

### Task 2: Thêm Makefile và thư mục backups tại root

**Files:**
- Create: `Makefile`
- Create: `backups/.gitkeep`
- Modify: `.gitignore`

- [ ] **Step 1: Tạo thư mục backup**

Run:

```bash
mkdir -p /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/backups
touch /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/backups/.gitkeep
```

Expected: có thư mục `backups/` ở root.

- [ ] **Step 2: Tạo `Makefile` root**

Nội dung:

```makefile
COMPOSE=docker compose
DB_NAME=vibe_course_ai_db
DB_USER=sa
DB_PASSWORD=VibeCourse@123
BACKUP_DIR=backups
TIMESTAMP=$(shell date +%Y%m%d_%H%M%S)
SQL_FILE=$(BACKUP_DIR)/$(DB_NAME)_$(TIMESTAMP).sql
BACPAC_FILE=$(BACKUP_DIR)/$(DB_NAME)_$(TIMESTAMP).bacpac

.PHONY: up down backup-db restore ps logs

up:
	$(COMPOSE) up -d

down:
	$(COMPOSE) down

ps:
	$(COMPOSE) ps

logs:
	$(COMPOSE) logs -f

backup-db:
	mkdir -p $(BACKUP_DIR)
	$(COMPOSE) exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U $(DB_USER) -P "$(DB_PASSWORD)" -C -Q "BACKUP DATABASE [$(DB_NAME)] TO DISK = N'/var/opt/mssql/backup/$(DB_NAME).bak' WITH INIT"
	$(COMPOSE) cp sqlserver:/var/opt/mssql/backup/$(DB_NAME).bak $(BACPAC_FILE)
	$(COMPOSE) exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U $(DB_USER) -P "$(DB_PASSWORD)" -C -Q "SET NOCOUNT ON; PRINT 'Logical SQL backup placeholder: use .bak as canonical backup for SQL Server in this scaffold.'"
	printf '%s\n' '-- SQL Server logical export placeholder. Canonical restore artifact is the .bak file created alongside this file.' > $(SQL_FILE)

restore:
	test -n "$(FILE)" || (echo "Usage: make restore FILE=backups/<file>"; exit 1)
	case "$(FILE)" in \
	  *.bak|*.bacpac) \
	    cp "$(FILE)" /tmp/restore.bak && \
	    $(COMPOSE) cp /tmp/restore.bak sqlserver:/var/opt/mssql/backup/restore.bak && \
	    $(COMPOSE) exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U $(DB_USER) -P "$(DB_PASSWORD)" -C -Q "IF DB_ID('$(DB_NAME)') IS NOT NULL ALTER DATABASE [$(DB_NAME)] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; IF DB_ID('$(DB_NAME)') IS NOT NULL DROP DATABASE [$(DB_NAME)']; RESTORE DATABASE [$(DB_NAME)] FROM DISK = N'/var/opt/mssql/backup/restore.bak' WITH MOVE '$(DB_NAME)' TO '/var/opt/mssql/data/$(DB_NAME).mdf', MOVE '$(DB_NAME)_log' TO '/var/opt/mssql/data/$(DB_NAME)_log.ldf', REPLACE";; \
	  *.sql) \
	    $(COMPOSE) exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U $(DB_USER) -P "$(DB_PASSWORD)" -C -d $(DB_NAME) -i /dev/stdin < "$(FILE)";; \
	  *) \
	    echo "Unsupported restore file: $(FILE)"; exit 1;; \
	esac
```

- [ ] **Step 3: Cập nhật `.gitignore` cho backups**

Thêm:

```gitignore
backups/*
!backups/.gitkeep
```

- [ ] **Step 4: Kiểm tra Makefile có thể parse**

Run:

```bash
make -n up
make -n down
make -n backup-db
make -n restore FILE=backups/sample.bak
```

Expected: Make hiển thị command tương ứng, không lỗi syntax.

### Task 3: Verify stack chạy từ root

**Files:**
- Verify: `docker-compose.yml`
- Verify: `.env`
- Verify: `Makefile`

- [ ] **Step 1: Tắt stack cũ nếu còn**

Run:

```bash
cd /home/phan-duong-quoc-nhat/workspace/VibeCourseAI
make down
```

Expected: stack root dừng sạch.

- [ ] **Step 2: Dựng stack từ root**

Run:

```bash
cd /home/phan-duong-quoc-nhat/workspace/VibeCourseAI
make up
```

Expected: `sqlserver`, `backend`, `frontend`, `ai-worker` cùng khởi động từ root.

- [ ] **Step 3: Kiểm tra health backend và worker**

Run:

```bash
curl -s http://localhost:5000/api/health
curl -s http://localhost:8000/health
```

Expected:

```json
{"status":"ok"}
```

cho cả hai endpoint.

- [ ] **Step 4: Kiểm tra frontend từ root**

Run:

```bash
curl -I http://localhost:3000
```

Expected: `HTTP/1.1 200 OK`.

- [ ] **Step 5: Kiểm tra SQL Server database mới**

Run:

```bash
docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "VibeCourse@123" -C -Q "SELECT name FROM sys.databases;"
```

Expected: có `vibe_course_ai_db`.

### Task 4: Verify backup/restore workflow tại root

**Files:**
- Verify: `Makefile`
- Verify: `backups/`

- [ ] **Step 1: Chạy backup-db**

Run:

```bash
cd /home/phan-duong-quoc-nhat/workspace/VibeCourseAI
make backup-db
```

Expected: tạo một file `.bak` lưu dưới tên `.bacpac` theo quy ước hiện tại và một file `.sql` placeholder trong `backups/`.

- [ ] **Step 2: Kiểm tra thư mục backups**

Run:

```bash
find /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/backups -maxdepth 1 -type f | sort
```

Expected: có file backup mới và `.sql` đi kèm.

- [ ] **Step 3: Verify restore command parse được**

Run:

```bash
cd /home/phan-duong-quoc-nhat/workspace/VibeCourseAI
make -n restore FILE=backups/sample.bak
make -n restore FILE=backups/sample.sql
```

Expected: Make render đúng các command restore cho hai loại file.

### Task 5: Cleanup dependency vào worktree và chuẩn bị bỏ worktree

**Files:**
- Verify: root workspace
- Verify: `.worktrees/course-video-system-scaffold`

- [ ] **Step 1: Kiểm tra root đã là nguồn đầy đủ**

Run:

```bash
git -C /home/phan-duong-quoc-nhat/workspace/VibeCourseAI status --short
```

Expected: mọi thay đổi vận hành cần thiết xuất hiện ở root.

- [ ] **Step 2: Xác nhận không cần chạy lệnh nào từ worktree nữa**

Run:

```bash
find /home/phan-duong-quoc-nhat/workspace/VibeCourseAI/.worktrees/course-video-system-scaffold -maxdepth 1 -type f | sort
```

Expected: chỉ còn vai trò tham chiếu tạm; root mới là nơi chạy thật.

- [ ] **Step 3: Chỉ sau khi user xác nhận, remove worktree**

Run:

```bash
git -C /home/phan-duong-quoc-nhat/workspace/VibeCourseAI worktree remove .worktrees/course-video-system-scaffold
git -C /home/phan-duong-quoc-nhat/workspace/VibeCourseAI branch -d feat/course-video-system-scaffold
```

Expected: worktree bị xóa khỏi đĩa và branch scaffold được dọn sau khi root đã ổn định.

## Self-Review

- Spec coverage: plan bao phủ chuyển scaffold lên root, thêm Makefile, thêm backups, verify root-based operations, và cleanup worktree.
- Placeholder scan: không dùng `TBD` hay chỉ dẫn mơ hồ.
- Type consistency: tên database `vibe_course_ai_db`, password `VibeCourse@123`, service `sqlserver`, và đường dẫn root đều thống nhất.
