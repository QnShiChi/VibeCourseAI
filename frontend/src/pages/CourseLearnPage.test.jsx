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
    courseTitle: "TRÍ TUỆ NHÂN TẠO ỨNG DỤNG",
    courseDescription: "Desc",
    selectedLessonId: "lesson-1",
    selectedLesson: {
      lessonId: "lesson-1",
      lessonTitle: "Tổng quan về AI",
      description: "Mo dau",
      contentSeed: "Noi dung lesson 1",
      videoUrl: "",
      videoGenerationStatus: "NotGenerated",
      videoGenerationError: "",
      orderIndex: 1
    },
    modules: [
      {
        moduleId: "module-1",
        moduleTitle: "Định nghĩa và Lịch sử",
        moduleDescription: "M1",
        orderIndex: 2,
        lessons: [
          {
            lessonId: "lesson-1",
            lessonTitle: "Tổng quan về AI",
            description: "Mo dau",
            contentSeed: "Noi dung lesson 1",
            videoUrl: "",
            videoGenerationStatus: "NotGenerated",
            videoGenerationError: "",
            orderIndex: 1
          },
          {
            lessonId: "lesson-2",
            lessonTitle: "Các mốc lịch sử",
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

    expect(await screen.findAllByText("Tổng quan về AI")).not.toHaveLength(0);
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

    fireEvent.click(await screen.findByRole("button", { name: /Lesson 2/i }));

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

  it("shows progress and moves to the next lesson from the footer navigation", async () => {
    mockGetCourseLearnPayload.mockResolvedValue(buildLearnPayload());

    render(
      <MemoryRouter initialEntries={["/courses/course-1/learn"]}>
        <Routes>
          <Route path="/courses/:courseId/learn" element={<CourseLearnPage />} />
        </Routes>
      </MemoryRouter>
    );

    expect(await screen.findByText(/Tiến độ: 50%/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Bài trước/i })).toBeDisabled();

    fireEvent.click(screen.getByRole("button", { name: /Tiếp tục bài học/i }));

    expect(await screen.findByText("Noi dung lesson 2")).toBeInTheDocument();
    expect(await screen.findByText(/Tiến độ: 100%/i)).toBeInTheDocument();
  });

  it("moves back to the previous lesson and renders the course content heading", async () => {
    const payload = buildLearnPayload();
    payload.selectedLessonId = "lesson-2";
    payload.selectedLesson = payload.modules[0].lessons[1];
    mockGetCourseLearnPayload.mockResolvedValue(payload);

    render(
      <MemoryRouter initialEntries={["/courses/course-1/learn"]}>
        <Routes>
          <Route path="/courses/:courseId/learn" element={<CourseLearnPage />} />
        </Routes>
      </MemoryRouter>
    );

    expect(await screen.findByRole("heading", { name: /Nội dung khóa học/i })).toBeInTheDocument();
    expect(await screen.findByText(/Tiến độ: 100%/i)).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: /Bài trước/i }));

    expect(await screen.findByText("Noi dung lesson 1")).toBeInTheDocument();
    expect(await screen.findByText(/Tiến độ: 50%/i)).toBeInTheDocument();
  });

  it("keeps only one module expanded when opening another module", async () => {
    const payload = buildLearnPayload();
    payload.modules = [
      payload.modules[0],
      {
        moduleId: "module-2",
        moduleTitle: "Du lieu va hoc may",
        moduleDescription: "M2",
        orderIndex: 3,
        lessons: [
          {
            lessonId: "lesson-3",
            lessonTitle: "Du lieu lon",
            description: "Mo rong",
            contentSeed: "Noi dung lesson 3",
            videoUrl: "",
            videoGenerationStatus: "NotGenerated",
            videoGenerationError: "",
            orderIndex: 1
          }
        ]
      }
    ];
    mockGetCourseLearnPayload.mockResolvedValue(payload);

    render(
      <MemoryRouter initialEntries={["/courses/course-1/learn"]}>
        <Routes>
          <Route path="/courses/:courseId/learn" element={<CourseLearnPage />} />
        </Routes>
      </MemoryRouter>
    );

    expect(await screen.findByRole("button", { name: /Tổng quan về AI/i })).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: /3. Du lieu va hoc may/i }));

    expect(await screen.findByRole("button", { name: /Du lieu lon/i })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Tổng quan về AI/i })).not.toBeInTheDocument();
  });
});
