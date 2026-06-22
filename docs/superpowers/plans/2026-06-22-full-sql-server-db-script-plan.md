# Full SQL Server DB Script Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tạo một file `.sql` duy nhất có thể `DROP` và tạo lại toàn bộ database `vibe_course_ai_db`, bao gồm schema và toàn bộ dữ liệu hiện tại, để import trực tiếp bằng SQL Server query window.

**Architecture:** Dùng chính SQL Server đang chạy trong Docker làm nguồn sự thật, trích xuất schema và data từ database hiện tại, rồi ghép thành một full snapshot script có preamble `DROP/CREATE/USE`. Đầu ra được lưu trong `backups/` để có thể dùng độc lập với file `.bak`.

**Tech Stack:** SQL Server 2022, Docker Compose, Bash, `sqlcmd`

---

### Task 1: Khảo sát database thực tế và xác định đầu ra

**Files:**
- Create: `backups/vibe_course_ai_db_full_2026-06-22.sql`
- Modify: `docs/superpowers/specs/2026-06-22-full-sql-server-db-script-design.md`

- [ ] **Step 1: Ghi lại danh sách bảng hiện có trong database**

Run:

```bash
docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'VibeCourse@123' -C \
  -Q "SET NOCOUNT ON; SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_SCHEMA, TABLE_NAME"
```

Expected: in ra toàn bộ bảng hiện có trong `vibe_course_ai_db` để chốt phạm vi export.

- [ ] **Step 2: Xác nhận credential và database name đang dùng thật**

Run:

```bash
docker inspect course_sqlserver --format '{{range .Config.Env}}{{println .}}{{end}}' | rg 'MSSQL_SA_PASSWORD|MSSQL_DB_NAME'
```

Expected: thấy đúng password `sa` đang dùng và, nếu có, biến tên DB để tránh export nhầm database.

- [ ] **Step 3: Bổ sung note vào spec nếu phát hiện khác biệt giữa code schema và DB thực tế**

Nếu có bảng/object xuất hiện trong DB nhưng không có trong `DbInitializer`, append thẳng vào spec một mục `Observed runtime-only objects` để người sau hiểu snapshot đang dựa trên DB thực tế chứ không chỉ code.

### Task 2: Sinh full snapshot SQL từ database hiện tại

**Files:**
- Create: `backups/vibe_course_ai_db_full_2026-06-22.sql`

- [ ] **Step 1: Xuất schema-only từ database hiện tại**

Run lệnh export bằng công cụ SQL Server sẵn trong container hoặc host, hướng đến file tạm để lấy phần DDL. Nếu trong môi trường hiện tại không có tool chuyên export schema, fallback bằng cách dùng metadata queries để sinh:

```sql
SELECT 'CREATE TABLE ...'
```

ưu tiên đầy đủ các thành phần:
- bảng
- primary key
- foreign key
- index
- default constraint

Expected: có một phần schema hoàn chỉnh có thể chạy trên database rỗng.

- [ ] **Step 2: Xuất data của toàn bộ bảng theo thứ tự phụ thuộc**

Sinh các câu:

```sql
SET IDENTITY_INSERT [TableName] ON;
INSERT INTO [TableName] (...) VALUES (...);
SET IDENTITY_INSERT [TableName] OFF;
```

theo đúng thứ tự cha trước con. Nếu một bảng không có identity thì không bật `IDENTITY_INSERT`.

Expected: mọi bảng có dữ liệu đều có `INSERT` tương ứng trong file snapshot.

- [ ] **Step 3: Ghép preamble `DROP + CREATE + USE`**

Mở đầu file với:

```sql
USE master;
GO

IF DB_ID(N'vibe_course_ai_db') IS NOT NULL
BEGIN
    ALTER DATABASE [vibe_course_ai_db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [vibe_course_ai_db];
END
GO

CREATE DATABASE [vibe_course_ai_db];
GO

USE [vibe_course_ai_db];
GO
```

Expected: script có thể chạy thẳng trên SQL Server mà không cần thao tác chuẩn bị thủ công.

- [ ] **Step 4: Ghi file snapshot cuối cùng vào `backups/`**

Đầu ra cuối cùng:

```bash
backups/vibe_course_ai_db_full_2026-06-22.sql
```

Expected: file tồn tại, không rỗng, có đủ phần `DROP DATABASE`, `CREATE DATABASE`, `CREATE TABLE`, `INSERT INTO`.

### Task 3: Kiểm tra script trên SQL Server

**Files:**
- Test: `backups/vibe_course_ai_db_full_2026-06-22.sql`

- [ ] **Step 1: Chạy thử snapshot script trên SQL Server đang chạy**

Run:

```bash
docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'VibeCourse@123' -C \
  -i /var/opt/mssql/backup/vibe_course_ai_db_full_2026-06-22.sql
```

Expected: script chạy hết không lỗi syntax, FK, hoặc identity.

- [ ] **Step 2: Xác nhận database online sau khi import**

Run:

```bash
docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'VibeCourse@123' -C \
  -Q "SET NOCOUNT ON; SELECT name, state_desc FROM sys.databases WHERE name='vibe_course_ai_db'"
```

Expected: `vibe_course_ai_db ONLINE`.

- [ ] **Step 3: Kiểm tra nhanh số lượng record ở vài bảng chính**

Run:

```bash
docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'VibeCourse@123' -C \
  -Q "SET NOCOUNT ON; SELECT 'Users' AS [Table], COUNT(*) AS [Count] FROM Users UNION ALL SELECT 'Courses', COUNT(*) FROM Courses UNION ALL SELECT 'Lessons', COUNT(*) FROM Lessons UNION ALL SELECT 'LessonComments', COUNT(*) FROM LessonComments"
```

Expected: trả ra count hợp lệ, chứng minh dữ liệu thật đã được nạp lại.

- [ ] **Step 4: Commit**

```bash
git add docs/superpowers/specs/2026-06-22-full-sql-server-db-script-design.md docs/superpowers/plans/2026-06-22-full-sql-server-db-script-plan.md backups/vibe_course_ai_db_full_2026-06-22.sql
git commit -m "feat: add full sql server database snapshot script"
```

Expected: có commit chứa spec, plan, và snapshot script.
