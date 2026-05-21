# Course Publish And Learn Page Design

## Tong quan

Muc tieu cua vong nay la mo rong he thong tu muc chi co admin generate va xem course structure sang muc co the dua khoa hoc ra giao dien hoc tap thuc te. Sau khi admin generate va ra soat course, admin co the `publish/unpublish` khoa hoc. Khi course da publish, no se xuat hien o trang `Khóa học` cho user. Khi user bam vao course, he thong mo trang hoc tap voi bo cuc:
- panel noi dung lesson o ben trai
- sidebar `Noi dung khoa hoc` o ben phai
- lesson duoc nhom theo module
- module co collapse/expand
- lesson active duoc highlight ro rang

Thiet ke nay bam theo huong UX cua nen tang hoc online nhu hinh minh hoa, nhung van giu language cua `DESIGN.md` cho toan he thong hien co.

## Muc tieu nghiep vu

Sau vong nay, he thong phai cho phep:
- admin xem course da sinh va quyet dinh publish hay unpublish
- user chi thay duoc cac course da publish
- admin thay duoc ca draft va published course
- user/co admin mo duoc trang hoc cua course
- trong trang hoc, user thay duoc module va lesson theo cau truc da generate

Day la buoc bien du lieu backend hien co thanh trai nghiem hoc thuc te, truoc khi di tiep sang video player that, progress, note, va quiz.

## Kien truc va ranh gioi trach nhiem

Backend `ASP.NET Core Web API` van la trung tam xu ly:
- quan ly publish state cua course
- tra danh sach course tuy theo role
- tra du lieu `learn page` cua course gom modules va lessons
- chan user thuong truy cap draft course

Frontend:
- render course cards o trang `Khóa học`
- render learn page voi panel trai + sidebar phai
- quan ly state collapse module va lesson dang chon

Khong can `ai-worker` trong vong nay.

## Du lieu va quy tac truy cap

### Publish state

Tiep tuc dung truong da co:
- `Course.IsPublished`

Khong can them cot moi o vong nay.

### Quy tac role

- `User`
  - chi nhin thay course `IsPublished = true`
  - chi truy cap duoc learn page cua course da publish
- `Admin`
  - thay tat ca course
  - co the publish/unpublish
  - co the preview learn page cua draft course

Neu user thuong co gang mo draft course:
- backend tra `404` de tranh lo thong tin course nhap

## API thiet ke

### Admin APIs

- `GET /api/courses/admin`
  - tra tat ca course cho admin
  - moi item co `IsPublished`
  - co the bo sung so module/lesson neu can

- `PUT /api/courses/{id}/publish`
  - set `IsPublished = true`

- `PUT /api/courses/{id}/unpublish`
  - set `IsPublished = false`

### Learner/Public-auth APIs

- `GET /api/courses/published`
  - tra danh sach course da publish
  - user dung endpoint nay o trang `Khóa học`
  - admin cung co the dung endpoint nay neu can xem perspective cua user

- `GET /api/courses/{id}/learn`
  - tra du lieu cho learn page
  - admin xem duoc ca draft va published
  - user thuong chi xem duoc published

## DTO learn page

Learn page response can bao gom:
- `courseId`
- `courseTitle`
- `courseDescription`
- `isPublished`
- `selectedLessonId` mac dinh
- `selectedLesson`
  - `lessonId`
  - `lessonTitle`
  - `description`
  - `contentSeed`
  - `videoUrl`
  - `duration`
- `modules`
  - `moduleId`
  - `moduleTitle`
  - `moduleDescription`
  - `orderIndex`
  - `lessons`
    - `lessonId`
    - `lessonTitle`
    - `description`
    - `orderIndex`
    - `videoUrl`
    - `duration`

`isCollapsed` khong luu backend, chi la state frontend.

## Trang `Khóa học`

Trang `Khóa học` se lay du lieu that tu backend thay vi demo cards.

### Hien thi cho user

- chi hien course da publish
- card course dang hoc/da publish theo grid
- CTA `Xem khóa học`

### Hien thi cho admin

- thay ca draft va published
- card draft co badge ro rang `Draft`
- card published co badge `Published`
- co the them action `Publish/Unpublish` ngay tren card hoac trong chi tiet

### Giao dien card

Moi card gom:
- visual cover gradient/placeholder
- title
- mo ta ngan
- badge status
- so module / so lesson neu co
- nut vao chi tiet hoc

Huong visual se tham khao cach gom card khoa hoc nhu anh minh hoa, nhung van bam system design cua du an hien tai.

## Learn page UI

Learn page co bo cuc co dinh theo hai cot:

### Cot trai

Vung noi dung lesson chinh:
- title course o muc nhe hoac header
- title lesson hien tai
- mo ta lesson
- placeholder player frame
- noi dung `contentSeed` hoac preview bai hoc

O vong nay, neu chua co video that:
- hien `placeholder player`
- hien lesson content thay cho player that

Muc tieu la giao dien hoc tap da dung cau truc, de sau nay thay bang video that ma khong phai thay doi layout lon.

### Cot phai

Sidebar `Noi dung khoa hoc`:
- danh sach module theo thu tu
- moi module co collapse/expand
- lesson nam ben trong module
- lesson active duoc highlight
- click lesson thi panel trai doi theo

### Hanh vi frontend

- khi vao course, tu dong chon lesson dau tien cua module dau tien
- module chua lesson dang chon tu dong mo
- khi click module header, collapse/expand
- khi click lesson, update panel trai va active state

## Publish/unpublish UX

Admin co the publish theo 1 trong 2 cho sau:
- trang `Khóa học` admin
- hoac learn/structure page

O vong nay, uu tien don gian:
- co action publish/unpublish tren khu admin courses list hoac course detail header

Can feedback ro rang:
- `Đã publish khóa học`
- `Đã chuyển khóa học về draft`

## Error handling

Can xu ly cac tinh huong:
- course khong ton tai
- user thuong mo draft course
- course chua co module/lesson
- loi load danh sach course
- loi publish/unpublish

Nguyen tac:
- user thuong mo draft -> `404`
- admin duoc thong bao loi ro neu publish/unpublish that bai
- learn page neu course rong thi hien empty state ro rang

## Test strategy

Backend tests:
- admin xem duoc tat ca course
- user chi lay duoc published courses
- publish API dat `IsPublished = true`
- unpublish API dat `IsPublished = false`
- user mo draft course bi `404`
- admin mo draft course thanh cong
- learn response tra dung modules va lessons

Frontend tests:
- course list render du lieu tu API
- admin thay badge `Draft/Published`
- user khong thay draft course
- learn page render lesson dau tien mac dinh
- click lesson cap nhat panel trai
- module collapse/expand hoat dong

## Thay doi code du kien

Backend:
- mo rong `CourseService`
- mo rong `CoursesController`
- them DTO list/admin/learn page
- them tests controller/service cho publish va learn permissions

Frontend:
- thay `CoursesPage` demo bang du lieu that
- them service API cho published/admin/learn/publish/unpublish
- them `CourseLearnPage`
- cap nhat routes
- cap nhat tests

## Tieu chi hoan thanh

Tinh nang duoc coi la hoan thanh khi:
- admin publish/unpublish duoc course
- user chi thay course da publish tren trang `Khóa học`
- admin thay ca draft va published
- click vao course mo duoc learn page
- learn page hien module/lesson theo sidebar collapse
- lesson active thay doi dung khi nguoi dung chon
- test backend/frontend lien quan pass
