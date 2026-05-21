import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import CourseLearnPage from "./CourseLearnPage";

const mockGetCourseLearnPayload = vi.fn();

vi.mock("../api/courseService", () => ({
  getCourseLearnPayload: (...args) => mockGetCourseLearnPayload(...args)
}));

describe("CourseLearnPage", () => {
  it("renders default selected lesson", async () => {
    mockGetCourseLearnPayload.mockResolvedValue({
      courseId: "course-1",
      courseTitle: "OOP",
      courseDescription: "Desc",
      selectedLessonId: "lesson-1",
      selectedLesson: {
        lessonId: "lesson-1",
        lessonTitle: "Lesson 1",
        description: "Mo dau",
        contentSeed: "Noi dung lesson 1"
      },
      modules: [
        {
          moduleId: "module-1",
          moduleTitle: "Module 1",
          moduleDescription: "M1",
          orderIndex: 1,
          lessons: [
            {
              lessonId: "lesson-1",
              lessonTitle: "Lesson 1",
              description: "Mo dau",
              contentSeed: "Noi dung lesson 1",
              orderIndex: 1
            }
          ]
        }
      ]
    });

    render(
      <MemoryRouter initialEntries={["/courses/course-1/learn"]}>
        <Routes>
          <Route path="/courses/:courseId/learn" element={<CourseLearnPage />} />
        </Routes>
      </MemoryRouter>
    );

    expect(await screen.findAllByText("Lesson 1")).not.toHaveLength(0);
    expect(await screen.findByText("Noi dung lesson 1")).toBeInTheDocument();
  });

  it("changes left panel when selecting another lesson", async () => {
    mockGetCourseLearnPayload.mockResolvedValue({
      courseId: "course-1",
      courseTitle: "OOP",
      courseDescription: "Desc",
      selectedLessonId: "lesson-1",
      selectedLesson: {
        lessonId: "lesson-1",
        lessonTitle: "Lesson 1",
        description: "Mo dau",
        contentSeed: "Noi dung lesson 1"
      },
      modules: [
        {
          moduleId: "module-1",
          moduleTitle: "Module 1",
          moduleDescription: "M1",
          orderIndex: 1,
          lessons: [
            {
              lessonId: "lesson-1",
              lessonTitle: "Lesson 1",
              description: "Mo dau",
              contentSeed: "Noi dung lesson 1",
              orderIndex: 1
            },
            {
              lessonId: "lesson-2",
              lessonTitle: "Lesson 2",
              description: "Tiep theo",
              contentSeed: "Noi dung lesson 2",
              orderIndex: 2
            }
          ]
        }
      ]
    });

    render(
      <MemoryRouter initialEntries={["/courses/course-1/learn"]}>
        <Routes>
          <Route path="/courses/:courseId/learn" element={<CourseLearnPage />} />
        </Routes>
      </MemoryRouter>
    );

    fireEvent.click(await screen.findByRole("button", { name: /2. Lesson 2/i }));

    expect(await screen.findByText("Noi dung lesson 2")).toBeInTheDocument();
  });
});
