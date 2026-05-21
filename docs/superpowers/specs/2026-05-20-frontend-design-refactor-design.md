# Frontend Design Refactor Design

## Context

Frontend hien tai da co auth flow co ban va mot so man hinh nen tang, nhung UI dang lech hoan toan khoi `DESIGN.md`.

Nhung diem lech chinh:
- Dung inline style phan tan tren tung page
- Dung font `Georgia` thay vi `Satoshi`
- Dung dark gradient background lam mac dinh, trai voi light theme cua design system
- Chua co design token trung tam cho color, spacing, radius, shadow
- Chua co component UI dung chung cho input, button, card, section, badge
- `MainLayout` va navigation chua theo visual language trong `DESIGN.md`

Muc tieu cua dot refactor nay la dua toan bo cac man frontend hien co ve cung mot visual system thong nhat, dung token va component rules cua `DESIGN.md`, nhung tiet che bo cuc de phu hop boi canh he thong hoc tap va admin dashboard.

## Scope

In scope:
- `frontend/src/components/layout/MainLayout.jsx`
- `frontend/src/pages/HomePage.jsx`
- `frontend/src/pages/LoginPage.jsx`
- `frontend/src/pages/RegisterPage.jsx`
- `frontend/src/pages/DashboardPage.jsx`
- `frontend/src/pages/CoursesPage.jsx`
- `frontend/src/pages/ProfilePage.jsx`
- `frontend/src/pages/ChangePasswordPage.jsx`
- Cac file global style, theme token, component UI dung chung can them moi

Out of scope:
- Scaffold them man hinh nghiep vu moi chua ton tai
- Thay doi logic auth/back-end
- Chuyen doi toan bo frontend sang Tailwind neu repo chua san sang
- Sua API hoac mo rong route ngoai pham vi UI refactor

## Goals

1. Dong bo toan bo cac man hien co theo `DESIGN.md`
2. Tao mot lop design foundation dung chung de cac man sau nay co the tai su dung
3. Giu auth flow va route hien tai hoat dong binh thuong sau refactor
4. Loai bo visual inconsistency giua public pages va protected pages

## Non-goals

- Dat muc pixel-perfect giong 100% style reference marketing site
- Bo sung man hinh syllabus, lesson, generation jobs khi chua ton tai
- Toi uu hoa animation phuc tap

## Design Direction

Ap dung phuong an: giu nguyen token va component rules cua `DESIGN.md`, nhung tiet che tone de phu hop ngữ cảnh san pham hoc tap va admin.

Huong visual:
- Light theme la mac dinh
- Nen trang `#ffffff`, text den `#000000`, vien `#171717`
- CTA chinh dung `#a3e635`
- Shadow offset nhe, khong dung drop shadow manh
- Typography dung `Satoshi` voi fallback he thong
- Section gap rong, card padding thoang, bo goc dung 4px/8px/100px theo vai tro
- Mau phu saffron/mint/lavender/pink chi dung de tao nhip trong card va dashboard, khong phu kin man hinh

## Architecture

### 1. Global Theme Layer

Them mot file global style cho frontend de khai bao:
- CSS custom properties theo `DESIGN.md`
- Base typography cho body, heading, paragraph
- Reset nhe cho button, input, anchor, page background
- Utility class toi thieu cho page container, section spacing, card variants, button variants

Muc dich la thay inline style bang mot language dung chung, de cac page moi co the tai su dung ma khong phai viet lai tu dau.

### 2. Shared UI Building Blocks

Them cac component UI co pham vi nho, de tai dung tren cac page hien co:
- `Button` ho tro `primary` va `ghost`
- `Card` ho tro `default`, `shadowed`, `highlight`
- `InputField` hoac cac class input dung chung
- `PageHeader`
- `Section`
- `Badge`

Khong can over-engineer thanh design system hoan chinh; chi can du de loai bo style inline va tao consistency.

### 3. Application Shell

`MainLayout` se duoc refactor thanh app shell trung tam:
- Sticky top navigation nen trang, vien duoi den, max-width contained
- Logo/brand ben trai, primary CTA va navigation ben phai
- Trang thai auth hien thi ro rang bang nhom action co style thong nhat
- Main content dat trong page container chung
- Background toan app la light, co the xen mot so decorative band hoac panel rat nhe

### 4. Page-level Composition

Moi page se dung cung mot bo quy tac:
- Co page header ro rang
- Co container width nhat quan
- Form nam trong card
- Danh sach/noi dung demo nam trong card hoac grid card
- Empty state va skeleton state van dung visual language chung

## Screen-by-screen Design

### HomePage

- Hero section voi nen gradient nhe theo `Sky Breeze`
- Headline lon va 2 CTA chinh/phu
- Cac card gioi thieu nhanh cho 3 nhom gia tri: hoc tap, khoa hoc, quan tri
- Khong lam qua marketing; van giu chat product landing page ngan gon

### LoginPage

- Form nam giua trong card co border va shadow offset nhe
- Tieu de ro rang, body text ngan
- Input dung style chuan
- Nut submit dung primary button
- Link sang register dung ghost/text treatment

### RegisterPage

- Cau truc giong LoginPage de tao consistency
- Them nhan manh ve viec tao tai khoan de truy cap khoa hoc

### DashboardPage

- Tu skeleton text thanh dashboard thuc su
- Co khu header + nhom stat card placeholder
- Co khu quick actions de tro duong cho cac tinh nang admin sap co
- Dung card mau phu co tiet che

### CoursesPage

- Tu doan text thanh page danh sach khoa hoc co header va card list/grid
- Khi chua noi API day du, hien demo card/empty state theo giao dien that

### ProfilePage

- Hien thong tin user trong info card thay vi cac dong text roi rac
- Tach thong tin tai khoan va thao tac lien quan neu can

### ChangePasswordPage

- Form card dong nhat voi login/register
- Feedback success/error hien thi theo block thong bao ro rang

### MainLayout

- Refactor navigation va footer/page shell neu can
- Loai bo dark gradient va `Georgia`
- Dua tat ca page vao he thong spacing va typography thong nhat

## Data and Behavior Considerations

- Khong thay doi API contract hoac auth flow
- `RequireAuth`, `AuthContext`, `authService` tiep tuc giu nguyen logic
- Neu `CoursesPage` chua fetch du lieu that, UI moi van phai chiu duoc trang thai placeholder ma khong gay hieu nham la da hoan tat nghiep vu

## Technical Approach

1. Them global stylesheet va import tai entry point
2. Them shared UI component toi thieu hoac class dung chung
3. Refactor `MainLayout` truoc de tao shell chung
4. Refactor nhom auth pages (`Login`, `Register`, `ChangePassword`)
5. Refactor nhom content pages (`Home`, `Dashboard`, `Courses`, `Profile`)
6. Chay build/test frontend de xac nhan khong vo route va auth shell

## Risks

1. Repo hien tai chua dung `Tailwind CSS`
   - Giai phap: ap token bang CSS thuong o dot nay, khong co gang nhua Tailwind vao cung luc

2. Inline style dang dan logic view rat sat component
   - Giai phap: refactor dan sang class/component nho, tranh rewrite qua tay mot luc

3. Design reference mang tinh brand/marketing nhieu hon app dashboard
   - Giai phap: giu token va component rules, tiet che decorative treatment trong man admin

4. Mot so page dang la skeleton text
   - Giai phap: nang cap thanh UI that nhung van trung thuc voi tinh trang du lieu placeholder

## Testing Strategy

- Chay `npm run build` cho frontend
- Chay test frontend hien co
- Kiem tra route cong khai va route can auth khong bi vo layout
- Kiem tra visual o cac man co trong repo tren desktop width truoc
- Neu co the, kiem tra them responsive co ban cho mobile width

## Success Criteria

Dot refactor duoc xem la dat neu:
- Toan bo page hien co khong con dung visual language cu lech `DESIGN.md`
- Font, mau, spacing, border, shadow, button, input, card da dong bo
- `MainLayout` tro thanh shell chung nhat quan cho app
- Login, register, profile, change password, courses, dashboard, home deu nhin cung mot he san pham
- Build frontend va test frontend hien co van chay duoc

## Implementation Boundary

Dot nay chi refactor UI cua cac man hien co. Sau khi xong moi tiep tuc ap cung he design cho cac man nghiep vu sap xay nhu syllabus import, generation jobs, course detail, lesson detail.
