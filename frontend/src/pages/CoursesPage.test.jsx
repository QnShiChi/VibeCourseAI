import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import CoursesPage from "./CoursesPage";

const mockGetAdminCourses = vi.fn();
const mockGetPublishedCourses = vi.fn();
const mockPublishCourse = vi.fn();
const mockUnpublishCourse = vi.fn();

vi.mock("../api/courseService", () => ({
  getAdminCourses: (...args) => mockGetAdminCourses(...args),
  getPublishedCourses: (...args) => mockGetPublishedCourses(...args),
  publishCourse: (...args) => mockPublishCourse(...args),
  unpublishCourse: (...args) => mockUnpublishCourse(...args)
}));

const mockUseAuth = vi.fn();

vi.mock("../auth/useAuth", () => ({
  useAuth: () => mockUseAuth()
}));

function renderCoursesPage() {
  render(
    <MemoryRouter>
      <CoursesPage />
    </MemoryRouter>
  );
}

describe("CoursesPage", () => {
  it("filters courses by search term and category", async () => {
    mockUseAuth.mockReturnValue({ user: { role: "User" } });
    mockGetPublishedCourses.mockResolvedValue([
      {
        id: "course-1",
        title: "Advanced UI Systems",
        description: "Design systems for product teams",
        category: "UiUxDesign",
        thumbnailUrl: "/storage/course-thumbnails/ui.png",
        isPublished: true,
        moduleCount: 12,
        lessonCount: 24
      },
      {
        id: "course-2",
        title: "Prompt Engineering",
        description: "AI workflows",
        category: "AiAndData",
        thumbnailUrl: "/storage/course-thumbnails/ai.png",
        isPublished: true,
        moduleCount: 8,
        lessonCount: 10
      }
    ]);

    renderCoursesPage();

    fireEvent.click(await screen.findByRole("button", { name: "AI & Data" }));
    fireEvent.change(screen.getByLabelText("Tìm khóa học"), { target: { value: "prompt" } });

    expect(await screen.findByText("Prompt Engineering")).toBeInTheDocument();
    expect(screen.queryByText("Advanced UI Systems")).not.toBeInTheDocument();
  });

  it("renders courses as compact vertical cards", async () => {
    mockUseAuth.mockReturnValue({ user: { role: "User" } });
    mockGetPublishedCourses.mockResolvedValue([
      {
        id: "course-1",
        title: "Advanced UI Systems",
        description: "Desc",
        category: "UiUxDesign",
        thumbnailUrl: "/storage/course-thumbnails/ui.png",
        isPublished: true,
        moduleCount: 12,
        lessonCount: 24
      },
      {
        id: "course-2",
        title: "Prompt Engineering",
        description: "Desc",
        category: "AiAndData",
        thumbnailUrl: "/storage/course-thumbnails/ai.png",
        isPublished: true,
        moduleCount: 8,
        lessonCount: 10
      }
    ]);

    renderCoursesPage();

    const grid = await screen.findByTestId("course-grid");
    const cards = await screen.findAllByTestId("course-card");

    expect(grid).toBeInTheDocument();
    expect(cards).toHaveLength(2);
    expect(cards[0]).toHaveAttribute("data-layout", "compact");
    expect(cards[1]).toHaveAttribute("data-layout", "compact");
    expect(within(cards[0]).getByText("UI/UX Design")).toBeInTheDocument();
    expect(within(cards[0]).getByText("Khóa học")).toBeInTheDocument();
    expect(within(cards[0]).getByRole("link", { name: "Xem khóa học" })).toBeInTheDocument();
    expect(within(cards[0]).getByRole("img", { name: "Advanced UI Systems" })).toBeInTheDocument();
    expect(within(cards[1]).getByRole("img", { name: "Prompt Engineering" })).toBeInTheDocument();
  });

  it("renders admin publish action inside course cards", async () => {
    mockUseAuth.mockReturnValue({ user: { role: "Admin" } });
    mockGetAdminCourses.mockResolvedValue([
      {
        id: "course-1",
        title: "Draft OOP",
        description: "Desc",
        category: "Development",
        thumbnailUrl: "/storage/course-thumbnails/dev.png",
        isPublished: false,
        moduleCount: 1,
        lessonCount: 2
      }
    ]);

    renderCoursesPage();

    expect(await screen.findByText("Draft OOP")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Publish" }));

    await waitFor(() => expect(mockPublishCourse).toHaveBeenCalledWith("course-1"));
  });
});
