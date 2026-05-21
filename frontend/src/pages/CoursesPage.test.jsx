import { fireEvent, render, screen, waitFor } from "@testing-library/react";
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

describe("CoursesPage", () => {
  it("renders published courses for normal user", async () => {
    mockUseAuth.mockReturnValue({ user: { role: "User" } });
    mockGetPublishedCourses.mockResolvedValue([
      { id: "course-1", title: "OOP", description: "Desc", isPublished: true, moduleCount: 2, lessonCount: 6 }
    ]);

    render(
      <MemoryRouter>
        <CoursesPage />
      </MemoryRouter>
    );

    expect(await screen.findByText("OOP")).toBeInTheDocument();
    expect(screen.queryByText("Draft")).not.toBeInTheDocument();
  });

  it("renders admin statuses and publish action", async () => {
    mockUseAuth.mockReturnValue({ user: { role: "Admin" } });
    mockGetAdminCourses.mockResolvedValue([
      { id: "course-1", title: "Draft OOP", description: "Desc", isPublished: false, moduleCount: 1, lessonCount: 2 }
    ]);

    render(
      <MemoryRouter>
        <CoursesPage />
      </MemoryRouter>
    );

    expect(await screen.findByText("Draft")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Publish" }));

    await waitFor(() => expect(mockPublishCourse).toHaveBeenCalledWith("course-1"));
  });
});
