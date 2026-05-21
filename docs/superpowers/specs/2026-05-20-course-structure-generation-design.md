# Course Structure Generation Design

## Tong quan

Muc tieu cua buoc nay la mo rong luong `Generate khoa hoc` hien tai tu muc tao `Course` skeleton sang muc tao day du cau truc `Course -> Module -> Lesson` o dang skeleton. Toan bo xu ly van duoc giu trong `ASP.NET Core Web API`, khong goi `ai-worker` va khong can `OpenRouter` o vong nay.

Tinh nang nay se bien mot `Syllabus` da import va da trich text thanh mot cau truc khoa hoc co the xem duoc va sua duoc tren admin dashboard. Day la buoc hoan thien du lieu cot loi truoc khi di sang sinh slide, script, audio va video.

Pham vi vong nay:
- them entity `Module`
- them entity `Lesson`
- mo rong `Generate` de tao `Course -> Module -> Lesson`
- them API xem chi tiet structure cua course
- them admin UI de xem va sua skeleton structure

Ngoai pham vi:
- khong sinh noi dung slide
- khong sinh script bai giang hoan chinh
- khong sinh audio/video
- khong goi `ai-worker`
- khong goi LLM

## Muc tieu nghiep vu

Sau khi admin import syllabus va bam `Generate`, he thong khong chi tao ra mot `Course` draft ma con tao duoc bo khung hoc tap gom module va lesson. Cau truc nay phai du de:
- admin xem duoc noi dung da sinh
- admin sua tay neu can
- lam dau vao cho cac buoc AI va media ve sau

Ket qua mong muon:
- moi `Course` duoc generate se co it nhat 1 `Module`
- moi `Module` se co it nhat 1 `Lesson`
- moi `Lesson` co `ContentSeed` de lam dau vao cho cac buoc sau
- thu tu module va lesson duoc luu bang `OrderIndex`

## Kien truc va ranh gioi trach nhiem

`ASP.NET Core Web API` la trung tam xu ly:
- doc `Syllabus.ExtractedText`
- phan tich va sinh `Module` / `Lesson` skeleton theo rule-based parser
- luu tat ca vao database trong mot transaction
- cap nhat `GenerationJob`

Frontend admin:
- xem duoc structure cua course da tao
- sua metadata co ban cua module va lesson
- khong tham gia xu ly parser

`ai-worker` khong tham gia o vong nay. Muc tieu la hoan thien backbone du lieu o phia C# truoc.

## Thiet ke du lieu

### Bang `Modules`

Them entity `Module`:
- `Id`
- `CourseId`
- `Title`
- `Description`
- `OrderIndex`
- `CreatedAt`
- `UpdatedAt`

### Bang `Lessons`

Them entity `Lesson`:
- `Id`
- `ModuleId`
- `Title`
- `Description`
- `OrderIndex`
- `ContentSeed`
- `VideoUrl` nullable
- `AudioUrl` nullable
- `Duration` nullable
- `CreatedAt`
- `UpdatedAt`

### Lien ket du lieu

- 1 `Course` co nhieu `Modules`
- 1 `Module` co nhieu `Lessons`
- `OrderIndex` quy dinh thu tu hien thi
- `ContentSeed` giu lai phan text nguon cua lesson de dung cho cac buoc sinh noi dung ve sau

## Generate flow duoc mo rong

`POST /api/syllabuses/{id}/generate` se duoc mo rong nhu sau:
1. validate syllabus
2. tao `GenerationJob`
3. tao `Course`
4. parse `ExtractedText` thanh `Modules` va `Lessons`
5. luu tat ca trong mot transaction
6. neu thanh cong, job `Completed`
7. neu loi, rollback va job `Failed`

Trang thai job o vong nay van giu bo nho gon hien tai:
- `Pending`
- `Processing`
- `Completed`
- `Failed`

Viec tach nho hon nhu `GeneratingModules`, `GeneratingLessons` se de o vong sau.

## Rule-based parser

Parser se uu tien tinh on dinh hon la thong minh:

### Buoc 1: Chuan hoa text
- trim khoang trang
- bo dong rong thua
- tach text thanh cac line va block co nghia

### Buoc 2: Nhan dien heading
Tim cac mau heading nhu:
- `Chuong 1`, `Chuong 2`
- `Phan 1`, `Phan 2`
- `Module 1`, `Module 2`
- `Unit 1`, `Unit 2`
- `Bai 1`, `Bai 2`
- `Lesson 1`, `Lesson 2`

Quy tac:
- heading lon (`Chuong`, `Phan`, `Module`, `Unit`) tao `Module`
- heading nho (`Bai`, `Lesson`) tao `Lesson` trong module hien tai

### Buoc 3: Fallback khi de cuong khong ro heading
Neu khong nhan dien duoc heading hop le:
- tao 1 `Module` mac dinh, vi du `Tong quan khoa hoc`
- chia `ExtractedText` thanh mot vai `Lesson` skeleton dua tren block text
- moi lesson lay mot doan `ContentSeed` dai vua phai

### Buoc 4: Description va ContentSeed
- `Module.Description` co the la dong mo ta ngan hoac tom tat block dau tien
- `Lesson.Description` la tom tat ngan cua block
- `Lesson.ContentSeed` giu text block goc

## API thiet ke

### Query structure
Them cac API:
- `GET /api/courses/{id}`
  - tra course detail kem modules va lessons neu muon tong hop
- `GET /api/courses/{id}/structure`
  - tra rieng cay `Course -> Module -> Lesson`

### Chinh sua skeleton
Them API:
- `PUT /api/modules/{id}`
- `PUT /api/lessons/{id}`

Chi cho phep sua:
- `Title`
- `Description`
- `OrderIndex`

Vong nay chua can full CRUD tao tay / xoa tay.

## Transaction va tinh nhat quan

Buoc generate structure phai duoc dat trong transaction.

Nguyen tac:
- neu tao `Course`, `Module`, `Lesson` thanh cong het thi moi commit
- neu loi o bat ky buoc nao trong parser hoac save DB, rollback toan bo structure vua tao
- `GenerationJob` phai phan anh dung ket qua cuoi cung

Muc tieu la tranh du lieu nua vo i nua, vi no se lam kho cac buoc media ve sau.

## UI admin

Them man admin xem structure cua course:
- hien `Course` title
- hien danh sach module
- hien lesson trong tung module
- sua nhanh `Title`, `Description`, `OrderIndex`

UI se tiep tuc bam theo design system hien co tu `DESIGN.md`.

Khong can editor phuc tap o vong nay. Muc tieu chi la xem va dieu chinh skeleton structure.

## Error handling

Can xu ly cac tinh huong:
- syllabus khong co `ExtractedText`
- parser khong tim duoc heading nao
- parser tim duoc heading nhung cho ra cau truc rong
- loi khi save module/lesson

Nguyen tac:
- neu parser khong tim duoc heading, dung fallback thay vi fail ngay
- chi fail khi khong the tao mot structure hop le toi thieu
- loi duoc luu vao `GenerationJob.ErrorMessage`

## Test strategy

Backend tests:
- generate structure thanh cong voi syllabus co heading ro
- fallback structure thanh cong voi syllabus khong co heading ro
- `OrderIndex` cua module va lesson tang dung
- `Lesson.ContentSeed` duoc luu
- loi save DB se rollback va job `Failed`

Frontend tests:
- admin xem duoc structure page
- admin sua duoc title/description/order cua module
- admin sua duoc title/description/order cua lesson
- hien thi empty/error state dung

## Trien khai du kien

Backend:
- them models, repositories, services, DTO cho `Module` va `Lesson`
- mo rong `CourseGenerationService`
- them parser service hoac helper rieng de tach trach nhiem
- cap nhat `DbContext`, `DbInitializer`, controllers, tests

Frontend:
- them page course structure admin
- them API service lay structure va cap nhat module/lesson
- cap nhat route admin va tests

## Tieu chi hoan thanh

Tinh nang duoc coi la hoan thanh khi:
- generate course tu syllabus tao duoc `Course -> Module -> Lesson`
- structure duoc luu nhat quan trong DB
- admin xem duoc structure tren giao dien
- admin sua duoc metadata co ban cua module va lesson
- parser co fallback hop le khi de cuong xau
- test backend/frontend lien quan pass
