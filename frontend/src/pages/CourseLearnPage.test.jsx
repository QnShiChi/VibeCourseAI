import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ThemeProvider } from "../theme/ThemeContext";
import CourseLearnPage from "./CourseLearnPage";

const mockGetCourseLearnPayload = vi.fn();
const mockGetLessonQuiz = vi.fn();
const mockStartQuizAttempt = vi.fn();
const mockSubmitQuizAttempt = vi.fn();
const mockGetLessonComments = vi.fn();
const mockUseAuth = vi.fn();

vi.mock("../api/courseService", () => ({
  getCourseLearnPayload: (...args) => mockGetCourseLearnPayload(...args)
}));

vi.mock("../api/quizService", () => ({
  getLessonQuiz: (...args) => mockGetLessonQuiz(...args),
  startQuizAttempt: (...args) => mockStartQuizAttempt(...args),
  submitQuizAttempt: (...args) => mockSubmitQuizAttempt(...args)
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

const mockUseLessonVoiceTutor = vi.fn();

vi.mock("../hooks/useLessonVoiceTutor", () => ({
  useLessonVoiceTutor: (...args) => mockUseLessonVoiceTutor(...args)
}));

function buildLearnPayload() {
  return {
    courseId: "course-1",
    courseTitle: "TRÍ TUỆ NHÂN TẠO ỨNG DỤNG",
    courseDescription: "Desc",
    hasFinalQuiz: true,
    finalQuizId: "final-quiz-1",
    finalQuizStatus: "Ready",
    finalQuizQuestionCount: 15,
    selectedLessonId: "lesson-1",
    selectedLesson: {
      lessonId: "lesson-1",
      lessonTitle: "Tổng quan về AI",
      description: "Mo dau",
      contentSeed: "Noi dung lesson 1",
      videoUrl: "",
      videoGenerationStatus: "NotGenerated",
      videoGenerationError: "",
      orderIndex: 1,
      quizId: "quiz-1",
      quizStatus: "Ready",
      quizQuestionCount: 3
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
            orderIndex: 1,
            quizId: "quiz-1",
            quizStatus: "Ready",
            quizQuestionCount: 3
          },
          {
            lessonId: "lesson-2",
            lessonTitle: "Các mốc lịch sử",
            description: "Tiep theo",
            contentSeed: "Noi dung lesson 2",
            videoUrl: "",
            videoGenerationStatus: "NotGenerated",
            videoGenerationError: "",
            orderIndex: 2,
            quizId: "quiz-2",
            quizStatus: "Generating",
            quizQuestionCount: 0
          }
        ]
      }
    ]
  };
}

describe("CourseLearnPage", () => {
  beforeEach(() => {
    window.localStorage.clear();
    window.localStorage.setItem("app-theme", "light");
    mockUseAuth.mockReturnValue({ user: { role: "User" } });
    mockUseLessonVoiceTutor.mockReturnValue({
      state: "idle",
      errorMessage: "",
      startRecording: vi.fn(),
      stopRecording: vi.fn(),
      requestFollowUp: vi.fn(),
      resumeLearning: vi.fn()
    });
    mockGetLessonComments.mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 10,
      totalCount: 0,
      hasMore: false,
      sort: "newest"
    });
    mockGetLessonQuiz.mockResolvedValue({
      quizId: "quiz-1",
      title: "Kiem tra nhanh",
      status: "Ready",
      questionCount: 1,
      questions: [
        {
          questionId: "q1",
          questionText: "AI mo phong dieu gi?",
          explanation: "AI mo phong tri tue con nguoi.",
          options: [
            { optionId: "o1", optionText: "Tri tue con nguoi" },
            { optionId: "o2", optionText: "May in" },
            { optionId: "o3", optionText: "Ban phim" },
            { optionId: "o4", optionText: "Loa" }
          ]
        }
      ]
    });
    mockStartQuizAttempt.mockResolvedValue({ attemptId: "attempt-1", startedAt: "2026-05-28T00:00:00Z" });
    mockSubmitQuizAttempt.mockResolvedValue({
      attemptId: "attempt-1",
      score: 100,
      correctCount: 1,
      totalQuestions: 1,
      answers: [
        {
          questionId: "q1",
          selectedOptionId: "o1",
          correctOptionId: "o1",
          isCorrect: true,
          explanation: "AI mo phong tri tue con nguoi."
        }
      ]
    });
  });

  it("provides a themed learner workspace scope in dark mode", async () => {
    window.localStorage.setItem("app-theme", "dark");
    mockGetCourseLearnPayload.mockResolvedValue(buildLearnPayload());

    render(
      <MemoryRouter initialEntries={["/courses/course-1/learn"]}>
        <ThemeProvider>
          <Routes>
            <Route path="/courses/:courseId/learn" element={<CourseLearnPage />} />
          </Routes>
        </ThemeProvider>
      </MemoryRouter>
    );

    await screen.findAllByText("Tổng quan về AI");
    expect(screen.getByTestId("course-learn-shell")).toHaveAttribute("data-theme", "dark");
  });

  it("renders default selected lesson", async () => {
    mockGetCourseLearnPayload.mockResolvedValue(buildLearnPayload());

    render(
      <MemoryRouter initialEntries={["/courses/course-1/learn"]}>
        <ThemeProvider>
          <Routes>
            <Route path="/courses/:courseId/learn" element={<CourseLearnPage />} />
          </Routes>
        </ThemeProvider>
      </MemoryRouter>
    );

    expect(await screen.findAllByText("Tổng quan về AI")).not.toHaveLength(0);
    expect(await screen.findByText("Noi dung lesson 1")).toBeInTheDocument();
    expect(await screen.findByRole("heading", { name: "Bình luận" })).toBeInTheDocument();
    expect(await screen.findByText("Quiz tong ket khoa hoc")).toBeInTheDocument();
  });

  it("submits the lesson quiz inline", async () => {
    mockGetCourseLearnPayload.mockResolvedValue(buildLearnPayload());

    render(
      <MemoryRouter initialEntries={["/courses/course-1/learn"]}>
        <ThemeProvider>
          <Routes>
            <Route path="/courses/:courseId/learn" element={<CourseLearnPage />} />
          </Routes>
        </ThemeProvider>
      </MemoryRouter>
    );

    fireEvent.click(await screen.findByRole("button", { name: "Lam quiz" }));
    fireEvent.click(await screen.findByLabelText("Tri tue con nguoi"));
    fireEvent.click(screen.getByRole("button", { name: "Nop bai" }));

    expect(await screen.findByText("Diem: 100")).toBeInTheDocument();
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
        <ThemeProvider>
          <Routes>
            <Route path="/courses/:courseId/learn" element={<CourseLearnPage />} />
          </Routes>
        </ThemeProvider>
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
        <ThemeProvider>
          <Routes>
            <Route path="/courses/:courseId/learn" element={<CourseLearnPage />} />
          </Routes>
        </ThemeProvider>
      </MemoryRouter>
    );

    await screen.findAllByText("Lesson 1");
    expect(container.querySelector("video")).not.toBeNull();
  });

  it("shows the floating lesson voice tutor action when the selected lesson has a video", async () => {
    const payload = buildLearnPayload();
    payload.selectedLesson.videoUrl = "/storage/video/lesson-1.mp4";
    payload.selectedLesson.videoGenerationStatus = "Completed";
    payload.modules[0].lessons[0].videoUrl = "/storage/video/lesson-1.mp4";
    payload.modules[0].lessons[0].videoGenerationStatus = "Completed";
    mockGetCourseLearnPayload.mockResolvedValue(payload);

    render(
      <MemoryRouter initialEntries={["/courses/course-1/learn"]}>
        <ThemeProvider>
          <Routes>
            <Route path="/courses/:courseId/learn" element={<CourseLearnPage />} />
          </Routes>
        </ThemeProvider>
      </MemoryRouter>
    );

    expect(await screen.findByRole("button", { name: "Hỏi ngay" })).toBeInTheDocument();
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
        <ThemeProvider>
          <Routes>
            <Route path="/courses/:courseId/learn" element={<CourseLearnPage />} />
          </Routes>
        </ThemeProvider>
      </MemoryRouter>
    );

    expect(await screen.findByText("Video này giải thích khá rõ.")).toBeInTheDocument();
  });

  it("shows progress and moves to the next lesson from the footer navigation", async () => {
    mockGetCourseLearnPayload.mockResolvedValue(buildLearnPayload());

    render(
      <MemoryRouter initialEntries={["/courses/course-1/learn"]}>
        <ThemeProvider>
          <Routes>
            <Route path="/courses/:courseId/learn" element={<CourseLearnPage />} />
          </Routes>
        </ThemeProvider>
      </MemoryRouter>
    );

    expect(await screen.findByText(/Tiến độ: 0%/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Bài trước/i })).toBeDisabled();

    fireEvent.click(screen.getByRole("button", { name: /Tiếp tục bài học/i }));

    expect(await screen.findByText("Noi dung lesson 2")).toBeInTheDocument();
    expect(await screen.findByText(/Tiến độ: 0%/i)).toBeInTheDocument();
  });

  it("moves back to the previous lesson and renders the course content heading", async () => {
    const payload = buildLearnPayload();
    payload.selectedLessonId = "lesson-2";
    payload.selectedLesson = payload.modules[0].lessons[1];
    mockGetCourseLearnPayload.mockResolvedValue(payload);

    render(
      <MemoryRouter initialEntries={["/courses/course-1/learn"]}>
        <ThemeProvider>
          <Routes>
            <Route path="/courses/:courseId/learn" element={<CourseLearnPage />} />
          </Routes>
        </ThemeProvider>
      </MemoryRouter>
    );

    expect(await screen.findByRole("heading", { name: /Nội dung khóa học/i })).toBeInTheDocument();
    expect(await screen.findByText(/Tiến độ: 0%/i)).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: /Bài trước/i }));

    expect(await screen.findByText("Noi dung lesson 1")).toBeInTheDocument();
    expect(await screen.findByText(/Tiến độ: 0%/i)).toBeInTheDocument();
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
        <ThemeProvider>
          <Routes>
            <Route path="/courses/:courseId/learn" element={<CourseLearnPage />} />
          </Routes>
        </ThemeProvider>
      </MemoryRouter>
    );

    expect(await screen.findByRole("button", { name: /Tổng quan về AI/i })).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: /3. Du lieu va hoc may/i }));

    expect(await screen.findByRole("button", { name: /Du lieu lon/i })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Tổng quan về AI/i })).not.toBeInTheDocument();
  });
});
