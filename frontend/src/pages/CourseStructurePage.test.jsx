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

describe("CourseStructurePage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
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

  it("renders generated modules and lessons", async () => {
    mockGetCourseStructure.mockResolvedValue(baseCourse);

    renderPage();

    expect(await screen.findByRole("heading", { name: "Cấu trúc khóa học" })).toBeInTheDocument();
    expect(await screen.findByText("Chuong 1")).toBeInTheDocument();
    expect(await screen.findByText("Bai 1")).toBeInTheDocument();
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

  it("updates a module inline", async () => {
    mockGetCourseStructure.mockResolvedValue({
      ...baseCourse,
      modules: [{ ...baseCourse.modules[0], lessons: [] }]
    });
    mockUpdateModule.mockResolvedValue({
      id: "module-1",
      title: "Chuong 1 moi",
      description: "Mo ta moi",
      orderIndex: 1,
      lessons: []
    });

    renderPage();

    fireEvent.click(await screen.findByRole("button", { name: "Sửa module" }));
    fireEvent.change(screen.getByLabelText("Tiêu đề module"), { target: { value: "Chuong 1 moi" } });
    fireEvent.change(screen.getByLabelText("Mô tả module"), { target: { value: "Mo ta moi" } });
    fireEvent.click(screen.getByRole("button", { name: "Lưu module" }));

    await waitFor(() => expect(mockUpdateModule).toHaveBeenCalledWith("module-1", {
      title: "Chuong 1 moi",
      description: "Mo ta moi"
    }));
    expect(await screen.findByText("Đã cập nhật module.")).toBeInTheDocument();
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

    fireEvent.click(await screen.findByRole("button", { name: "Xem nội dung AI" }));
    expect(await screen.findByText("Script goc")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Chỉnh nội dung AI" }));
    fireEvent.change(screen.getByLabelText("Giọng điệu"), { target: { value: "Warm" } });
    fireEvent.click(screen.getByRole("button", { name: "Lưu nội dung AI" }));

    await waitFor(() => expect(mockUpdateLessonGeneratedContent).toHaveBeenCalledWith("lesson-1", {
      teachingScript: "Script goc",
      slideOutlineJson: '[{"slideNumber":1,"title":"S1","bulletPoints":["A"],"speakerNotes":"N"}]',
      voiceoverPlanJson:
        '{"estimatedDurationMinutes":8,"tone":"Warm","pacing":"Moderate","targetAudience":"Students","pronunciationNotes":"OOP"}'
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

    fireEvent.click(await screen.findByRole("button", { name: "Generate audio" }));

    await waitFor(() => expect(mockGenerateLessonAudio).toHaveBeenCalledWith("course-1", "lesson-1"));
  });
});
