# Admin Dashboard Invoice Management Design

## Muc tieu

Bo sung ngay trong `Dashboard` cua admin mot khoi quan ly hoa don de nhin nhanh tinh hinh thanh toan cua user ma khong can mo them trang rieng.

## Pham vi giai doan nay

- Chi lam trong `frontend/src/pages/DashboardPage.jsx`
- Khong mo rong `AdminFinancePage` o buoc nay
- Khong them filter, search, phan trang, export
- Khong them thao tac sua/xoa hoa don

## Trai nghiem mong muon

Dashboard se co them mot cum moi nam duoi hang metric hien tai:

1. `Tong quan hoa don`
   - Tong hoa don
   - Da thanh toan
   - Cho thanh toan
   - Het han / loi

2. `Hoa don gan day`
   - Hien 5-10 dong gan nhat
   - Cac cot:
     - Ma don
     - User
     - Khoa hoc
     - So tien
     - Trang thai
     - Thoi gian

## Nguon du lieu

Them mot API admin rieng, tach khoi `getDashboardStats()` hien tai de tranh lam dashboard tong bi rang buoc vao payment:

- De xuat endpoint: `GET /api/admin/payment-orders/overview`

Response gom:

- `totalOrders`
- `paidOrders`
- `pendingOrders`
- `failedOrExpiredOrders`
- `recentOrders`

Moi `recentOrder` can co:

- `paymentOrderId`
- `orderCode`
- `userId`
- `userFullName`
- `userEmail`
- `courseId`
- `courseTitle`
- `amount`
- `status`
- `createdAt`
- `paidAt`

## Logic backend

Nguon du lieu la `PaymentOrders`, join voi `Users` va `Courses`.

Quy uoc trang thai:

- Thanh cong: `Paid`, `LatePaid`
- Dang cho: `Pending`
- Loi/het han: `Expired`, `Failed`

Danh sach hoa don gan day sap xep:

1. `PaidAt` giam dan neu co
2. neu khong co `PaidAt` thi dung `CreatedAt` giam dan

## Logic frontend

`DashboardPage` goi them mot request payment overview song song voi:

- `getDashboardStats()`
- `getAdminCourses()`
- `getUsers()`

Neu request payment loi:

- Chi hong rieng khoi hoa don
- Dashboard tong van render duoc
- Hien `ui-alert--error` cuc bo trong khu payment

## Hien thi UI

### Card 1: Tong quan hoa don

Dung cung ngon ngu thiet ke dark glassmorphism hien co.

4 o so lieu nho:

- Tong hoa don
- Da thanh toan
- Cho thanh toan
- Het han / loi

### Card 2: Hoa don gan day

Bang gon, read-only, moi dong co badge trang thai:

- Xanh: `Paid`, `LatePaid`
- Neutral/vang: `Pending`
- Do: `Expired`, `Failed`

Cot thoi gian:

- Uu tien hien `PaidAt` neu da thanh toan
- Neu chua thanh toan thi hien `CreatedAt`

## Empty state

Neu chua co hoa don:

- `Chua co hoa don nao duoc ghi nhan.`

## Testing

### Backend

- Test aggregation theo trang thai
- Test mapping recent orders
- Test sort theo `PaidAt` / `CreatedAt`

### Frontend

- Test `DashboardPage` render metric payment dung
- Test render recent orders dung du lieu
- Test badge trang thai dung mau/label
- Test payment block loi khong lam sap dashboard tong

## Ghi chu implementation

- Khong nhan them logic payment vao `DashboardStatsResponse` hien tai
- Tach API payment overview rieng de de bao tri va de fallback khi loi
- Khi xong implementation, can restart backend container de `localhost` dung runtime moi
