# Quiz Learning Assessment Design

Date: 2026-05-28

## Goal

Bo sung chuc nang quiz danh gia kien thuc cho learner trong trang hoc:

- moi lesson co the co `1 lesson quiz`
- moi course co the co `1 final quiz`
- quiz dung de do muc do hieu bai va tong ket kien thuc khoa hoc
- quiz khong chan learner hoc tiep neu chua dat diem mong muon
- learner duoc lam lai quiz khong gioi han so lan
- sau khi nop bai, learner thay diem, dap an dung, va giai thich ngan

V1 chi tap trung vao quiz `trac nghiem 1 dap an dung`, duoc sinh boi AI va duoc luu san trong he thong truoc khi learner lam bai.

## Product Decisions

Nhung quyet dinh da duoc chot:

- quiz la `khuyen nghi`, khong khoa lesson tiep theo va khong khoa final course completion flow
- cau hoi do `AI tu sinh`
- V1 chi ho tro `trac nghiem 1 dap an dung`
- so cau `linh hoat theo noi dung`, nhung backend quyet dinh muc muc tieu thay vi de AI tu quyet hoan toan
- learner duoc `lam lai khong gioi han`
- sau khi nop bai, learner thay `dap an dung + giai thich`
- moi cau hoi, dap an, va giai thich phai la `tieng Viet co dau`, ngan gon, dung trong tam, khong lan man

## Existing Context

Codebase hien tai da co:

- backend `ASP.NET Core Web API` voi pattern `Controller -> Service -> Repository`
- frontend learner page tai `frontend/src/pages/CourseLearnPage.jsx`
- API `GET /api/courses/{id}/learn` tra ve course, module, lesson, video, va content
- pipeline generation cho lesson content, audio, va video
- tracking lesson completion hien dang nam o `localStorage` tren frontend, chua co progress persistence o backend

He thong hien chua co:

- concept `Quiz`
- luu ket qua danh gia cua learner
- API lay quiz, nop bai, hoac xem lich su lam quiz

Vi vay quiz nen duoc bo sung thanh mot subsystem rieng trong backend, thay vi nhoi vao bang `Lesson` hoac `Course`.

## Scope

Trong pham vi thay doi:

- them `lesson quiz` cho tung lesson co noi dung hoc hop le
- them `final quiz` cho moi course da co du du lieu de tong hop
- AI worker sinh cau hoi va dap an theo lesson/course content
- backend luu quiz, luu attempt, cham diem, va tra ket qua
- frontend learner hien thi quiz sau lesson va quiz tong ket khoa hoc
- admin thay duoc trang thai quiz va co the regenerate khi can

Ngoai pham vi thay doi:

- khong khoa learner hoc tiep dua tren diem quiz
- khong ho tro nhieu loai cau hoi trong V1
- khong co man hinh admin sua tay tung cau hoi trong V1
- khong co certificate hay graduation workflow dua tren diem quiz trong V1
- khong co anti-cheat, timer, random bank phuc tap, hoac proctoring

## Recommended Approach

Chon huong `generate truoc, luu san vao database`.

Ly do chon:

- learner mo quiz se load nhanh va on dinh
- khong phu thuoc vao AI runtime ngay thoi diem user hoc
- de thong ke va kiem soat chat luong quiz
- phu hop voi kien truc da co san cac job generate lesson/audio/video
- de retry va regenerate khi quiz loi hoac noi dung kem chat luong

Khong chon generate on-demand khi learner bam vao quiz vi:

- tang do tre
- de tao trai nghiem khong dong deu
- kho xu ly loi AI runtime
- kho dam bao final quiz on dinh giua cac attempt

## Architecture

### Backend Responsibility

Backend `ASP.NET Core` la trung tam dieu phoi:

- xac dinh khi nao can generate lesson quiz va final quiz
- goi AI worker de sinh quiz JSON
- validate chat che ket qua AI tra ve
- luu quiz va cac question/option vao DB
- tao va cham `QuizAttempt`
- tra payload quiz cho learner
- tra lich su lam bai cho learner
- cung cap action regenerate cho admin

### AI Worker Responsibility

`ai-worker` nhan lesson/course context va tra ve quiz payload JSON da chuan hoa:

- title
- danh sach question
- moi question co `4` options
- `1` option dung duy nhat
- explanation ngan gon

### Frontend Responsibility

Frontend:

- hien lesson quiz trong learn page
- hien final quiz o diem tong ket cua khoa hoc
- thu thap lua chon cua learner
- gui submit attempt
- hien diem, dap an dung, va giai thich
- hien lich su attempt co ban neu can

## Data Model

De xuat them cac entity rieng:

### `Quiz`

Fields de xuat:

- `Id`
- `LessonId` nullable
- `CourseId` nullable
- `Type` (`Lesson`, `Final`)
- `Status` (`Draft`, `Generating`, `Ready`, `Failed`, `Outdated`)
- `Title`
- `SourceContentVersion`
- `QuestionCount`
- `CreatedAt`
- `UpdatedAt`
- `LastGeneratedAt`
- `GenerationError`

Rules:

- quiz lesson gan voi `LessonId`
- quiz final gan voi `CourseId`
- mot record phai gan voi lesson hoac course, khong duoc ca hai cung null
- `Outdated` duoc dung khi lesson/course content thay doi sau lan generate truoc

### `QuizQuestion`

- `Id`
- `QuizId`
- `QuestionText`
- `Explanation`
- `OrderIndex`

### `QuizOption`

- `Id`
- `QuizQuestionId`
- `OptionText`
- `OrderIndex`
- `IsCorrect`

Rule:

- moi question phai co dung `4` options
- moi question chi duoc co `1` option `IsCorrect = true`

### `QuizAttempt`

- `Id`
- `QuizId`
- `UserId`
- `StartedAt`
- `SubmittedAt`
- `Score`
- `CorrectCount`
- `TotalQuestions`

Rule:

- learner duoc tao attempt khong gioi han
- diem la ket qua cua tung attempt, khong ghi de lich su cu

### `QuizAttemptAnswer`

- `Id`
- `QuizAttemptId`
- `QuizQuestionId`
- `SelectedOptionId`
- `IsCorrect`

## Quiz Generation Flow

### Lesson Quiz

1. Lesson content duoc generate hoac regenerate thanh cong.
2. Backend enqueue lesson quiz generation job.
3. Backend dat `Quiz.Status = Generating`.
4. Backend goi `ai-worker` voi context lesson.
5. AI worker tra ve quiz JSON.
6. Backend validate schema va quality rules.
7. Neu hop le, backend ghi de question/option hien tai va dat `Status = Ready`.
8. Neu khong hop le, backend dat `Status = Failed` va luu ly do.

### Final Quiz

1. Course da co du lesson content duoc xem la du de tong hop.
2. Backend enqueue final quiz generation job.
3. Backend goi `ai-worker` voi tong hop lesson context trong course.
4. Backend validate va luu quiz.

Final quiz phai duoc sinh truc tiep tu toan bo noi dung course, khong phai ghep co hoc tu cac lesson quiz da co. Cach nay giam trung lap va giu dung muc tieu danh gia tong quan.

### Regeneration and Staleness

- neu lesson content thay doi, lesson quiz lien quan phai chuyen sang `Outdated` va duoc regenerate
- neu nhieu lesson trong course thay doi, final quiz cua course phai chuyen sang `Outdated`
- admin co the bam `Regenerate` neu quiz `Failed`, `Outdated`, hoac can lam moi noi dung

## Question Count Rules

Backend khong de AI tu quyet dinh so cau. Backend tinh `target question count` truoc, roi truyen xuong prompt.

### Lesson Quiz

- lesson ngan: `3` cau
- lesson trung binh: `5` cau
- lesson dai: `7` cau

### Final Quiz

- course ngan: `10` cau
- course trung binh: `15` cau
- course dai: `20` cau

V1 co the xac dinh ngan/trung binh/dai dua tren:

- do dai `contentSeed`
- hoac tong do dai noi dung da generate
- hoac so slide/su kien noi dung neu du lieu do da co san

Rule nay giu quiz du ngan de learner lam nhanh, nhung van co do phu hop ly.

## AI Prompt and Validation Rules

Day la rang buoc nghiep vu bat buoc:

- toan bo cau hoi, dap an, va giai thich phai la `tieng Viet co dau`
- cau hoi phai ngan gon, ro nghia, va dung trong tam bai hoc
- khong duoc hoi lan man, meo vat, hoac thong tin ngoai pham vi lesson/course
- uu tien kiem tra:
  - khai niem trong tam
  - quy trinh chinh
  - phan biet giua cac khai niem gan nhau
  - ung dung truc tiep cua noi dung bai hoc
- tranh:
  - cau qua dai
  - phu dinh kep
  - nhieu dap an deu co ve dung
  - giai thich vo nghia hoac qua chung chung

Backend can validate it nhat:

- dung so cau theo target
- moi cau co dung `4` lua chon
- chi `1` lua chon dung
- question text, option text, explanation khong rong
- text co dau hieu la tieng Viet co dau
- do dai tung cau va explanation nam trong nguong hop ly

Neu payload vi pham cac rule tren, backend khong publish quiz do cho learner.

## API Design

### Learner APIs

- `GET /api/lessons/{lessonId}/quiz`
  - tra lesson quiz neu `Status = Ready`
  - neu quiz dang generate hoac outdated, tra payload status de frontend hien thong bao phu hop

- `GET /api/courses/{courseId}/final-quiz`
  - tra final quiz cua course

- `POST /api/quizzes/{quizId}/attempts`
  - tao mot attempt moi
  - tra `attemptId` va payload cau hoi neu can

- `POST /api/quizzes/{quizId}/attempts/{attemptId}/submit`
  - nhan dap an learner chon
  - backend cham diem
  - tra:
    - score
    - correct count
    - total count
    - tung cau dung/sai
    - dap an dung
    - explanation

- `GET /api/quizzes/{quizId}/attempts`
  - tra lich su attempt cua learner hien tai

### Admin APIs

- `POST /api/admin/quizzes/{quizId}/regenerate`
  - enqueue regenerate cho quiz

- co the bo sung `GET /api/admin/quizzes/{quizId}` neu can xem diagnostic va generation error

## Learner Experience

### Lesson Quiz Placement

Lesson quiz nen xuat hien trong `CourseLearnPage` o vi tri tu nhien:

- ben duoi phan `Noi dung bai hoc`
- hoac giua content va comments

UI can co:

- heading ro rang nhu `Kiem tra nhanh sau bai hoc`
- CTA `Lam quiz`
- loading state neu quiz dang generate
- empty/error state neu quiz chua san sang

### Final Quiz Placement

Final quiz nen hien o vung tong ket khoa hoc:

- o cuoi danh sach lesson trong sidebar
- hoac mot card tong ket rieng khi learner da hoc het lesson

No khong khoa dieu huong, nhung can du noi bat de learner biet day la buoc danh gia tong ket.

### Attempt Flow

1. Learner mo quiz.
2. Frontend tao `attempt`.
3. Learner chon dap an cho tung cau.
4. Learner bam `Nop bai`.
5. Backend cham diem va tra ket qua.
6. Frontend hien:
   - diem tong
   - so cau dung
   - dap an dung cua tung cau
   - giai thich ngan
   - nut `Lam lai`

## Admin Experience

V1 khong can man hinh sua tay tung cau hoi.

Admin chi can:

- thay trang thai quiz trong workflow quan tri lesson/course
- thay thong bao `Generating`, `Ready`, `Failed`, `Outdated`
- bam `Regenerate` khi quiz loi, cu, hoac khong dat chat luong mong muon

Neu can debug, admin co the xem generation error o muc toi thieu.

## Error Handling

Can xu ly ro cac tinh huong:

- AI tra JSON sai schema
- AI tra text khong dat rule tieng Viet co dau
- AI tao cau hoi lan man hoac khong dung trong tam
- learner mo quiz khi `Status != Ready`
- learner submit attempt khong hop le
- lesson/course content thay doi sau khi quiz da generate

Nguyen tac:

- payload loi -> `Status = Failed`
- payload cu so voi content moi -> `Status = Outdated`
- frontend gap `Generating` -> hien `Quiz dang duoc chuan bi`
- frontend gap `Failed` -> hien thong bao tam thoi va khong cho lam quiz loi
- neu submit payload sai -> backend tra `400`
- neu quiz khong ton tai hoac khong duoc phep xem -> `404`

Co the cho phep backend auto-regenerate `1` lan neu AI tra payload sai format, nhung khong nen loop vo han.

## Testing Strategy

### Backend Unit Tests

- tinh dung `target question count`
- validate dung schema quiz JSON
- validate moi cau co 4 options va 1 dap an dung
- phat hien payload khong dat rule noi dung co dau va qua dai
- cham diem attempt dung voi tung dap an chon

### Backend Integration Tests

- lay lesson quiz thanh cong khi `Status = Ready`
- lay final quiz thanh cong
- tao attempt va submit attempt
- luu lich su nhieu attempt cho cung mot user va mot quiz
- admin regenerate quiz thanh cong

### Frontend Tests

- render lesson quiz va final quiz
- hien loading, failed, not-ready states
- submit dap an va hien ket qua
- hien dap an dung va explanation sau khi nop
- cho phep lam lai

### Worker/Contract Tests

- AI worker tra dung schema JSON
- dung so cau duoc yeu cau
- toan bo text la tieng Viet co dau
- giai thich ngan va co noi dung

## Rollout Notes

Nen trien khai theo thu tu:

1. backend data model + API + cham diem
2. AI worker prompt + validation + generation orchestration
3. frontend learner cho lesson quiz
4. final quiz UI
5. admin regenerate/status surfacing

Thu tu nay giu system co the test tung lop rieng, giam rui ro khi dua quiz vao luong hoc hien tai.

## Open Constraints Chosen For V1

De tranh mo rong scope qua som, V1 co cac gioi han co y:

- chua co progress persistence tong the cho completion cua course
- chua co dashboard analytics quiz cho admin
- chua co sua tay ngan hang cau hoi
- chua co randomization bank, timer, hay anti-cheat

Nhung cau truc entity va attempt o tren da du de mo rong cac huong do ve sau ma khong pha boundary hien tai.
