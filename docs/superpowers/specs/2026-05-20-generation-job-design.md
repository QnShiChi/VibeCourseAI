# Generation Job + Generate Course Trigger Design

## Tong quan

Muc tieu cua buoc nay la noi truc tiep du lieu `Syllabus` da import sang luong tao khoa hoc, nhung van giu backend C# la trung tam nghiep vu. Admin se bam nut `Generate khoa hoc` tu man chi tiet de cuong. `ASP.NET Core Web API` se tao `GenerationJob`, doc du lieu de cuong da duoc trich text, va tao mot `Course` skeleton lam dau vao cho cac buoc phat trien tiep theo.

Pham vi cua vong nay chi bao gom:
- tao `GenerationJob`
- tao `Course` skeleton tu `Syllabus`
- trang admin xem danh sach va chi tiet job
- chan generate trung khi job cua cung syllabus dang chay

Ngoai pham vi:
- khong goi `ai-worker`
- khong can `OpenRouter API key`
- khong sinh `Module`, `Lesson`, slide, audio, video
- khong them retry workflow phuc tap

## Muc tieu nghiep vu

Sau khi import de cuong thanh cong, admin can co cach bat dau buoc sinh khoa hoc ma khong phai xu ly dong bo trong mot request dai. He thong can theo doi duoc tien trinh, lien ket ro de cuong nguon voi khoa hoc duoc tao, va hien thi duoc trang thai thanh cong hay that bai cho admin.

Ket qua mong muon cua MVP nay:
- moi de cuong co the duoc dung lam dau vao cho thao tac `Generate`
- backend tao mot job de theo doi qua trinh xu ly
- neu xu ly thanh cong, mot `Course` draft duoc tao ra
- admin xem duoc lich su job va course da tao tu tung de cuong

## Kien truc va ranh gioi trach nhiem

`ASP.NET Core Web API` la noi dieu phoi chinh:
- nhan request generate tu admin
- validate `Syllabus`
- tao va cap nhat `GenerationJob`
- tao `Course` skeleton
- tra du lieu job/course ve frontend

Frontend admin:
- bo sung nut `Generate khoa hoc` trong man `De cuong`
- bo sung trang `Generation Jobs`
- hien thi trang thai, loi, syllabus nguon va course tao ra

`ai-worker` khong tham gia o buoc nay. Muc dich la xay dung xuong song nghiep vu bang C# truoc, sau do moi noi sang Python cho cac buoc AI/video nang.

## Thiet ke du lieu

### Bang `GenerationJobs`

Them mot entity `GenerationJob` voi cac truong:
- `Id`
- `SyllabusId`
- `CourseId` nullable
- `Status`
- `ErrorMessage` nullable
- `CreatedByUserId`
- `StartedAt` nullable
- `CompletedAt` nullable
- `CreatedAt`
- `UpdatedAt`

Y nghia:
- `SyllabusId`: job nay duoc tao tu de cuong nao
- `CourseId`: khoa hoc duoc tao ra neu thanh cong
- `Status`: theo doi vong doi xu ly
- `ErrorMessage`: luu ly do that bai de admin debug
- `CreatedByUserId`: admin nao da kich hoat generate

### Gia tri `Status`

MVP nay chi dung 4 trang thai:
- `Pending`
- `Processing`
- `Completed`
- `Failed`

Danh sach trang thai chi tiet hon nhu `GeneratingOutline`, `GeneratingSlides`, `GeneratingAudio`, `RenderingVideo` duoc de danh cho giai doan noi AI worker ve sau.

### Lien ket voi `Course`

`Course` duoc tao o muc skeleton. Neu model `Course` hien tai chua co lien ket nguon, se bo sung truong phu hop de biet khoa hoc duoc sinh tu `Syllabus` nao, uu tien `SourceSyllabusId` nullable neu phu hop voi cau truc hien co.

Du lieu khoi tao cho `Course`:
- `Title`: lay tu `Syllabus.Title`
- `Description`: uu tien `Syllabus.Description`, fallback sang doan dau cua `ExtractedText`
- trang thai/publish flag: dat ve draft/chua publish theo model hien co

## API thiet ke

### `POST /api/syllabuses/{id}/generate`

- `Admin` only
- tim `Syllabus` theo `id`
- kiem tra `Syllabus` ton tai va co `ExtractedText`
- kiem tra khong co `GenerationJob` nao cua syllabus do dang o `Pending` hoac `Processing`
- tao `GenerationJob`
- thuc hien xu ly tao `Course` skeleton
- cap nhat job sang `Completed` hoac `Failed`

Phan hoi:
- `200 OK` voi thong tin job va course khi thanh cong
- `404 Not Found` neu `Syllabus` khong ton tai
- `400 Bad Request` neu du lieu de cuong khong hop le hoac khong the generate
- `409 Conflict` neu de cuong dang co job dang chay

### `GET /api/generation-jobs`

- `Admin` only
- tra danh sach job moi nhat, kem thong tin co ban cua `Syllabus` va `Course`

### `GET /api/generation-jobs/{id}`

- `Admin` only
- tra chi tiet job, gom:
  - `Status`
  - `ErrorMessage`
  - `Syllabus`
  - `Course` neu da tao
  - moc thoi gian xu ly

## Luong xu ly chi tiet

1. Admin mo man chi tiet de cuong da import.
2. Admin bam `Generate khoa hoc`.
3. Backend tim `Syllabus` va validate du lieu dau vao.
4. Backend kiem tra syllabus do khong co job dang chay.
5. Backend tao `GenerationJob` voi trang thai `Pending`.
6. Backend cap nhat job sang `Processing` va ghi `StartedAt`.
7. Backend tao `Course` skeleton tu du lieu syllabus.
8. Neu thanh cong:
   - gan `CourseId` vao job
   - cap nhat `Completed`
   - ghi `CompletedAt`
9. Neu that bai:
   - cap nhat `Failed`
   - luu `ErrorMessage`
   - van giu ban ghi job de admin theo doi lich su

## Quy tac generate lai

He thong cho phep generate lai tu cung mot syllabus, voi dieu kien khong co job nao cua syllabus do dang o `Pending` hoac `Processing`.

Ly do cua quyet dinh nay:
- phu hop cho demo va thu nghiem do an
- khong khoa cung admin khi muon sinh lai `Course` draft sau mot lan thu truoc
- don gian hon so voi viec ep moi syllabus chi duoc generate mot lan

He qua chap nhan trong MVP:
- mot syllabus co the tao ra nhieu `Course` draft qua nhieu job khac nhau
- frontend can hien ro job nao tao ra course nao

## Giao dien admin

### Mo rong man `De cuong`

Tai phan chi tiet de cuong:
- them nut `Generate khoa hoc`
- neu syllabus dang co job `Pending/Processing`, nut bi vo hieu hoa hoac API tra loi ro rang
- sau khi generate thanh cong, hien thong diep va co the dieu huong den trang `Generation Jobs` hoac refresh chi tiet

### Trang `Generation Jobs`

Them mot man admin moi de theo doi job:
- danh sach job theo thu tu moi nhat
- cot/truong hien thi:
  - tieu de de cuong
  - ten khoa hoc neu da tao
  - `Status`
  - thoi gian tao
  - thoi gian hoan thanh
- khu chi tiet job hien:
  - loi neu fail
  - lien ket syllabus nguon
  - lien ket course da tao neu co

UI tiep tuc bam `DESIGN.md` va bo component da refactor truoc do.

## Error handling

Can xu ly ro cac tinh huong sau:
- syllabus khong ton tai
- syllabus khong co `ExtractedText`
- syllabus dang co job dang chay
- tao `Course` that bai do du lieu khong hop le
- loi he thong trong qua trinh luu DB

Nguyen tac:
- job da tao phai duoc cap nhat `Failed` neu loi xay ra sau khi bat dau xu ly
- loi nghiep vu can tra message ro rang cho frontend
- khong xoa job da fail, vi can giu lich su van hanh

## Test strategy

Backend tests:
- generate thanh cong tao `GenerationJob` va `Course`
- generate voi syllabus khong ton tai tra ket qua phu hop
- generate khi da co job `Pending/Processing` bi chan
- generate gap loi trong qua trinh tao course se danh dau job `Failed`
- endpoint la `Admin` only

Frontend tests:
- man syllabus hien nut generate trong detail
- bam generate goi dung API va hien thong diep thanh cong/that bai
- man `Generation Jobs` tai va hien thi danh sach job
- route admin bi chan voi user khong phai `Admin`

## Trien khai du kien

Backend:
- them entity/repository/service/controller cho `GenerationJob`
- mo rong `SyllabusService` hoac tach mot `CourseGenerationService` de dieu phoi generate
- bo sung lien ket can thiet voi `Course`
- cap nhat `DbContext`, `DbInitializer`, DTO va tests

Frontend:
- them service API cho generation jobs
- mo rong `SyllabusesPage`
- them `GenerationJobsPage`
- cap nhat navigation admin va tests

## Tieu chi hoan thanh

Tinh nang duoc coi la hoan thanh khi:
- admin co the bam `Generate khoa hoc` tu mot syllabus da import
- backend C# tao duoc `GenerationJob`
- backend C# tao duoc `Course` skeleton va lien ket nguoc hop ly
- admin xem duoc danh sach va chi tiet job
- generate trung khi job dang chay bi chan dung
- test backend/frontend lien quan pass
