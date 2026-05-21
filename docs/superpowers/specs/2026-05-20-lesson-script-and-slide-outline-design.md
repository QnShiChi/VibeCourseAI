# Lesson Script And Slide Outline Generation Design

## Tong quan

Muc tieu cua buoc nay la nang cap he thong tu muc chi co `Course -> Module -> Lesson` va `contentSeed` sang muc co the tao noi dung bai giang dung cho cac buoc TTS va render video. Khi admin bam `Generate noi dung bai hoc` o cap `Course`, backend `ASP.NET Core Web API` se lay toan bo lesson cua course, goi `OpenRouter` de sinh `teachingScript`, `slideOutline`, va `voiceoverPlan` cho tung lesson, sau do luu vao database.

Pham vi vong nay:
- sinh noi dung bai hoc cho toan bo lesson trong mot course
- dung `OpenRouter` la duong sinh noi dung chinh
- luu script, slide outline, voiceover plan vao database
- bo sung preview va chinh sua noi dung bai hoc cho admin
- chuan bi data contract on dinh cho cac buoc TTS va render video sau nay

Ngoai pham vi:
- chua goi TTS
- chua sinh audio file
- chua render video
- chua sinh hinh anh slide that
- chua tinh learning progress

## Muc tieu nghiep vu

Sau buoc nay, moi `Lesson` can co du lieu noi dung day hoc du dung de:
- admin kiem duyet va chinh sua truoc khi xuat ban media
- cap TTS co script ro rang de doc
- cap tao slide co outline va ghi chu thuyet minh
- cap render video co metadata thoi luong va pacing de dong bo media

Ket qua mong muon:
- khong chi dung `contentSeed` tho de hoc hoac render
- moi lesson co script tieng Viet de doc duoc
- moi lesson co danh sach slide hop ly theo trinh tu giang day
- metadata voiceover du de lam dau vao cho TTS/video pipeline tiep theo

## Kien truc va ranh gioi trach nhiem

`ASP.NET Core Web API` van la trung tam xu ly:
- nhan lenh generate tu admin o cap course
- tao va cap nhat `GenerationJob`
- lap qua tung lesson trong course
- goi `OpenRouter` cho tung lesson
- validate output AI theo schema
- luu noi dung vao `Lessons`
- tong hop tien do va loi cho job

`OpenRouter` chi dong vai tro AI provider:
- nhan thong tin khoa hoc va lesson context
- sinh `teachingScript`, `slideOutline`, `voiceoverPlan`
- khong truy cap truc tiep database
- khong quan ly auth, job, hay storage

`ai-worker` tiep tuc chua tham gia o vong nay.

## Du lieu va mo rong model

Mo rong `Lessons` bang cac truong moi:
- `TeachingScript`
- `SlideOutlineJson`
- `VoiceoverPlanJson`
- `ContentGenerationStatus`
- `ContentGeneratedAt`
- `ContentGenerationError`

Muc dich:
- `TeachingScript` la script goc cho TTS
- `SlideOutlineJson` la contract de buoc tao slide dung lai
- `VoiceoverPlanJson` chua metadata pacing, tone, target audience, pronunciation notes
- `ContentGenerationStatus` cho biet lesson da duoc generate thanh cong hay chua
- `ContentGeneratedAt` phuc vu audit
- `ContentGenerationError` de admin debug truong hop fail

Trong vong nay, du lieu generated content duoc luu ngay tren `Lessons` de de truy xuat va de trien khai. Neu he thong phat trien lon hon, co the tach sang bang rieng sau.

## Flow generate noi dung bai hoc

Flow moi se la:
1. admin mo course structure va bam `Generate noi dung bai hoc`
2. backend validate course ton tai va co lesson
3. backend tao `GenerationJob` loai `GenerateLessonContent`
4. backend lap qua toan bo lesson cua course theo thu tu module/lesson
5. voi moi lesson, backend goi `OpenRouter`
6. backend validate JSON schema lesson content
7. neu hop le, backend luu vao lesson
8. cap nhat tien do job theo so lesson da xong
9. khi hoan tat, job chuyen thanh `Completed` hoac `CompletedWithWarnings`

Vong nay chi ho tro generate toan course, khong co endpoint generate rieng tung lesson. Viec chinh sua tay tung lesson sau khi generate van duoc ho tro.

## API du kien

Cho admin:
- `POST /api/courses/{id}/generate-lesson-content`
  - tao job sinh noi dung cho toan bo lesson trong course
- `GET /api/lessons/{id}/content`
  - xem script, slide outline, voiceover plan cua lesson
- `PUT /api/lessons/{id}/content`
  - cap nhat tay noi dung generated cua lesson

Co the bo sung:
- `GET /api/courses/{id}/lesson-contents`
  - tra ve tong quan content generation de hien thi tren course structure page neu can

## Schema JSON dau ra tu OpenRouter

Backend se ep model tra ve JSON theo schema cho tung lesson:
- `lessonId`
- `lessonTitle`
- `teachingScript`
- `slideOutline`
  - moi slide co:
    - `slideNumber`
    - `title`
    - `bulletPoints`
    - `speakerNotes`
- `voiceoverPlan`
  - `estimatedDurationMinutes`
  - `tone`
  - `pacing`
  - `targetAudience`
  - `pronunciationNotes`

Rang buoc validate phia backend:
- `lessonId` phai khop lesson dang duoc generate
- `lessonTitle` khong rong
- `teachingScript` khong rong
- `slideOutline` co it nhat 1 slide
- moi slide co `title` va it nhat 1 bullet point
- `speakerNotes` khong rong
- `voiceoverPlan.estimatedDurationMinutes` > 0
- cac truong text can duoc trim va khong chi la placeholder

## Prompt design

### System prompt

Model duoc dat vai tro la instructional designer va script writer cho bai giang dai hoc bang tieng Viet. Nhiem vu:
- tao script bai giang de doc duoc thanh tieng noi
- chia slide theo trinh tu day hoc logic
- viet ghi chu thuyet minh an khop voi slide
- giu ngu dien de hieu, chinh xac, khong qua ram ro
- tra ve JSON duy nhat dung schema

### Context gui cho model

Payload moi lesson gom:
- thong tin course title va course description
- module title va module description
- lesson title, lesson description, `contentSeed`
- rang buoc audience va muc tieu output
- huong dan khong sao chep nguyen van de cuong tho

### Nguyen tac prompt

- temperature thap de giam dao dong
- uu tien structured output/json schema
- script viet bang tieng Viet sach, doc tu nhien
- slide khong qua dai dong chu
- `speakerNotes` bo sung dien giai cho bullet points thay vi lap lai nguyen van
- `voiceoverPlan` phai thuc dung cho TTS, khong viet chung chung

## Generation job va tracking

Su dung lai `GenerationJob`, mo rong y nghia de ho tro loai `GenerateLessonContent`.

Job can luu hoac the hien duoc:
- `CourseId`
- tong so lesson can generate
- so lesson da generate thanh cong
- so lesson loi
- message tien do hien tai

Trang thai mong muon:
- `Pending`
- `Processing`
- `GeneratingLessonContent`
- `Completed`
- `CompletedWithWarnings`
- `Failed`

Neu co mot so lesson loi nhung van con lesson thanh cong, job nen ket thuc voi `CompletedWithWarnings` thay vi danh sap toan bo.

## Error handling va fallback

Vong nay khong dung fallback co hoc de che script/slide.

Nguyen tac:
- `OpenRouter` la nguon sinh noi dung chinh va duy nhat
- neu 1 lesson loi vi timeout, schema sai, output rong, hay loi API, lesson do duoc danh dau fail
- ghi chi tiet vao `Lesson.ContentGenerationError`
- tiep tuc sang lesson khac neu co the
- job tong hop ket qua cuoi cung

Ly do khong fallback:
- script/slide outline fallback co hoc thuong cho chat luong thap
- de du lieu xau se lam hong cac buoc TTS/video phia sau
- tot hon het la bao loi ro de admin biet lesson nao can generate lai hoac sua tay

## Preview va chinh sua cho admin

Trang `Course Structure` se duoc mo rong de admin co the:
- xem trang thai content generation cua tung lesson
- mo preview `Script`
- mo preview `Slides`
- mo preview `Voiceover`
- chinh tay noi dung va luu lai

Muc tieu cua man nay la tao mot buoc kiem duyet noi dung AI truoc khi di sang TTS va render video.

UI o vong nay nen co:
- nut `Generate noi dung bai hoc` o cap course
- badge trang thai o tung lesson: `Chua generate`, `Dang xu ly`, `Da xong`, `Loi`
- panel preview co tab hoac section rieng cho `Script`, `Slides`, `Voiceover`

## Data contract cho TTS va render video

Sau vong nay, buoc sau co the noi truc tiep vao cac truong generated content:
- `TeachingScript` -> input cho TTS
- `SlideOutlineJson` -> input cho slide/image generator
- `VoiceoverPlanJson` -> input cho pacing, tone, pronunciation trong TTS

Dieu nay giup buoc TTS/video khong phai sua lai schema nghiep vu, chi can tieu thu lai du lieu da generate va da duoc admin kiem duyet.

## Thay doi code du kien

Backend se them:
- service OpenRouter moi cho lesson content generation, hoac mo rong service hien co
- DTO/schema cho lesson generated content
- cap nhat `GenerationJob` de ho tro loai job moi va warning state
- service generate lesson content cho course
- API `generate-lesson-content`, `get lesson content`, `update lesson content`
- test cho success, partial failure, full failure, schema invalid, authorization

Frontend se them:
- action `Generate noi dung bai hoc` trong course structure page
- view preview generated content cho lesson
- form chinh sua script/slide/voiceover
- cap nhat course structure UI de hien trang thai generated content

## Test strategy

Backend test can cover:
- generate thanh cong toan bo lesson trong course
- partial failure va `CompletedWithWarnings`
- full failure va `Failed`
- OpenRouter tra JSON sai schema
- lesson error duoc ghi vao `ContentGenerationError`
- user thuong khong duoc goi admin endpoints
- update tay lesson content luu dung vao DB

Frontend test can cover:
- hien nut generate o course structure page
- hien badge trang thai lesson content
- render preview script/slides/voiceover
- luu tay noi dung lesson thanh cong
- hien loi khi generate content that bai

## Rui ro va gioi han

- chi phi AI tang theo tong so lesson trong course
- thoi gian generate se dai hon so voi generate structure
- can timeout hop ly va feedback ro cho admin
- neu `contentSeed` cua lesson qua ngheo, output AI co the van phai sua tay
- OpenRouter can duoc cau hinh key/model dung trong runtime moi test live duoc
