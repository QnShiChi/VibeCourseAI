# Syllabus Import Module Design

## Context

He thong hien da co auth va role `Admin/User`, cung voi frontend shell va dashboard admin co ban. Tuy nhien, chuc nang nghiep vu cot loi de khoi dong flow tao khoa hoc van chua ton tai: `Admin` import de cuong vao he thong de lam dau vao cho cac buoc sinh khoa hoc sau nay.

Nguoi dung da yeu cau ro:
- Chi role `Admin` moi co quyen import de cuong
- Chuc nang nay phai nam trong khu vuc dashboard admin
- Vong nay phai trien khai luon muc `Upload file + trich text ngay cho pdf/docx/txt`
- Backend cot loi van phai dua chu yeu tren `ASP.NET Core Web API`

## Scope

In scope:
- Model va bang `Syllabuses` trong backend C#
- API admin-only de upload, xem danh sach, xem chi tiet, xoa de cuong
- Luu file de cuong vao `storage/syllabuses/`
- Trich text ngay sau upload cho `txt`, `docx`, `pdf`
- Luu `ExtractedText` vao database
- UI admin trong frontend de import va quan ly de cuong

Out of scope:
- Generation job tu de cuong
- AI worker tham gia xu ly de cuong
- Module, lesson, video generation
- OCR cho PDF scan image-only
- Tailwind migration hoac refactor frontend ngoai pham vi man admin import syllabus

## Goals

1. Tao dau vao nghiep vu chuan cho cac buoc generation sau nay
2. Giu backend nghiep vu chinh o `ASP.NET Core Web API`
3. Dam bao chi `Admin` co the upload va quan ly de cuong
4. Co text trich xuat san de phuc vu buoc sinh khoa hoc tiep theo

## Non-goals

- Xu ly moi loai PDF phuc tap hoan hao
- OCR cho file scan
- Sinh khoa hoc ngay trong cung vong nay
- Dua logic trich text sang Python worker

## Design Direction

Module nay se duoc thiet ke theo dung pattern backend hien co:
- `Controller -> Service -> Repository -> DbContext`
- DTO request/response rieng cho syllabus
- Role authorization tai controller/API layer
- Luu file vat ly + metadata + extracted text trong backend C#

Python `ai-worker` khong tham gia buoc nay. Toan bo luong import de cuong va trich text duoc giai quyet trong `ASP.NET Core Web API` de giu backend C# la cot loi cua do an.

## Data Model

Them entity `Syllabus` voi cac truong:
- `Id`
- `Title`
- `Description`
- `OriginalFileName`
- `StoredFileName`
- `FilePath`
- `FileType`
- `FileSize`
- `ExtractedText`
- `UploadedByUserId`
- `UploadedByUser`
- `CreatedAt`
- `UpdatedAt`

Quan he:
- Mot `User` co the upload nhieu `Syllabus`
- `Course` da co san truong `SyllabusId`, se duoc su dung ve sau khi sinh khoa hoc

## Backend Architecture

### 1. API Endpoints

Tat ca endpoint deu la `Admin` only:
- `POST /api/syllabuses/import`
- `GET /api/syllabuses`
- `GET /api/syllabuses/{id}`
- `DELETE /api/syllabuses/{id}`

`POST /api/syllabuses/import` nhan `multipart/form-data`:
- `title`
- `description`
- `file`

Luong xu ly:
1. Validate role `Admin`
2. Validate file ton tai va hop le
3. Validate extension nam trong `pdf/docx/txt`
4. Sinh ten file luu noi bo
5. Luu file vao `storage/syllabuses/`
6. Trich text dua tren loai file
7. Tao ban ghi `Syllabus`
8. Tra response metadata + preview text rut gon neu can

### 2. Service Layer

`SyllabusService` se chiu trach nhiem:
- validate nghiep vu upload
- goi file storage helper
- goi text extraction helper
- tao entity va luu database
- tra response DTO

Khong de controller chua logic xu ly file hoac trich text.

### 3. Repository Layer

`SyllabusRepository` se chiu trach nhiem:
- tao syllabus moi
- lay danh sach syllabus
- lay syllabus theo id
- xoa syllabus
- save changes

Danh sach se duoc sap xep theo `CreatedAt` giam dan de admin thay file moi nhat truoc.

### 4. File Storage

File se duoc luu tai:
- `storage/syllabuses/`

Ten file vat ly se la ten sinh moi de tranh trung lap va tranh phu thuoc ten file nguoi dung.

Database se luu:
- ten file goc
- ten file da luu
- duong dan tuong doi
- mime/extension
- kich thuoc file

### 5. Text Extraction Strategy

`txt`:
- doc truc tiep bang .NET text reader

`docx`:
- dung `DocumentFormat.OpenXml`
- doc text tu body cua Word document

`pdf`:
- dung thu vien .NET de extract text tu PDF
- muc tieu o vong nay la text extraction co ban cho file PDF co text layer
- khong ho tro OCR cho PDF scan image-only

Neu extraction that bai:
- upload se that bai va tra loi ro rang cho admin
- khong tao ban ghi syllabus nua voi extracted text rong, de tranh du lieu nua vung

## DTOs

### Request

`ImportSyllabusRequest`:
- `Title`
- `Description`
- `IFormFile File`

### Response

`SyllabusListItemResponse`:
- `Id`
- `Title`
- `OriginalFileName`
- `FileType`
- `FileSize`
- `CreatedAt`
- `UploadedByName`

`SyllabusDetailResponse`:
- tat ca metadata chinh
- `ExtractedText`

`ImportSyllabusResponse`:
- metadata sau upload
- thong tin extraction co ban

## Frontend Admin UX

Chuc nang nam trong khu vuc admin, boi canh dashboard.

Frontend se co:
- route admin cho `Syllabus Import`
- form upload gom:
  - title
  - description
  - file picker
- danh sach syllabus da import
- khu chi tiet syllabus voi preview extracted text

Visual phai tiep tuc dung shared UI system da duoc refactor theo `DESIGN.md`:
- card
- input
- button
- section header
- alert state

## Security and Validation

- Chỉ `Admin` duoc goi API syllabus
- Validate extension `pdf/docx/txt`
- Validate file khong rong
- Gioi han dung luong upload hop ly
- Khong cho path traversal
- File name luu noi bo phai do he thong sinh
- Delete API xoa ca DB record va file vat ly neu ton tai

## Error Handling

Can tra loi ro rang cho cac truong hop:
- khong co quyen admin
- file khong hop le
- extension khong duoc ho tro
- luu file that bai
- extract text that bai
- syllabus khong ton tai

API response khong can qua phuc tap, nhung phai nhat quan voi pattern hien co cua project.

## Testing Strategy

Backend:
- unit test service upload cho file hop le
- unit test reject extension khong hop le
- unit test reject khi extraction that bai
- controller test cho admin authorization va bad request cases

Frontend:
- test render form upload
- test list state/empty state co ban neu co thoi gian

Manual verification:
- upload `txt`
- upload `docx`
- upload `pdf`
- xem danh sach
- xem chi tiet text da trich
- xoa syllabus

## Risks

1. PDF extraction quality
   - Chi dam bao tot voi PDF co text layer
   - Khong cover OCR o vong nay

2. Them thu vien doc file vao backend
   - Can chon thu vien .NET phu hop, on dinh, de tich hop

3. File handling trong docker volume
   - Can bao dam duong dan `storage/syllabuses/` hoat dong trong container backend

## Success Criteria

Module dat neu:
- `Admin` upload duoc `pdf/docx/txt`
- File duoc luu vao storage
- `ExtractedText` duoc luu vao DB
- Admin xem duoc danh sach va chi tiet de cuong
- Chi `Admin` moi truy cap duoc chuc nang nay
- Toan bo luong nghiep vu chinh nam trong `ASP.NET Core Web API`

## Next Dependency

Sau khi module nay xong, no tro thanh dau vao truc tiep cho buoc tiep theo:
- `Generate Course from Syllabus`
- `Generation Jobs`
- `Course -> Module -> Lesson` pipeline
