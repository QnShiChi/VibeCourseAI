# Checklist Chuc Nang He Thong VibeCourseAI

Tai lieu nay duoc tach ra tu [tailieuhethong.md](/home/phan-duong-quoc-nhat/workspace/VibeCourseAI/tailieuhethong.md) de theo doi tien do thuc hien theo tung nhom chuc nang. Lam toi dau tick toi do.

## Cach dung

- `[ ]` Chua lam
- `[-]` Dang lam
- `[x]` Da hoan thanh

## Trang thai ra soat hien tai

- Ra soat tren codebase ngay `2026-05-20`
- Danh dau duoc cap nhat theo bang chung hien co trong repo, khong dua tren ke hoach
- Muc `[-]` nghia la da co skeleton, mot phan logic, hoac da co khung nhung chua hoan thien end-to-end
- Luu y hien tai he thong dang dung `SQL Server`, chua phai `MySQL` nhu trong `tailieuhethong.md`
- Luu y frontend hien chua thay `Tailwind CSS`, nhung cac man hien co da duoc refactor de bam token/component rules cua `DESIGN.md`

## 1. Nen tang he thong va kien truc

- [x] Khoi tao cau truc dich vu gom `backend/`, `frontend/`, `ai-worker/`, `storage/`, `docker-compose.yml`
- [x] Chuan hoa kien truc backend theo huong `Controller -> Service/ImplService -> Repository/ImplRepository -> Database`
- [x] Thiet lap DTO request/response cho cac API chinh
- [ ] Cau hinh RESTful API convention va versioning neu can
- [x] Tich hop Swagger de test va mo ta API
- [ ] Cau hinh logging co ban cho backend va ai-worker
- [ ] Xay dung co che xu ly loi va tra response thong nhat
- [x] Cau hinh Docker cho backend
- [x] Cau hinh Docker cho frontend
- [ ] Cau hinh Docker cho MySQL
- [x] Cau hinh Docker cho ai-worker
- [x] Cau hinh mount volume cho `storage/`
- [x] Dam bao toan bo he thong co the chay bang `docker-compose`

## 2. Xac thuc va phan quyen

- [x] Thiet ke bang `Users`
- [x] Thiet ke bang `Roles`
- [x] Seed du lieu role mac dinh `Admin`, `User`
- [x] API dang ky nguoi dung
- [x] API dang nhap
- [x] API refresh token
- [x] API logout
- [x] API lay thong tin nguoi dung hien tai
- [x] JWT authentication cho backend
- [x] Role-based authorization cho `Admin`
- [x] Role-based authorization cho `User`
- [x] Chan nguoi dung bi khoa dang nhap hoac su dung he thong

## 3. Quan ly nguoi dung

- [x] Admin xem danh sach user
- [ ] Admin xem chi tiet user
- [ ] Admin doi role user
- [x] Admin khoa/mo khoa user
- [x] User xem profile ca nhan
- [ ] User cap nhat profile ca nhan

## 4. Import de cuong

- [x] Thiet ke bang `Syllabuses`
- [x] API upload de cuong qua file
- [x] Ho tro cac dinh dang dau vao can dung nhu `pdf`, `docx`, `txt`
- [ ] Ho tro nhap de cuong bang text truc tiep neu can
- [x] Luu metadata de cuong: tieu de, mo ta, ten file goc, loai file, nguoi upload
- [x] Luu file de cuong vao `storage/`
- [x] Trich xuat noi dung text tu de cuong
- [x] Luu `ExtractedText` vao database
- [x] Admin xem danh sach de cuong da import
- [x] Admin xem chi tiet de cuong
- [x] Admin xoa de cuong

## 5. Tao khoa hoc tu de cuong

- [x] API generate course tu `syllabusId`
- [x] Tao `generation job` khi admin bam generate
- [x] Thiet ke bang hoac co che luu job xu ly nen
- [-] Trang thai job co it nhat: `Pending`, `Processing`, `GeneratingOutline`, `GeneratingSlides`, `GeneratingAudio`, `RenderingVideo`, `Completed`, `Failed`
- [x] Admin xem danh sach job tao khoa hoc
- [x] Admin xem chi tiet tung job
- [x] Luu log loi/ly do that bai cho moi job
- [ ] Co che retry job that bai neu can
- [x] Ho tro generate structure bang OpenRouter theo schema JSON

## 6. Quan ly khoa hoc, module, lesson

- [x] Thiet ke bang `Courses`
- [x] Thiet ke bang `Modules`
- [x] Thiet ke bang `Lessons`
- [x] Tao tu dong cau truc `Course -> Module -> Lesson` tu de cuong
- [x] API xem danh sach khoa hoc
- [x] API xem chi tiet khoa hoc
- [ ] API tao khoa hoc thu cong neu can
- [ ] API cap nhat khoa hoc
- [ ] API xoa khoa hoc
- [x] API publish khoa hoc
- [x] API unpublish khoa hoc
- [x] API xem danh sach lesson theo khoa hoc
- [x] API xem chi tiet lesson
- [x] API cap nhat lesson
- [ ] API xoa lesson
- [x] Quan ly thu tu hien thi `OrderIndex` cho module va lesson

## 7. AI worker va xu ly noi dung bai giang

- [x] Xay dung `ai-worker` tach rieng khoi backend
- [ ] Co che backend gui job sang ai-worker
- [x] Co cau hinh AI provider ngoai thong qua OpenRouter cho bước phan tich de cuong
- [ ] Phan tich de cuong de sinh outline bai giang
- [x] Sinh noi dung slide cho tung lesson
- [x] Sinh script giang day cho tung lesson
- [ ] Quy dinh cau truc du lieu dau ra giua backend va ai-worker
- [ ] Kiem soat gioi han do dai noi dung de tranh job qua nang
- [ ] Co che xu ly khi trich xuat de cuong loi hoac thieu noi dung

## 8. Tao slide, audio va video

- [ ] Chon chien luoc tao slide: HTML-to-image hoac giai phap tuong duong
- [ ] Sinh slide/image cho tung lesson
- [ ] Tich hop VibeVoice de tao audio TTS
- [ ] Cau hinh giong doc phu hop cho bai giang
- [ ] Luu file audio vao `storage/`
- [ ] Tich hop FFmpeg de ghep slide + audio thanh video
- [ ] Luu file video vao `storage/`
- [ ] Luu `AudioUrl`, `VideoUrl`, `Duration` vao lesson
- [ ] Kiem tra chat luong dong bo giua slide va audio
- [ ] Xu ly truong hop render video that bai

## 9. Hoc tap va theo doi tien do hoc vien

- [x] User xem danh sach khoa hoc da publish
- [x] User xem chi tiet khoa hoc
- [x] User xem danh sach module trong khoa hoc
- [x] User xem danh sach lesson trong module
- [ ] User xem video bai giang
- [ ] Thiet ke bang `LearningProgress`
- [ ] API danh dau hoan thanh lesson
- [ ] API lay tien do hoc theo khoa hoc
- [ ] Tinh toan phan tram hoan thanh khoa hoc
- [ ] Hien thi tien do hoc tap tren giao dien hoc vien

## 10. Media va luu tru

- [ ] Quy uoc cau truc thu muc luu `syllabuses`, `slides`, `audio`, `videos`
- [ ] Luu URL hoac path file thong nhat trong database
- [ ] Co che xoa file vat ly khi xoa du lieu lien quan neu duoc phep
- [ ] Gioi han dung luong upload
- [ ] Kiem tra dinh dang file hop le
- [ ] Bao ve truy cap file noi bo neu co file khong duoc public

## 11. Frontend cho Admin

- [x] Trang dang nhap admin
- [x] Dashboard tong quan cho admin
- [ ] Trang quan ly nguoi dung
- [x] Trang import de cuong
- [x] Trang danh sach de cuong
- [x] Trang chi tiet de cuong
- [x] Nut generate khoa hoc tu de cuong
- [x] Trang theo doi trang thai generation job
- [x] Trang danh sach khoa hoc
- [x] Trang chi tiet khoa hoc/module/lesson da tao
- [ ] Trang xem video bai giang da render
- [x] Chuc nang publish/unpublish khoa hoc tren giao dien admin
- [x] Admin preview va chinh sua noi dung AI cua lesson

## 12. Frontend cho User

- [x] Trang dang ky
- [x] Trang dang nhap
- [x] Trang danh sach khoa hoc
- [x] Trang chi tiet khoa hoc
- [x] Giao dien xem module va lesson
- [ ] Giao dien xem video bai hoc
- [ ] Nut danh dau da hoc
- [ ] Hien thi tien do hoc tap theo khoa hoc
- [x] Trang profile ca nhan

## 13. Database va du lieu

- [-] Tao migration cho cac bang chinh
- [x] Quan he giua `Users`, `Roles`, `Syllabuses`, `Courses`, `Modules`, `Lessons`, `LearningProgress`, `GenerationJobs`
- [ ] Cau hinh index cho cac truong tim kiem chinh
- [x] Seed du lieu mau toi thieu de test
- [ ] Kiem tra nhat quan du lieu khi xoa/cap nhat cac doi tuong lien quan

## 14. Kiem thu va xac nhan chat luong

- [x] Unit test cho Auth service
- [ ] Unit test cho User service
- [x] Unit test cho Syllabus service
- [x] Unit test cho Course generation flow
- [x] Unit test cho parser cau truc khoa hoc
- [x] Unit test cho Module service
- [x] Unit test cho Lesson service
- [x] Unit test cho OpenRouter course structure service
- [ ] Unit test cho Learning progress service
- [-] API test cho cac endpoint chinh
- [-] Test upload de cuong that bai va file khong hop le
- [x] Test luong generate khoa hoc thanh cong
- [x] Test luong generate khoa hoc that bai
- [x] Test phan quyen Admin/User
- [x] Test user chi xem duoc khoa hoc da publish
- [ ] Test video playback va cap nhat tien do hoc

## 15. Design system compliance bat buoc theo `DESIGN.md`

### 15.1 Nen tang design token

- [x] Khai bao central design tokens cho mau sac, typography, spacing, radius, shadow theo `DESIGN.md`
- [-] Ap dung `Satoshi` lam font chinh toan he thong
- [x] Dung dung cac token mau chinh: `#000000`, `#ffffff`, `#171717`, `#0a0a0d`, `#a3e635`
- [x] Dung dung spacing scale 4/8/12/16/24/32/40... theo tai lieu
- [x] Dung dung radius 4px, 8px, 100px dung vai tro component
- [x] Dung dung shadow nhe kieu offset theo tai lieu, khong dung shadow manh

### 15.2 Quy tac giao dien tong the

- [x] Toan bo UI theo light theme lam mac dinh
- [x] CTA chinh dung `Accent Green #a3e635`
- [x] Khong dung dark background cho cac vung noi dung chinh
- [-] Khong dung font he thong thay cho `Satoshi` neu chua co ly do ro rang
- [x] Khong dung border-radius tuy y ngoai bo quy tac da dinh
- [x] Gradient chi dung cho hero hoac khu vuc trang tri phu hop, khong lam lan tren toan bo man hinh
- [x] Hinh anh/illustration theo huong abstract hoac explanatory, khong lam giao dien nang ve photography

### 15.3 Component checklist

- [x] Button chinh dung style primary action button
- [x] Button phu dung style ghost action button
- [x] Badge/tag dung pill badge
- [x] Input field dung border, padding, radius dung chuan
- [x] Card noi dung dung `Content Card` hoac `Shadowed Content Card` dung quy tac
- [x] Navigation co su dong nhat ve border, shadow, typography
- [x] Dashboard admin va man hinh hoc vien cung dung chung he token/component

### 15.4 Kiem tra responsive va tinh nhat quan

- [x] Giao dien desktop hoat dong tot
- [-] Giao dien mobile hoat dong tot
- [x] Typography hierarchy nhat quan giua cac trang
- [x] Khoang cach section va card nhat quan
- [-] Khong co man hinh nao di lech visual language so voi `DESIGN.md`

## 16. Moc uu tien de trien khai

### Phase 1 - MVP can co truoc

- [x] Dang ky / dang nhap / phan quyen Admin-User
- [x] Import de cuong
- [x] Generate course tu de cuong
- [-] Sinh lesson co slide + script + audio + video
- [ ] Admin xem trang thai xu ly
- [x] User xem khoa hoc da publish
- [ ] User xem video bai hoc
- [ ] User danh dau da hoc

### Phase 2 - Hoan thien van hanh

- [ ] Quan ly user day du
- [x] Publish / unpublish course
- [ ] Theo doi tien do hoc tap chi tiet
- [ ] Retry job that bai
- [ ] Toi uu storage va xu ly media
- [ ] Bo sung test coverage cho cac flow chinh

### Phase 3 - Chuan hoa san pham

- [ ] Hoan tat toan bo checklist design compliance
- [ ] Toi uu trai nghiem admin dashboard
- [ ] Toi uu trai nghiem hoc vien
- [ ] Ra soat bao mat, logging, xu ly loi
- [ ] Tai cau truc neu can de giu codebase dung SOLID va de mo rong

## 17. Ghi chu cap nhat tien do

- [ ] Moi khi hoan thanh mot hang muc lon, cap nhat ngay file nay
- [ ] Neu mot muc qua lon, tach thanh checklist con trong pull request hoac tai lieu chi tiet
- [ ] Neu co thay doi pham vi tu `tailieuhethong.md`, cap nhat lai checklist tuong ung
- [ ] Moi man hinh frontend moi deu phai duoc doi chieu voi `DESIGN.md` truoc khi danh dau hoan thanh
