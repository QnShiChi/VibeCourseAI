# Admin Payment Management Implementation Plan

**Goal:** Thêm màn `Quản lý hóa đơn` riêng trong admin sidebar với trang danh sách và trang chi tiết hóa đơn dùng dữ liệu thật từ backend.

**Architecture:** Mở rộng backend bằng 2 admin payment endpoints chuyên dụng để frontend không phụ thuộc payload dashboard. Frontend thêm nav item, route list/detail, API client riêng, và 2 page admin mới theo pattern admin hiện có.

**Tech Stack:** ASP.NET Core Web API (.NET 8), Entity Framework Core, React 18, React Router, Axios, Vitest, Testing Library.

---

### Task 1: Add backend admin payment DTOs

**Files:**
- Create: `backend/CourseVideo.API/DTOs/Payments/AdminPaymentOrderListItemResponse.cs`
- Create: `backend/CourseVideo.API/DTOs/Payments/AdminPaymentOrderDetailResponse.cs`

**Step 1: Write the DTOs**

Create list DTO with:

```csharp
namespace CourseVideo.API.DTOs.Payments;

public class AdminPaymentOrderListItemResponse
{
    public Guid PaymentOrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
}
```

Create detail DTO with all list fields plus:

```csharp
public string BankCode { get; set; } = string.Empty;
public string BankName { get; set; } = string.Empty;
public string BankAccountNumber { get; set; } = string.Empty;
public string AccountHolderName { get; set; } = string.Empty;
public string TransferContent { get; set; } = string.Empty;
public int? SepayTransactionId { get; set; }
```

**Step 2: Build backend project**

Run: `dotnet build backend/CourseVideo.API/CourseVideo.API.csproj`

Expected: build succeeds with new DTOs.

### Task 2: Add backend admin payment endpoints

**Files:**
- Create: `backend/CourseVideo.API/Controllers/AdminPaymentOrdersController.cs`
- Test: `backend/CourseVideo.API.Tests/Controllers/AdminPaymentOrdersControllerTests.cs`

**Step 1: Write failing controller tests**

Add tests for:

- list returns all statuses including `Pending`
- list filters by `status`
- list filters by `query` against order code and user name/email
- detail returns full order information
- detail returns `NotFound` for missing order

Use in-memory `AppDbContext` pattern already used by controller tests.

**Step 2: Run tests to verify failure**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter AdminPaymentOrdersControllerTests`

Expected: fail because controller does not exist yet.

**Step 3: Write minimal controller**

Implement:

```csharp
[ApiController]
[Route("api/admin/payment-orders")]
[Authorize(Roles = "Admin")]
public class AdminPaymentOrdersController : ControllerBase
```

List action:

- query params: `query`, `status`
- load `PaymentOrders` with `User` and `Course`
- sort by `PaidAt ?? CreatedAt` desc
- filter in-memory for v1
- map to `AdminPaymentOrderListItemResponse`

Detail action:

- load by `id`
- include `User` and `Course`
- map to `AdminPaymentOrderDetailResponse`

**Step 4: Run tests to verify pass**

Run: `dotnet test backend/CourseVideo.API.Tests/CourseVideo.API.Tests.csproj --filter AdminPaymentOrdersControllerTests`

Expected: pass in an environment with .NET 8 testhost available.

### Task 3: Add frontend payment admin API client

**Files:**
- Modify: `frontend/src/api/paymentService.js`
- Test: `frontend/src/pages/AdminPaymentsPage.test.jsx`

**Step 1: Add API functions**

Add:

```javascript
export async function getAdminPaymentOrders(params = {}) {
  const { data } = await axiosClient.get("/admin/payment-orders", { params });
  return data;
}

export async function getAdminPaymentOrderDetail(paymentOrderId) {
  const { data } = await axiosClient.get(`/admin/payment-orders/${paymentOrderId}`);
  return data;
}
```

**Step 2: Verify usage compiles later with page tests**

No separate command for this step; covered by frontend tests in later tasks.

### Task 4: Add admin payment list page

**Files:**
- Create: `frontend/src/pages/AdminPaymentsPage.jsx`
- Create: `frontend/src/pages/AdminPaymentsPage.test.jsx`
- Modify: `frontend/src/styles/theme.css`

**Step 1: Write failing frontend test**

Test that page:

- renders heading `Quản lý hóa đơn`
- loads and shows rows for `Pending`, `Paid`, `Expired`
- filters by search text
- filters by status
- links to `/admin/payments/:id`

Mock `getAdminPaymentOrders`.

**Step 2: Run test to verify failure**

Run: `npm run test -- --run src/pages/AdminPaymentsPage.test.jsx`

Expected: fail because page does not exist yet.

**Step 3: Implement page**

Build a page with:

- hero header
- search input
- status select with `All`, `Pending`, `Paid`, `LatePaid`, `Expired`, `Failed`
- refresh button
- list/table card rendering payment rows
- status badges reusing admin badge tones
- `Link` to detail page

Keep v1 simple:

- fetch all on mount
- local filtering by `query` and `status`

**Step 4: Add supporting styles**

Add table/list styles under admin section in `theme.css` for:

- toolbar row
- payment table/list card
- payment meta lines
- responsive stacked rows

**Step 5: Run test to verify pass**

Run: `npm run test -- --run src/pages/AdminPaymentsPage.test.jsx`

Expected: pass.

### Task 5: Add admin payment detail page

**Files:**
- Create: `frontend/src/pages/AdminPaymentDetailPage.jsx`
- Create: `frontend/src/pages/AdminPaymentDetailPage.test.jsx`
- Modify: `frontend/src/styles/theme.css`

**Step 1: Write failing detail page test**

Test that page:

- renders order code
- renders status
- renders user/course/payment info
- formats times in `Asia/Ho_Chi_Minh`
- shows error state when API rejects

Mock `getAdminPaymentOrderDetail`.

**Step 2: Run test to verify failure**

Run: `npm run test -- --run src/pages/AdminPaymentDetailPage.test.jsx`

Expected: fail because detail page does not exist yet.

**Step 3: Implement detail page**

Use route param `id` and fetch detail by ID.

Render sections:

- summary card
- buyer/course info
- payment info
- timeline info
- quick actions with back link and copy buttons

Time formatting must use:

```javascript
new Intl.DateTimeFormat("vi-VN", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
  hour12: false,
  timeZone: "Asia/Ho_Chi_Minh"
})
```

**Step 4: Add detail page styles**

Add detail grid/card styles to `theme.css`.

**Step 5: Run test to verify pass**

Run: `npm run test -- --run src/pages/AdminPaymentDetailPage.test.jsx`

Expected: pass.

### Task 6: Wire routes and sidebar navigation

**Files:**
- Modify: `frontend/src/routes/AppRoutes.jsx`
- Modify: `frontend/src/components/layout/AdminLayout.jsx`

**Step 1: Add routes**

Add imports and routes:

```javascript
<Route path="/admin/payments" element={<AdminPaymentsPage />} />
<Route path="/admin/payments/:id" element={<AdminPaymentDetailPage />} />
```

**Step 2: Add sidebar nav item**

Add:

```javascript
<AdminNavItem icon="◨" label="Quản lý hóa đơn" to="/admin/payments" />
```

Place it near `Báo cáo hệ thống`.

**Step 3: Run targeted route/nav tests if needed**

Run:

```bash
npm run test -- --run src/pages/AdminPaymentsPage.test.jsx src/pages/AdminPaymentDetailPage.test.jsx src/components/layout/AdminLayout.test.jsx
```

Expected: pass.

### Task 7: Final verification

**Files:**
- Verify only

**Step 1: Run frontend tests**

Run:

```bash
npm run test -- --run src/pages/AdminPaymentsPage.test.jsx src/pages/AdminPaymentDetailPage.test.jsx src/pages/DashboardPage.test.jsx src/styles/theme.test.jsx
```

Expected: pass.

**Step 2: Build frontend**

Run:

```bash
npm run build
```

Expected: build succeeds.

**Step 3: Build backend**

Run:

```bash
dotnet build backend/CourseVideo.API/CourseVideo.API.csproj
```

Expected: build succeeds.

**Step 4: Rebuild runtime containers**

Run:

```bash
docker compose up -d --build frontend backend
```

Expected: admin UI serves new sidebar item and routes.

