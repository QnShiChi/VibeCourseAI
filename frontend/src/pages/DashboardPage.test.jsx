import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import DashboardPage from "./DashboardPage";

const mockGetDashboardStats = vi.fn();
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
  getDashboardStats: (...args) => mockGetDashboardStats(...args)
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
    expect(screen.getByText("Hiện có 1 bình luận tiêu cực chưa xử lý cần admin xem xét.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Xem chi tiết" })).toHaveAttribute("href", "/admin/comment-moderation");
  });
});
