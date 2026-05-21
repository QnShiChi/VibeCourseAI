# OpenRouter-Powered Course Structure Generation Design

## Tong quan

Muc tieu cua buoc nay la nang cap luong `Generate khoa hoc` hien tai tu parser rule-based sang huong su dung `OpenRouter` lam nguon sinh cau truc hoc tap chinh. Khi admin bam `Generate`, backend `ASP.NET Core Web API` se lay `ExtractedText` tu `Syllabus`, gui sang `OpenRouter`, nhan ve JSON schema co cau truc `Course -> Module -> Lesson`, validate, sau do luu vao database.

Muc tieu nghiep vu la tang do chinh xac va tinh sach cua cau truc khoa hoc, dac biet voi cac de cuong dai hoc tieng Viet co layout phuc tap, bi vo dong, hoac trich xuat PDF chua dep. Rule-based parser cu se khong con la duong sinh chinh nua.

Pham vi vong nay:
- tich hop `OpenRouter` vao backend C#
- goi LLM de sinh `Course -> Module -> Lesson`
- ep model tra ve JSON dung schema
- luu structure vao DB nhu flow generate hien tai
- bo sung logging/error handling cho job AI

Ngoai pham vi:
- chua sinh slide, script, audio, video
- chua goi `ai-worker`
- chua bo sung dashboard chi phi su dung AI

## Muc tieu nghiep vu

Sau khi admin import syllabus va bam `Generate`, he thong phai tao duoc cau truc khoa hoc co chat luong cao hon dang parser thu cong:
- ten khoa hoc sach va dung ngu canh hoc phan
- mo ta khoa hoc ngan gon, hop ly
- module duoc tach theo noi dung hoc tap that su
- lesson duoc tach theo logic giang day thay vi om nguyen khoi text tho
- `contentSeed` du giau de lam dau vao cho cac buoc sinh noi dung tiep theo

Ket qua mong muon:
- giam manh truong hop fallback `Tong quan khoa hoc -> Bai 1`
- bo qua thong tin hanh chinh du thua nhu email, so dien thoai, khoa, giang vien
- uu tien cac muc hoc thuat nhu `Muc tieu`, `Noi dung hoc phan`, `Ke hoach giang day`, `Chuong`, `Bai`

## Kien truc va ranh gioi trach nhiem

`ASP.NET Core Web API` van la trung tam xu ly:
- lay `Syllabus.ExtractedText`
- dung service OpenRouter client de goi LLM
- validate JSON response
- map ket qua sang `Course`, `Module`, `Lesson`
- luu du lieu va cap nhat `GenerationJob`

`OpenRouter` chi dong vai tro AI provider:
- nhan raw text tu backend
- sinh cau truc JSON dung schema
- khong truy cap truc tiep database
- khong tham gia auth, job orchestration, hay CRUD

`ai-worker` tiep tuc khong tham gia o vong nay.

## Cau hinh he thong

Them cac env/backend options moi:
- `OPENROUTER_API_KEY`
- `OPENROUTER_MODEL`
- `OPENROUTER_BASE_URL` mac dinh `https://openrouter.ai/api/v1`
- `OPENROUTER_TIMEOUT_SECONDS`

Backend se goi:
- `POST /chat/completions`

oi voi body theo dinh dang OpenRouter chat completions.

Model se khong hardcode trong code. Backend doc model tu env de de doi chat luong/chi phi ve sau.

## Generate flow moi

Flow `POST /api/syllabuses/{id}/generate` se thanh:
1. validate syllabus ton tai va co `ExtractedText`
2. kiem tra khong co running job va chua co completed job
3. tao `GenerationJob`
4. goi OpenRouter voi prompt + extracted text
5. nhan JSON response theo schema
6. validate output AI
7. tao `Course`
8. tao `Modules`
9. tao `Lessons`
10. commit transaction
11. cap nhat `GenerationJob = Completed`

Neu bat ky buoc nao loi:
- rollback structure dang tao
- luu `GenerationJob = Failed`
- ghi `ErrorMessage`

## Schema dau ra bat buoc

AI phai tra ve JSON theo schema duoc backend chi dinh. Shape nghiep vu:

- `courseTitle`
- `courseDescription`
- `modules`
  - `title`
  - `description`
  - `lessons`
    - `title`
    - `description`
    - `contentSeed`

Rang buoc validate phia backend:
- `courseTitle` khong rong
- `courseDescription` khong rong
- co it nhat 1 module
- moi module co `title`, `description`
- moi module co it nhat 1 lesson
- moi lesson co `title`, `description`, `contentSeed`

Neu response khong dat schema nghiep vu, backend coi la loi generate.

## Prompt design

### System prompt

Model se duoc dat vai tro la mot chuyen gia phan tich de cuong hoc phan dai hoc bang tieng Viet. Nhiem vu:
- trich xuat cau truc khoa hoc phuc vu day hoc
- bo thong tin hanh chinh khong can cho noi dung bai giang
- uu tien muc hoc thuat
- tra ve JSON duy nhat, khong them giai thich tu do

### User payload

Payload gui len model gom:
- huong dan sinh `courseTitle`, `courseDescription`, `modules`, `lessons`
- quy dinh ro cach xu ly text nhieu/noi dung PDF xau
- raw `ExtractedText`

### Nguyen tac prompt

- temperature thap de giam do dao dong
- dung structured outputs/json schema neu model ho tro
- huong dan ro rang rang khong duoc copy nguyen van nhung phan hanh chinh vao title/module/lesson
- neu de cuong co `Ke hoach giang day`, uu tien dung muc do de chia lesson
- neu chi co `Noi dung hoc phan`, duoc gom thanh module/lesson hop ly theo logic giang day

## Chien luoc fallback

Duong chinh la `OpenRouter always-on`.

Tuy nhien, de he thong khong chet cung khi co loi ky thuat, backend van giu fallback cuoi cung:
- neu OpenRouter timeout
- neu API loi xac thuc/quota/rate limit
- neu model tra JSON hong
- neu model tra output rong

thi fallback sang parser rule-based noi bo.

Can luu dau vet ro tren `GenerationJob` hoac log he thong rang:
- job da dung `AI`
- hay da `fallback` sang parser noi bo

Muc tieu la phan biet chat luong du lieu dau ra khi debug.

## Error handling

Can xu ly ro cac tinh huong sau:
- thieu `OPENROUTER_API_KEY`
- khong cau hinh `OPENROUTER_MODEL`
- OpenRouter tra `401/403`
- rate limit
- timeout
- structured output khong hop le
- JSON deserialize that bai
- JSON hop le nhung khong dat yeu cau nghiep vu toi thieu

Nguyen tac:
- loi duoc dua vao `GenerationJob.ErrorMessage`
- thong diep hien thi du ro de admin biet la loi AI hay loi parser/save DB
- khong luu structure nua voi nua

## Du lieu va transaction

Sau khi backend nhan duoc output AI hop le:
- tao `Course`
- tao `Module`
- tao `Lesson`
- gan `OrderIndex` theo thu tu AI tra ve
- luu trong transaction

Neu save DB loi:
- rollback toan bo
- `GenerationJob = Failed`

## Thay doi code du kien

Backend se them:
- `OpenRouterOptions`
- `IOpenRouterCourseStructureService`
- `OpenRouterCourseStructureService`
- DTO cho request/response OpenRouter neu can
- cap nhat `CourseGenerationService` de dung OpenRouter la nguon chinh
- bo sung validation output AI
- bo sung tests cho nhieu truong hop AI success/fail/fallback

Frontend khong doi nhieu:
- tiep tuc dung man `Generation Jobs`
- tiep tuc dung man `Course Structure`
- chi can message ro hon neu generate that bai do AI

## Test strategy

Backend tests can co:
- generate thanh cong khi OpenRouter tra JSON hop le
- luu dung `Course -> Module -> Lesson`
- fail khi thieu API key/model config
- fail khi OpenRouter tra JSON sai schema
- fallback sang parser noi bo khi loi ky thuat
- job ghi dung `Completed/Failed`
- `OrderIndex` module/lesson dung theo thu tu output

Integration/runtime checks:
- generate voi de cuong PDF thuc te
- xac minh output khong con bi fallback qua tho
- xac minh title/module/lesson sach hon hien tai

## Rui ro va danh doi

Loi ich:
- chat luong cau truc cao hon rule-based parser ro ret
- phu hop muc tieu “chinh xac nhat co the”
- de cuong tieng Viet duoc xu ly linh hoat hon

Danh doi:
- phu thuoc API ngoai va internet
- ton chi phi theo so lan generate
- can logging/timeout/rate-limit handling tot
- ket qua AI van co tinh xac suat, nen van can validate nghiep vu

## Tieu chi hoan thanh

Tinh nang duoc coi la hoan thanh khi:
- backend generate structure chu yeu bang OpenRouter
- output AI duoc validate truoc khi luu
- de cuong PDF thuc te cho ra cau truc hop ly hon ro rang so voi parser cu
- admin xem duoc structure tren UI nhu hien tai
- fallback ky thuat hoat dong neu OpenRouter loi
- test backend/frontend lien quan pass
