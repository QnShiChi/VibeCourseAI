# Full SQL Server DB Script Design

## Mục tiêu

Tạo một file `.sql` duy nhất có thể chạy trực tiếp trong SQL Server để dựng lại toàn bộ database `vibe_course_ai_db`, bao gồm schema và toàn bộ dữ liệu hiện tại.

## Phạm vi

- File script phải:
  - `DROP` database cũ nếu đã tồn tại
  - `CREATE DATABASE vibe_course_ai_db`
  - `USE vibe_course_ai_db`
  - tạo toàn bộ bảng, khóa chính, khóa ngoại, index và các object cần thiết
  - insert toàn bộ dữ liệu hiện tại từ database đang chạy
- Kết quả phải phù hợp với cách import qua SQL Server bằng `New Query` / `Execute`
- Không dùng `.bak` làm đầu ra cuối cùng cho yêu cầu này

## Cách tiếp cận

### Khuyến nghị

Sinh full snapshot `.sql` từ chính database SQL Server hiện tại đang chạy trong Docker.

Lý do:

- đúng với dữ liệu thật đang có
- tránh lệch giữa schema trong code và schema thực tế
- phù hợp nhất với yêu cầu `insert script`

## Đầu ra

- File mới trong `backups/`
- Tên dự kiến: `vibe_course_ai_db_full_2026-06-22.sql`

## Hành vi mong muốn

Khi chạy file script:

1. nếu database `vibe_course_ai_db` đã tồn tại thì chuyển sang `SINGLE_USER` và `DROP`
2. tạo lại database mới
3. tạo lại toàn bộ schema
4. nạp lại toàn bộ dữ liệu

## Rủi ro và xử lý

- Nếu export theo thứ tự bảng sai có thể vỡ foreign key:
  - cần export theo dependency order hoặc tạm disable constraint rồi enable lại
- Nếu có cột identity:
  - cần `SET IDENTITY_INSERT` đúng lúc
- Nếu có dữ liệu lớn:
  - file `.sql` có thể nặng, nhưng vẫn chấp nhận được vì mục tiêu là snapshot đầy đủ

## Kiểm tra sau cùng

- chạy script trên SQL Server sạch
- xác nhận database được tạo lại thành công
- kiểm tra số lượng bảng và một số bảng dữ liệu chính
