import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import ProfilePage from "./ProfilePage";

const mockGetPublishedCourses = vi.fn();
const mockGetAdminCourses = vi.fn();
const mockGetPurchaseHistory = vi.fn();
const mockUseAuth = vi.fn();

vi.mock("../api/courseService", () => ({
  getPublishedCourses: (...args) => mockGetPublishedCourses(...args),
  getAdminCourses: (...args) => mockGetAdminCourses(...args)
}));

vi.mock("../api/paymentService", () => ({
  getPurchaseHistory: (...args) => mockGetPurchaseHistory(...args)
}));

vi.mock("../auth/useAuth", () => ({
  useAuth: () => mockUseAuth()
}));

vi.mock("../utils/learningProgress", () => ({
  readCurrentLearningProgress: () => null
}));

vi.mock("../utils/webActivity", () => ({
  formatActivityDuration: () => "0h",
  readWebActivitySeries: () => []
}));

function renderProfilePage() {
  return render(
    <MemoryRouter>
      <ProfilePage />
    </MemoryRouter>
  );
}

describe("ProfilePage", () => {
  it("prioritizes owned courses by grantedAt for the featured learner section", async () => {
    mockUseAuth.mockReturnValue({
      isAuthenticated: true,
      user: {
        role: "User",
        fullName: "Phuong Nguyen",
        email: "phuong@example.com"
      }
    });

    mockGetPublishedCourses.mockResolvedValue([
      {
        id: "course-old-owned",
        title: "Lập trình hướng đối tượng",
        category: "Kiến thức nền tảng",
        thumbnailUrl: "/oop.png",
        moduleCount: 10,
        lessonCount: 49,
        createdAt: "2026-05-10T09:00:00Z",
        grantedAt: "2026-06-14T09:00:00Z",
        alreadyOwned: true
      },
      {
        id: "course-new-owned",
        title: "Trí Tuệ Nhân Tạo Ứng Dụng",
        category: "Kiến thức nền tảng",
        thumbnailUrl: "/ai.png",
        moduleCount: 6,
        lessonCount: 21,
        createdAt: "2026-01-10T09:00:00Z",
        grantedAt: "2026-06-16T09:00:00Z",
        alreadyOwned: true
      },
      {
        id: "course-not-owned",
        title: "Prompt Engineering",
        category: "AI & Data",
        thumbnailUrl: "/prompt.png",
        moduleCount: 8,
        lessonCount: 16,
        createdAt: "2026-06-20T09:00:00Z",
        grantedAt: null,
        alreadyOwned: false
      }
    ]);
    mockGetPurchaseHistory.mockResolvedValue([
      {
        paymentOrderId: "order-new",
        orderCode: "VCNEW",
        courseId: "course-new-owned",
        courseTitle: "Trí Tuệ Nhân Tạo Ứng Dụng",
        courseThumbnailUrl: "/ai.png",
        amount: 699000,
        status: "Paid",
        purchasedAt: "2026-06-16T09:00:00Z",
        paidAt: "2026-06-16T09:00:00Z"
      },
      {
        paymentOrderId: "order-old",
        orderCode: "VCOLD",
        courseId: "course-old-owned",
        courseTitle: "Lập trình hướng đối tượng",
        courseThumbnailUrl: "/oop.png",
        amount: 599000,
        status: "LatePaid",
        purchasedAt: "2026-06-14T09:00:00Z",
        paidAt: "2026-06-14T09:15:00Z"
      }
    ]);

    renderProfilePage();

    expect(await screen.findByText("Khóa học của bạn")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Tiếp tục học" })).toBeInTheDocument();
    expect(await screen.findByRole("heading", { name: "Lịch sử mua hàng" })).toBeInTheDocument();
    expect(screen.getByText("Khóa học khả dụng")).toBeInTheDocument();
    expect(screen.getByText("Số khóa học bạn đã mua thành công")).toBeInTheDocument();
    expect(screen.getByText("Bạn hiện sở hữu 2 khóa học đã mua.")).toBeInTheDocument();

    const featureTitles = screen.getAllByRole("heading", { level: 4 }).map((item) => item.textContent);
    expect(featureTitles.slice(0, 2)).toEqual([
      "Trí Tuệ Nhân Tạo Ứng Dụng",
      "Lập trình hướng đối tượng"
    ]);
    expect(featureTitles).not.toContain("Prompt Engineering");
    expect(screen.getByText("Mua ngày 16/6/2026")).toBeInTheDocument();
    expect(screen.getByText("Mua ngày 14/6/2026")).toBeInTheDocument();
    expect(screen.getByText("Đơn hàng VCNEW")).toBeInTheDocument();
    expect(screen.getByText("Đã thanh toán")).toBeInTheDocument();
    expect(screen.getByText("Thanh toán muộn")).toBeInTheDocument();
    expect(screen.getByText("Số tiền 699.000 ₫")).toBeInTheDocument();
  });
});
