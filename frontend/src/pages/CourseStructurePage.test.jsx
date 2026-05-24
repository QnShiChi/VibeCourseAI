import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import CourseStructurePage from "./CourseStructurePage";

const mockGetCourseStructure = vi.fn();
const mockUpdateModule = vi.fn();
const mockUpdateLesson = vi.fn();
const mockUploadCourseThumbnail = vi.fn();
const mockUpdateCourseCategory = vi.fn();
const mockGenerateCourseLessonContent = vi.fn();
const mockGenerateCourseLessonAudio = vi.fn();
const mockGenerateCourseLessonVideo = vi.fn();
const mockGenerateLessonAudio = vi.fn();
const mockGenerateLessonVideo = vi.fn();
const mockRegenerateLessonAudio = vi.fn();
const mockRegenerateLessonVideo = vi.fn();
const mockRegenerateLessonContent = vi.fn();
const mockGetLessonGeneratedContent = vi.fn();
const mockGetLessonAudio = vi.fn();
const mockGetLessonVideo = vi.fn();
const mockUpdateLessonGeneratedContent = vi.fn();
const mockGetGenerationJobs = vi.fn();
const mockGetGenerationJobDetail = vi.fn();

vi.mock("../api/courseStructureService", () => ({
  getCourseStructure: (...args) => mockGetCourseStructure(...args),
  updateModule: (...args) => mockUpdateModule(...args),
  updateLesson: (...args) => mockUpdateLesson(...args),
  uploadCourseThumbnail: (...args) => mockUploadCourseThumbnail(...args),
  updateCourseCategory: (...args) => mockUpdateCourseCategory(...args)
}));

vi.mock("../api/lessonContentService", () => ({
  generateCourseLessonContent: (...args) => mockGenerateCourseLessonContent(...args),
  generateCourseLessonAudio: (...args) => mockGenerateCourseLessonAudio(...args),
  generateCourseLessonVideo: (...args) => mockGenerateCourseLessonVideo(...args),
  generateLessonAudio: (...args) => mockGenerateLessonAudio(...args),
  generateLessonVideo: (...args) => mockGenerateLessonVideo(...args),
  regenerateLessonAudio: (...args) => mockRegenerateLessonAudio(...args),
  regenerateLessonVideo: (...args) => mockRegenerateLessonVideo(...args),
  regenerateLessonContent: (...args) => mockRegenerateLessonContent(...args),
  getLessonGeneratedContent: (...args) => mockGetLessonGeneratedContent(...args),
  getLessonAudio: (...args) => mockGetLessonAudio(...args),
  getLessonVideo: (...args) => mockGetLessonVideo(...args),
  updateLessonGeneratedContent: (...args) => mockUpdateLessonGeneratedContent(...args)
}));

vi.mock("../api/generationJobService", () => ({
  getGenerationJobs: (...args) => mockGetGenerationJobs(...args),
  getGenerationJobDetail: (...args) => mockGetGenerationJobDetail(...args)
}));

const baseCourse = {
  id: "course-1",
  title: "OOP",
  description: "Course description",
  category: "UiUxDesign",
  thumbnailUrl: "",
  modules: [
    {
      id: "module-1",
      title: "Chuong 1",
      description: "Tong quan",
      orderIndex: 1,
      lessons: [
        {
          id: "lesson-1",
          title: "Bai 1",
          description: "Mo dau",
          orderIndex: 1,
          contentSeed: "Noi dung",
          contentGenerationStatus: "NotGenerated",
          contentGenerationError: "",
          audioGenerationStatus: "NotGenerated",
          audioGenerationError: "",
          audioUrl: "",
          videoGenerationStatus: "NotGenerated",
          videoGenerationError: "",
          videoUrl: ""
        }
      ]
    }
  ]
};

function renderPage() {
  render(
    <MemoryRouter initialEntries={["/admin/courses/course-1"]}>
      <Routes>
        <Route path="/admin/courses/:courseId" element={<CourseStructurePage />} />
      </Routes>
    </MemoryRouter>
  );
}

async function selectCentralizedLesson(lessonId = "lesson-1") {
  const lessonSelect = await screen.findByRole("combobox", { name: "Chọn bài học để điều khiển" });
  fireEvent.change(lessonSelect, { target: { value: lessonId } });
  return lessonSelect;
}

describe("CourseStructurePage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    Object.defineProperty(window, "scrollTo", {
      configurable: true,
      value: vi.fn()
    });
    Object.defineProperty(window, "scrollY", {
      configurable: true,
      writable: true,
      value: 0
    });
    mockGetGenerationJobs.mockResolvedValue([]);
    mockGetGenerationJobDetail.mockResolvedValue({
      id: "job-1",
      courseId: "course-1",
      jobType: "GenerateLessonContent",
      status: "Completed",
      totalItems: 1,
      processedItems: 1,
      failedItems: 0,
      progressMessage: "Đã generate nội dung cho toàn bộ lesson cần xử lý.",
      errorMessage: ""
    });
  });

  it("renders the page without module cards", async () => {
    mockGetCourseStructure.mockResolvedValue({
      ...baseCourse,
      modules: [
        {
          ...baseCourse.modules[0],
          lessons: [
            {
              ...baseCourse.modules[0].lessons[0],
              title: "Bai 1"
            },
            {
              ...baseCourse.modules[0].lessons[0],
              id: "lesson-2",
              orderIndex: 2,
              title: "Bai 2"
            }
          ]
        }
      ]
    });

    renderPage();

    expect(await screen.findByRole("heading", { name: "Cấu trúc khóa học" })).toBeInTheDocument();
    expect(await screen.findByText("Bảng điều khiển tác vụ Lesson tập trung")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Xem chi tiết module" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Sửa module" })).not.toBeInTheDocument();
  });

  it("renders the course structure workspace with themed styling hooks", async () => {
    mockGetCourseStructure.mockResolvedValue(baseCourse);

    renderPage();

    const heading = await screen.findByRole("heading", { name: "Cấu trúc khóa học" });
    expect(heading.closest("section")).toHaveClass("course-structure-workspace");
    expect(screen.getByText("Course draft").closest(".surface-card")).toHaveClass("course-structure-hero-card");
  });

  it("renders course thumbnail preview and selected category", async () => {
    mockGetCourseStructure.mockResolvedValue({
      ...baseCourse,
      category: "AiAndData",
      thumbnailUrl: "/storage/course-thumbnails/ai.png"
    });

    renderPage();

    expect(await screen.findByAltText("Thumbnail khóa học OOP")).toHaveAttribute("src", "/storage/course-thumbnails/ai.png");
    expect(screen.getByLabelText("Danh mục khóa học")).toHaveValue("AiAndData");
  });

  it("uploads a new thumbnail", async () => {
    mockGetCourseStructure.mockResolvedValue({ ...baseCourse, category: "UiUxDesign", thumbnailUrl: "" });
    mockUploadCourseThumbnail.mockResolvedValue({
      ...baseCourse,
      category: "UiUxDesign",
      thumbnailUrl: "/storage/course-thumbnails/new-thumb.png"
    });

    renderPage();

    const file = new File(["thumb"], "thumb.png", { type: "image/png" });
    fireEvent.change(await screen.findByLabelText("Ảnh thumbnail"), { target: { files: [file] } });
    fireEvent.click(screen.getByRole("button", { name: "Upload thumbnail" }));

    await waitFor(() => expect(mockUploadCourseThumbnail).toHaveBeenCalledWith("course-1", file));
  });

  it("shows selected lesson details from the centralized panel", async () => {
    mockGetCourseStructure.mockResolvedValue(baseCourse);

    renderPage();

    await selectCentralizedLesson("lesson-1");

    expect(await screen.findByRole("heading", { name: "Bài 1: Bai 1" })).toBeInTheDocument();
    expect(screen.getByText("Thuộc Module 1")).toBeInTheDocument();
  });

  it("shows progress for an active background generation job", async () => {
    mockGetCourseStructure.mockResolvedValue(baseCourse);
    mockGetGenerationJobs.mockResolvedValue([
      {
        id: "job-1",
        courseId: "course-1",
        jobType: "GenerateLessonContent",
        status: "GeneratingLessonContent",
        totalItems: 4,
        processedItems: 1,
        failedItems: 0,
        progressMessage: "Đang xử lý lesson 2/4",
        errorMessage: ""
      }
    ]);
    mockGetGenerationJobDetail.mockResolvedValue({
      id: "job-1",
      courseId: "course-1",
      jobType: "GenerateLessonContent",
      status: "GeneratingLessonContent",
      totalItems: 4,
      processedItems: 1,
      failedItems: 0,
      progressMessage: "Đang xử lý lesson 2/4",
      errorMessage: ""
    });

    renderPage();

    expect(await screen.findByLabelText("Tiến trình generate nội dung bài học")).toBeInTheDocument();
    expect(await screen.findByText("1/4 lesson đã xử lý")).toBeInTheDocument();
  });

  it("starts whole-course lesson content generation as a background job", async () => {
    mockGetCourseStructure.mockResolvedValue(baseCourse);
    mockGenerateCourseLessonContent.mockResolvedValue({
      jobId: "job-1",
      status: "Pending",
      totalLessons: 1,
      failedLessons: 0,
      message: "Đã tạo job generate 1 lesson."
    });

    renderPage();

    fireEvent.click(await screen.findByRole("button", { name: "Generate nội dung bài học" }));

    await waitFor(() => expect(mockGenerateCourseLessonContent).toHaveBeenCalledWith("course-1"));
    await waitFor(() => expect(mockGetGenerationJobDetail).toHaveBeenCalledWith("job-1"));
    expect(await screen.findAllByText("Đã generate nội dung cho toàn bộ lesson cần xử lý.")).toHaveLength(2);
  });

  it("starts whole-course lesson video generation as a background job", async () => {
    mockGetCourseStructure.mockResolvedValue({
      ...baseCourse,
      modules: [
        {
          ...baseCourse.modules[0],
          lessons: [
            {
              ...baseCourse.modules[0].lessons[0],
              audioGenerationStatus: "Completed",
              audioUrl: "/storage/audio/lesson-1.wav"
            }
          ]
        }
      ]
    });
    mockGenerateCourseLessonVideo.mockResolvedValue({
      jobId: "video-job-1",
      status: "Pending",
      totalLessons: 1,
      failedLessons: 0,
      message: "Đã tạo job generate video 1 lesson."
    });

    renderPage();

    fireEvent.click(await screen.findByRole("button", { name: "Generate video khóa học" }));

    await waitFor(() => expect(mockGenerateCourseLessonVideo).toHaveBeenCalledWith("course-1"));
  });

  it("allows regenerating a failed lesson", async () => {
    mockGetCourseStructure.mockResolvedValue({
      ...baseCourse,
      modules: [
        {
          ...baseCourse.modules[0],
          lessons: [
            {
              ...baseCourse.modules[0].lessons[0],
              contentGenerationStatus: "Failed",
              contentGenerationError: "schema invalid"
            }
          ]
        }
      ]
    });
    mockRegenerateLessonContent.mockResolvedValue({
      jobId: "job-2",
      status: "Pending",
      totalLessons: 1,
      failedLessons: 0,
      message: "Đã tạo job generate lại lesson \"Bai 1\"."
    });

    renderPage();

    await selectCentralizedLesson("lesson-1");
    fireEvent.click(await screen.findByRole("button", { name: "Generate lại lesson lỗi" }));

    await waitFor(() => expect(mockRegenerateLessonContent).toHaveBeenCalledWith("course-1", "lesson-1"));
    expect(await screen.findByText(/schema invalid/i)).toBeInTheDocument();
  });

  it("loads and saves generated lesson content", async () => {
    mockGetCourseStructure.mockResolvedValue({
      ...baseCourse,
      modules: [
        {
          ...baseCourse.modules[0],
          lessons: [
            {
              ...baseCourse.modules[0].lessons[0],
              contentGenerationStatus: "Completed"
            }
          ]
        }
      ]
    });
    mockGetLessonGeneratedContent.mockResolvedValue({
      lessonId: "lesson-1",
      lessonTitle: "Bai 1",
      teachingScript: "Script goc",
      slideOutlineJson: '[{"slideNumber":1,"title":"S1","bulletPoints":["A"],"speakerNotes":"N"}]',
      voiceoverPlanJson:
        '{"EstimatedDurationMinutes":8,"Tone":"Clear","Pacing":"Moderate","TargetAudience":"Students","PronunciationNotes":"OOP"}',
      contentGenerationStatus: "Completed"
    });
    mockUpdateLessonGeneratedContent.mockResolvedValue({
      lessonId: "lesson-1",
      lessonTitle: "Bai 1",
      teachingScript: "Script goc",
      slideOutlineJson: '[{"slideNumber":1,"title":"S1","bulletPoints":["A"],"speakerNotes":"N"}]',
      voiceoverPlanJson:
        '{"estimatedDurationMinutes":8,"tone":"Warm","pacing":"Moderate","targetAudience":"Students","pronunciationNotes":"OOP"}',
      contentGenerationStatus: "ManuallyEdited"
    });

    renderPage();

    await selectCentralizedLesson("lesson-1");
    fireEvent.click(screen.getByRole("button", { name: "Nội dung AI" }));
    expect(await screen.findByText("Script goc")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Chỉnh sửa nội dung AI" }));
    fireEvent.change(screen.getByLabelText("Giọng điệu"), { target: { value: "Warm" } });
    fireEvent.click(screen.getByRole("button", { name: "Lưu nội dung AI" }));

    await waitFor(() => expect(mockUpdateLessonGeneratedContent).toHaveBeenCalledWith("lesson-1", {
      teachingScript: "Script goc",
      slideOutlineJson: '[{"slideNumber":1,"title":"S1","bulletPoints":["A"],"speakerNotes":"N"}]',
      voiceoverPlanJson:
        '{"estimatedDurationMinutes":8,"tone":"Warm","pacing":"Moderate","targetAudience":"Students","pronunciationNotes":"OOP"}'
    }));
  });

  it("allows editing the parent module from the centralized panel", async () => {
    mockGetCourseStructure.mockResolvedValue(baseCourse);
    mockUpdateModule.mockResolvedValue({
      ...baseCourse.modules[0],
      title: "Chuong 1 da sua",
      description: "Tong quan moi"
    });

    renderPage();

    await selectCentralizedLesson("lesson-1");
    fireEvent.click(screen.getByRole("button", { name: "Sửa thông tin" }));
    fireEvent.click(screen.getByRole("button", { name: "Chỉnh sửa thông tin module" }));
    fireEvent.change(screen.getByLabelText("Tiêu đề module"), { target: { value: "Chuong 1 da sua" } });
    fireEvent.change(screen.getByLabelText("Mô tả module"), { target: { value: "Tong quan moi" } });
    fireEvent.click(screen.getByRole("button", { name: "Lưu module" }));

    await waitFor(() => expect(mockUpdateModule).toHaveBeenCalledWith("module-1", {
      title: "Chuong 1 da sua",
      description: "Tong quan moi"
    }));
  });

  it("starts lesson audio generation and shows audio controls", async () => {
    mockGetCourseStructure.mockResolvedValue({
      ...baseCourse,
      modules: [
        {
          ...baseCourse.modules[0],
          lessons: [
            {
              ...baseCourse.modules[0].lessons[0],
              contentGenerationStatus: "Completed",
              audioGenerationStatus: "NotGenerated",
              audioUrl: ""
            }
          ]
        }
      ]
    });
    mockGenerateLessonAudio.mockResolvedValue({
      jobId: "audio-job-1",
      status: "Pending",
      totalLessons: 1,
      failedLessons: 0,
      message: "Đã tạo job generate audio cho lesson."
    });

    renderPage();

    await selectCentralizedLesson("lesson-1");
    fireEvent.click(await screen.findByRole("button", { name: "Generate audio" }));

    await waitFor(() => expect(mockGenerateLessonAudio).toHaveBeenCalledWith("course-1", "lesson-1"));
  });

  it("selects a lesson from the centralized dropdown", async () => {
    mockGetCourseStructure.mockResolvedValue({
      ...baseCourse,
      modules: [
        {
          ...baseCourse.modules[0],
          lessons: [
            {
              ...baseCourse.modules[0].lessons[0],
              title: "Bai 1"
            },
            {
              ...baseCourse.modules[0].lessons[0],
              id: "lesson-2",
              orderIndex: 2,
              title: "Bai 2"
            }
          ]
        }
      ]
    });

    renderPage();
    await selectCentralizedLesson("lesson-2");

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Bài 2: Bai 2" })).toBeInTheDocument();
    });
  });

  it("updates the centralized dropdown selection when choosing a lesson", async () => {
    mockGetCourseStructure.mockResolvedValue(baseCourse);

    renderPage();
    const lessonSelect = await selectCentralizedLesson("lesson-1");

    expect(lessonSelect).toHaveValue("lesson-1");
    expect(await screen.findByRole("heading", { name: "Bài 1: Bai 1" })).toBeInTheDocument();
  });

  it("keeps module cards hidden after selecting a lesson", async () => {
    mockGetCourseStructure.mockResolvedValue({
      ...baseCourse,
      modules: [
        {
          ...baseCourse.modules[0],
          lessons: [
            {
              ...baseCourse.modules[0].lessons[0],
              title: "Bai 1"
            },
            {
              ...baseCourse.modules[0].lessons[0],
              id: "lesson-2",
              orderIndex: 2,
              title: "Bai 2"
            }
          ]
        }
      ]
    });

    renderPage();

    await selectCentralizedLesson("lesson-2");

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Bài 2: Bai 2" })).toBeInTheDocument();
    });

    expect(screen.queryByRole("button", { name: "Xem chi tiết module" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Sửa module" })).not.toBeInTheDocument();
  });
});
