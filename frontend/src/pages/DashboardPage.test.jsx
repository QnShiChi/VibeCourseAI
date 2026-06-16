import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import DashboardPage from "./DashboardPage";

const mockGetDashboardStats = vi.fn();
const mockGetDashboardPaymentOverview = vi.fn();
const mockGetAdminCourses = vi.fn();
const mockGetUsers = vi.fn();

vi.mock("../auth/useAuth", () => ({
  useAuth: () => ({
    user: {
      fullName: "Admin User",
      email: "admin@vibecourse.local",
      role: "Admin"
    }
  })
}));

vi.mock("../api/dashboardService", () => ({
  getDashboardStats: (...args) => mockGetDashboardStats(...args),
  getDashboardPaymentOverview: (...args) => mockGetDashboardPaymentOverview(...args)
}));

vi.mock("../api/courseService", () => ({
  getAdminCourses: (...args) => mockGetAdminCourses(...args)
}));

vi.mock("../api/userService", () => ({
  getUsers: (...args) => mockGetUsers(...args)
}));

function renderDashboard() {
  render(
    <MemoryRouter>
      <DashboardPage />
    </MemoryRouter>
  );
}

describe("DashboardPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetAdminCourses.mockResolvedValue([]);
    mockGetUsers.mockResolvedValue([]);
    mockGetDashboardPaymentOverview.mockResolvedValue({
      totalOrders: 3,
      paidOrders: 2,
      pendingOrders: 0,
      failedOrExpiredOrders: 2,
      timeline: [
        { label: "10/06", paidOrders: 1, pendingOrders: 0, failedOrExpiredOrders: 0 },
        { label: "11/06", paidOrders: 2, pendingOrders: 0, failedOrExpiredOrders: 0 },
        { label: "12/06", paidOrders: 1, pendingOrders: 0, failedOrExpiredOrders: 2 },
        { label: "13/06", paidOrders: 2, pendingOrders: 0, failedOrExpiredOrders: 2 },
        { label: "14/06", paidOrders: 1, pendingOrders: 0, failedOrExpiredOrders: 0 },
        { label: "15/06", paidOrders: 2, pendingOrders: 0, failedOrExpiredOrders: 0 },
        { label: "16/06", paidOrders: 2, pendingOrders: 0, failedOrExpiredOrders: 1 }
      ],
      recentOrders: [
        {
          paymentOrderId: "order-1",
          orderCode: "VCPAID001",
          userFullName: "Nguyen Van A",
          userEmail: "a@example.com",
          courseTitle: "Lập trình hướng đối tượng",
          amount: 3000,
          status: "Paid",
          createdAt: "2026-06-16T08:00:00Z",
          paidAt: "2026-06-16T08:05:00Z"
        },
        {
          paymentOrderId: "order-2",
          orderCode: "VCCANCEL001",
          userFullName: "Nguyen Van B",
          userEmail: "b@example.com",
          courseTitle: "Trí Tuệ Nhân Tạo Ứng Dụng",
          amount: 5000,
          status: "Cancelled",
          createdAt: "2026-06-16T09:00:00Z",
          paidAt: null
        }
      ]
    });
  });

  it("renders moderation summary card on admin dashboard", async () => {
    mockGetDashboardStats.mockResolvedValue({
      usersCount: 4,
      syllabusesCount: 2,
      coursesCount: 1,
      generationJobsCount: 3,
      negativeCommentsCount: 1
    });

    renderDashboard();

    expect(await screen.findByRole("heading", { name: "Cảnh báo bình luận tiêu cực" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Tổng quan hóa đơn" })).toBeInTheDocument();
    expect(screen.getByText("Theo dõi giao dịch")).toBeInTheDocument();
    expect(screen.getByText("Nguyen Van A")).toBeInTheDocument();
    expect(screen.getAllByText("Đã thanh toán")).toHaveLength(2);
    expect(screen.getByText("Đã hủy thanh toán")).toBeInTheDocument();
    expect(screen.getByText("Hết hạn / hủy / lỗi")).toBeInTheDocument();
    expect(screen.getByText("Nguyen Van B")).toBeInTheDocument();
    expect(screen.getByText(/VCPAID001 • 3\.000 ₫ • 15:05 16\/06\/2026/i)).toBeInTheDocument();
    expect(screen.getByText("Hiện có 1 bình luận tiêu cực chưa xử lý cần admin xem xét.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Xem chi tiết" })).toHaveAttribute("href", "/admin/comment-moderation");
  });
});
