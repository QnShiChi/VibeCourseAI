import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import CourseLearnPage from "./CourseLearnPage";

const mockGetCourseLearnPayload = vi.fn();
const mockGetLessonComments = vi.fn();
const mockUseAuth = vi.fn();

vi.mock("../api/courseService", () => ({
  getCourseLearnPayload: (...args) => mockGetCourseLearnPayload(...args)
}));

vi.mock("../api/commentService", () => ({
  getLessonComments: (...args) => mockGetLessonComments(...args),
  createLessonComment: vi.fn(),
  createLessonReply: vi.fn(),
  addLessonCommentReaction: vi.fn(),
  removeLessonCommentReaction: vi.fn(),
  deleteLessonComment: vi.fn(),
  hideLessonComment: vi.fn(),
  unhideLessonComment: vi.fn()
}));

vi.mock("../auth/AuthContext", () => ({
  useAuth: () => mockUseAuth()
}));

function buildLearnPayload() {
  return {
    courseId: "course-1",
    courseTitle: "OOP",
    courseDescription: "Desc",
    selectedLessonId: "lesson-1",
    selectedLesson: {
      lessonId: "lesson-1",
      lessonTitle: "Lesson 1",
      description: "Mo dau",
      contentSeed: "Noi dung lesson 1",
      videoUrl: "",
      videoGenerationStatus: "NotGenerated",
      videoGenerationError: ""
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
            videoUrl: "",
            videoGenerationStatus: "NotGenerated",
            videoGenerationError: "",
            orderIndex: 1
          }
        ]
      }
    ]
  };
}

describe("CourseLearnPage", () => {
  beforeEach(() => {
    mockUseAuth.mockReturnValue({ user: { role: "User" } });
    mockGetLessonComments.mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 10,
      totalCount: 0,
      hasMore: false,
      sort: "newest"
    });
  });

  it("renders default selected lesson", async () => {
    mockGetCourseLearnPayload.mockResolvedValue(buildLearnPayload());

    render(
      <MemoryRouter initialEntries={["/courses/course-1/learn"]}>
        <Routes>
          <Route path="/courses/:courseId/learn" element={<CourseLearnPage />} />
        </Routes>
      </MemoryRouter>
    );

    expect(await screen.findAllByText("Lesson 1")).not.toHaveLength(0);
    expect(await screen.findByText("Noi dung lesson 1")).toBeInTheDocument();
    expect(await screen.findByRole("heading", { name: "Bình luận" })).toBeInTheDocument();
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
        contentSeed: "Noi dung lesson 1",
        videoUrl: "",
        videoGenerationStatus: "NotGenerated",
        videoGenerationError: ""
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
              videoUrl: "",
              videoGenerationStatus: "NotGenerated",
              videoGenerationError: "",
              orderIndex: 1
            },
            {
              lessonId: "lesson-2",
              lessonTitle: "Lesson 2",
              description: "Tiep theo",
              contentSeed: "Noi dung lesson 2",
              videoUrl: "",
              videoGenerationStatus: "NotGenerated",
              videoGenerationError: "",
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

  it("renders a video player when the selected lesson has a videoUrl", async () => {
    mockGetCourseLearnPayload.mockResolvedValue({
      courseId: "course-1",
      courseTitle: "OOP",
      courseDescription: "Desc",
      selectedLessonId: "lesson-1",
      selectedLesson: {
        lessonId: "lesson-1",
        lessonTitle: "Lesson 1",
        description: "Mo dau",
        contentSeed: "Noi dung lesson 1",
        videoUrl: "/storage/video/lesson-1.mp4",
        videoGenerationStatus: "Completed",
        videoGenerationError: ""
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
              videoUrl: "/storage/video/lesson-1.mp4",
              videoGenerationStatus: "Completed",
              videoGenerationError: "",
              orderIndex: 1
            }
          ]
        }
      ]
    });

    const { container } = render(
      <MemoryRouter initialEntries={["/courses/course-1/learn"]}>
        <Routes>
          <Route path="/courses/:courseId/learn" element={<CourseLearnPage />} />
        </Routes>
      </MemoryRouter>
    );

    await screen.findAllByText("Lesson 1");
    expect(container.querySelector("video")).not.toBeNull();
  });

  it("renders comment content below the selected lesson video", async () => {
    mockGetCourseLearnPayload.mockResolvedValue(buildLearnPayload());
    mockGetLessonComments.mockResolvedValue({
      items: [
        {
          comment: {
            id: "comment-1",
            userId: "user-1",
            authorName: "Alice",
            content: "Video này giải thích khá rõ.",
            isHidden: false,
            isDeleted: false,
            canDelete: false,
            canModerate: false,
            createdAt: "2026-05-21T08:00:00Z",
            reactions: []
          },
          replies: []
        }
      ],
      page: 1,
      pageSize: 10,
      totalCount: 1,
      hasMore: false,
      sort: "newest"
    });

    render(
      <MemoryRouter initialEntries={["/courses/course-1/learn"]}>
        <Routes>
          <Route path="/courses/:courseId/learn" element={<CourseLearnPage />} />
        </Routes>
      </MemoryRouter>
    );

    expect(await screen.findByText("Video này giải thích khá rõ.")).toBeInTheDocument();
  });
});
