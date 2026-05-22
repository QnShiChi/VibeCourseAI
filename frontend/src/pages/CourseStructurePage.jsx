import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import {
  getCourseStructure,
  updateCourseCategory,
  updateLesson,
  updateModule,
  uploadCourseThumbnail
} from "../api/courseStructureService";
import { COURSE_CATEGORY_OPTIONS } from "../constants/coursePresentation";
import {
  generateCourseLessonContent,
  generateCourseLessonAudio,
  generateCourseLessonVideo,
  generateLessonAudio,
  generateLessonVideo,
  getLessonGeneratedContent,
  getLessonAudio,
  getLessonVideo,
  regenerateLessonAudio,
  regenerateLessonVideo,
  regenerateLessonContent,
  updateLessonGeneratedContent
} from "../api/lessonContentService";
import { getGenerationJobDetail, getGenerationJobs } from "../api/generationJobService";
import Button from "../components/ui/Button";
import LessonContentEditor from "../components/course/LessonContentEditor";
import LessonContentPreview from "../components/course/LessonContentPreview";
import LessonContentStatusBadge from "../components/course/LessonContentStatusBadge";
import Card from "../components/ui/Card";
import FormField from "../components/ui/FormField";
import PageHeader from "../components/ui/PageHeader";
import Section from "../components/ui/Section";

export default function CourseStructurePage() {
  const { courseId = "" } = useParams();
  const [course, setCourse] = useState(null);
  const [editingModuleId, setEditingModuleId] = useState(null);
  const [editingLessonId, setEditingLessonId] = useState(null);
  const [editingGeneratedLessonId, setEditingGeneratedLessonId] = useState(null);
  const [moduleForm, setModuleForm] = useState({ title: "", description: "" });
  const [lessonForm, setLessonForm] = useState({ title: "", description: "", contentSeed: "" });
  const [generatedContentForm, setGeneratedContentForm] = useState({
    teachingScript: "",
    slideOutlineJson: "",
    voiceoverPlanJson: ""
  });
  const [selectedGeneratedLessonId, setSelectedGeneratedLessonId] = useState(null);
  const [generatedContentByLessonId, setGeneratedContentByLessonId] = useState({});
  const [audioByLessonId, setAudioByLessonId] = useState({});
  const [videoByLessonId, setVideoByLessonId] = useState({});
  const [activeJobId, setActiveJobId] = useState(null);
  const [activeJob, setActiveJob] = useState(null);
  const [message, setMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isGeneratingContent, setIsGeneratingContent] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState("UiUxDesign");
  const [thumbnailFile, setThumbnailFile] = useState(null);
  const [isSavingPresentation, setIsSavingPresentation] = useState(false);

  useEffect(() => {
    if (courseId) {
      loadCourse();
      discoverActiveJob();
    }
  }, [courseId]);

  useEffect(() => {
    if (!activeJobId) {
      return undefined;
    }

    let isCancelled = false;

    async function pollJob() {
      try {
        const detail = await getGenerationJobDetail(activeJobId);
        if (isCancelled) {
          return;
        }

        setActiveJob(detail);

        if (!isJobActive(detail.status)) {
          setActiveJobId(null);
          setIsGeneratingContent(false);
          setMessage(detail.progressMessage || "Job generate đã hoàn tất.");
          await loadCourse();
        }
      } catch {
        if (!isCancelled) {
          setErrorMessage("Không thể tải tiến độ generate nội dung bài học.");
        }
      }
    }

    pollJob();
    const timerId = window.setInterval(pollJob, 2500);

    return () => {
      isCancelled = true;
      window.clearInterval(timerId);
    };
  }, [activeJobId]);

  async function loadCourse() {
    setIsLoading(true);
    setErrorMessage("");
    try {
      const data = await getCourseStructure(courseId);
      setCourse(data);
      setSelectedCategory(data.category || "UiUxDesign");
      setThumbnailFile(null);
    } catch {
      setErrorMessage("Không thể tải cấu trúc khóa học.");
    } finally {
      setIsLoading(false);
    }
  }

  async function discoverActiveJob({ silent = true } = {}) {
    try {
      const jobs = await getGenerationJobs();
      const currentJob = jobs.find(
        (job) =>
          job.courseId === courseId &&
          isCourseJobType(job.jobType) &&
          isJobActive(job.status)
      );

      if (currentJob) {
        setActiveJob(currentJob);
        setActiveJobId(currentJob.id);
        setIsGeneratingContent(true);
        return currentJob;
      }

      setActiveJob(null);
      setActiveJobId(null);
      setIsGeneratingContent(false);
      return null;
    } catch {
      if (!silent) {
        setErrorMessage("Không thể tải tiến độ job generate.");
      }
      return null;
    }
  }

  function startEditModule(module) {
    setEditingModuleId(module.id);
    setModuleForm({ title: module.title, description: module.description });
  }

  function startEditLesson(lesson) {
    setEditingLessonId(lesson.id);
    setLessonForm({
      title: lesson.title,
      description: lesson.description,
      contentSeed: lesson.contentSeed
    });
  }

  async function handleGenerateLessonContent() {
    setMessage("");
    setErrorMessage("");
    setIsGeneratingContent(true);
    try {
      const response = await generateCourseLessonContent(courseId);
      setActiveJobId(response.jobId);
      setActiveJob({
        id: response.jobId,
        jobType: "GenerateLessonContent",
        status: response.status,
        totalItems: response.totalLessons,
        processedItems: 0,
        failedItems: 0,
        progressMessage: response.message,
        courseId
      });
      setMessage(response.message || "Đã tạo job generate nội dung bài học.");
      await loadCourse();
    } catch (error) {
      const apiMessage = error?.response?.data?.message ?? "";
      if (apiMessage.includes("đang có job generate nội dung lesson chạy nền")) {
        const existingJob = await discoverActiveJob({ silent: false });
        if (existingJob) {
          setMessage("Khóa học đang có job generate nền. Đã kết nối lại tiến trình hiện tại.");
          return;
        }
      }

      setIsGeneratingContent(false);
      setErrorMessage(apiMessage || "Không thể generate nội dung bài học.");
    }
  }

  async function handleGenerateLessonAudio() {
    setMessage("");
    setErrorMessage("");
    setIsGeneratingContent(true);
    try {
      const response = await generateCourseLessonAudio(courseId);
      setActiveJobId(response.jobId);
      setActiveJob({
        id: response.jobId,
        jobType: "GenerateLessonAudio",
        status: response.status,
        totalItems: response.totalLessons,
        processedItems: 0,
        failedItems: 0,
        progressMessage: response.message,
        courseId
      });
      setMessage(response.message || "Đã tạo job generate audio bài học.");
      await loadCourse();
    } catch (error) {
      const apiMessage = error?.response?.data?.message ?? "";
      if (apiMessage.includes("đang có job generate audio chạy nền")) {
        const existingJob = await discoverActiveJob({ silent: false });
        if (existingJob) {
          setMessage("Khóa học đang có job audio nền. Đã kết nối lại tiến trình hiện tại.");
          return;
        }
      }

      setIsGeneratingContent(false);
      setErrorMessage(apiMessage || "Không thể generate audio bài học.");
    }
  }

  async function handleGenerateLessonVideo() {
    setMessage("");
    setErrorMessage("");
    setIsGeneratingContent(true);
    try {
      const response = await generateCourseLessonVideo(courseId);
      setActiveJobId(response.jobId);
      setActiveJob({
        id: response.jobId,
        jobType: "GenerateLessonVideo",
        status: response.status,
        totalItems: response.totalLessons,
        processedItems: 0,
        failedItems: 0,
        progressMessage: response.message,
        courseId
      });
      setMessage(response.message || "Đã tạo job generate video bài học.");
      await loadCourse();
    } catch (error) {
      const apiMessage = error?.response?.data?.message ?? "";
      if (apiMessage.includes("đang có job generate video chạy nền")) {
        const existingJob = await discoverActiveJob({ silent: false });
        if (existingJob) {
          setMessage("Khóa học đang có job video nền. Đã kết nối lại tiến trình hiện tại.");
          return;
        }
      }

      setIsGeneratingContent(false);
      setErrorMessage(apiMessage || "Không thể generate video bài học.");
    }
  }

  async function handleRegenerateLessonContent(lessonId) {
    setMessage("");
    setErrorMessage("");
    setIsGeneratingContent(true);
    try {
      const response = await regenerateLessonContent(courseId, lessonId);
      setActiveJobId(response.jobId);
      setActiveJob({
        id: response.jobId,
        lessonId,
        jobType: "RegenerateLessonContent",
        status: response.status,
        totalItems: response.totalLessons,
        processedItems: 0,
        failedItems: 0,
        progressMessage: response.message,
        courseId
      });
      setMessage(response.message || "Đã tạo job generate lại lesson.");
      await loadCourse();
    } catch (error) {
      const apiMessage = error?.response?.data?.message ?? "";
      if (apiMessage.includes("đang có job generate nội dung lesson chạy nền")) {
        const existingJob = await discoverActiveJob({ silent: false });
        if (existingJob) {
          setMessage("Khóa học đang có job generate nền. Đã kết nối lại tiến trình hiện tại.");
          return;
        }
      }

      setIsGeneratingContent(false);
      setErrorMessage(apiMessage || "Không thể generate lại lesson này.");
    }
  }

  async function handleGenerateAudioForLesson(lessonId, regenerate = false) {
    setMessage("");
    setErrorMessage("");
    setIsGeneratingContent(true);
    try {
      const response = regenerate
        ? await regenerateLessonAudio(courseId, lessonId)
        : await generateLessonAudio(courseId, lessonId);
      setActiveJobId(response.jobId);
      setActiveJob({
        id: response.jobId,
        lessonId,
        jobType: regenerate ? "RegenerateLessonAudio" : "GenerateLessonAudio",
        status: response.status,
        totalItems: response.totalLessons,
        processedItems: 0,
        failedItems: 0,
        progressMessage: response.message,
        courseId
      });
      setMessage(response.message || "Đã tạo job generate audio lesson.");
      await loadCourse();
    } catch (error) {
      const apiMessage = error?.response?.data?.message ?? "";
      if (apiMessage.includes("đang có job generate audio chạy nền")) {
        const existingJob = await discoverActiveJob({ silent: false });
        if (existingJob) {
          setMessage("Khóa học đang có job audio nền. Đã kết nối lại tiến trình hiện tại.");
          return;
        }
      }

      setIsGeneratingContent(false);
      setErrorMessage(apiMessage || "Không thể generate audio cho lesson này.");
    }
  }

  async function handleViewGeneratedContent(lessonId) {
    setMessage("");
    setErrorMessage("");
    try {
      const content = await getLessonGeneratedContent(lessonId);
      setGeneratedContentByLessonId((current) => ({ ...current, [lessonId]: content }));
      setSelectedGeneratedLessonId(lessonId);
      setEditingGeneratedLessonId(null);
    } catch (error) {
      setErrorMessage(error?.response?.data?.message ?? "Không thể tải nội dung AI của lesson.");
    }
  }

  async function handleViewLessonAudio(lessonId) {
    setMessage("");
    setErrorMessage("");
    try {
      const audio = await getLessonAudio(lessonId);
      setAudioByLessonId((current) => ({ ...current, [lessonId]: audio }));
    } catch (error) {
      setErrorMessage(error?.response?.data?.message ?? "Không thể tải audio của lesson.");
    }
  }

  async function handleGenerateVideoForLesson(lessonId, regenerate = false) {
    setMessage("");
    setErrorMessage("");
    setIsGeneratingContent(true);
    try {
      const response = regenerate
        ? await regenerateLessonVideo(courseId, lessonId)
        : await generateLessonVideo(courseId, lessonId);
      setActiveJobId(response.jobId);
      setActiveJob({
        id: response.jobId,
        lessonId,
        jobType: regenerate ? "RegenerateLessonVideo" : "GenerateLessonVideo",
        status: response.status,
        totalItems: response.totalLessons,
        processedItems: 0,
        failedItems: 0,
        progressMessage: response.message,
        courseId
      });
      setMessage(response.message || "Đã tạo job generate video lesson.");
      await loadCourse();
    } catch (error) {
      const apiMessage = error?.response?.data?.message ?? "";
      if (apiMessage.includes("đang có job generate video chạy nền")) {
        const existingJob = await discoverActiveJob({ silent: false });
        if (existingJob) {
          setMessage("Khóa học đang có job video nền. Đã kết nối lại tiến trình hiện tại.");
          return;
        }
      }

      setIsGeneratingContent(false);
      setErrorMessage(apiMessage || "Không thể generate video cho lesson này.");
    }
  }

  async function handleViewLessonVideo(lessonId) {
    setMessage("");
    setErrorMessage("");
    try {
      const video = await getLessonVideo(lessonId);
      setVideoByLessonId((current) => ({ ...current, [lessonId]: video }));
    } catch (error) {
      setErrorMessage(error?.response?.data?.message ?? "Không thể tải video của lesson.");
    }
  }

  function handleStartEditingGeneratedContent(lessonId) {
    const content = generatedContentByLessonId[lessonId];
    if (!content) {
      return;
    }

    setEditingGeneratedLessonId(lessonId);
    setGeneratedContentForm({
      teachingScript: content.teachingScript || "",
      slideOutlineJson: content.slideOutlineJson || "",
      voiceoverPlanJson: content.voiceoverPlanJson || ""
    });
  }

  async function handleSaveGeneratedContent(lessonId) {
    setMessage("");
    setErrorMessage("");
    try {
      const updated = await updateLessonGeneratedContent(lessonId, generatedContentForm);
      setGeneratedContentByLessonId((current) => ({ ...current, [lessonId]: updated }));
      setCourse((current) => ({
        ...current,
        modules: current.modules.map((module) => ({
          ...module,
          lessons: module.lessons.map((lesson) =>
            lesson.id === lessonId
              ? {
                  ...lesson,
                  contentGenerationStatus: updated.contentGenerationStatus,
                  contentGenerationError: "",
                  audioGenerationStatus: "NotGenerated",
                  audioGenerationError: "",
                  audioUrl: "",
                  videoGenerationStatus: "NotGenerated",
                  videoGenerationError: "",
                  videoUrl: ""
                }
              : lesson
          )
        }))
      }));
      setAudioByLessonId((current) => ({ ...current, [lessonId]: null }));
      setVideoByLessonId((current) => ({ ...current, [lessonId]: null }));
      setEditingGeneratedLessonId(null);
      setSelectedGeneratedLessonId(lessonId);
      setMessage("Đã lưu nội dung AI của lesson.");
    } catch (error) {
      setErrorMessage(error?.response?.data?.message ?? "Không thể lưu nội dung AI của lesson.");
    }
  }

  async function handleModuleSave() {
    if (!editingModuleId) {
      return;
    }

    setMessage("");
    setErrorMessage("");
    try {
      const updated = await updateModule(editingModuleId, moduleForm);
      setCourse((current) => ({
        ...current,
        modules: current.modules.map((module) => (module.id === updated.id ? updated : module))
      }));
      setEditingModuleId(null);
      setMessage("Đã cập nhật module.");
    } catch (error) {
      setErrorMessage(error?.response?.data?.message ?? "Không thể cập nhật module.");
    }
  }

  async function handleLessonSave(moduleId) {
    if (!editingLessonId) {
      return;
    }

    setMessage("");
    setErrorMessage("");
    try {
      const updated = await updateLesson(editingLessonId, lessonForm);
      setCourse((current) => ({
        ...current,
        modules: current.modules.map((module) =>
          module.id !== moduleId
            ? module
            : {
                ...module,
                lessons: module.lessons.map((lesson) => (lesson.id === updated.id ? updated : lesson))
              }
        )
      }));
      setEditingLessonId(null);
      setMessage("Đã cập nhật lesson.");
    } catch (error) {
      setErrorMessage(error?.response?.data?.message ?? "Không thể cập nhật lesson.");
    }
  }

  async function handleSaveCategory() {
    setMessage("");
    setErrorMessage("");
    setIsSavingPresentation(true);
    try {
      const updatedCourse = await updateCourseCategory(courseId, selectedCategory);
      setCourse(updatedCourse);
      setSelectedCategory(updatedCourse.category || selectedCategory);
      setMessage("Đã cập nhật category khóa học.");
    } catch (error) {
      setErrorMessage(error?.response?.data?.message ?? "Không thể cập nhật category khóa học.");
    } finally {
      setIsSavingPresentation(false);
    }
  }

  async function handleUploadThumbnail() {
    if (!thumbnailFile) {
      setErrorMessage("Vui lòng chọn ảnh thumbnail hợp lệ.");
      return;
    }

    setMessage("");
    setErrorMessage("");
    setIsSavingPresentation(true);
    try {
      const updatedCourse = await uploadCourseThumbnail(courseId, thumbnailFile);
      setCourse(updatedCourse);
      setThumbnailFile(null);
      setMessage("Đã cập nhật thumbnail khóa học.");
    } catch (error) {
      setErrorMessage(error?.response?.data?.message ?? "Không thể upload thumbnail khóa học.");
    } finally {
      setIsSavingPresentation(false);
    }
  }

  const hasActiveJob = activeJob && isJobActive(activeJob.status);
  const processedItems = activeJob?.processedItems ?? 0;
  const failedItems = activeJob?.failedItems ?? 0;
  const totalItems = activeJob?.totalItems ?? 0;
  const successfulItems = Math.max(processedItems - failedItems, 0);
  const progressPercent = getProgressPercent(activeJob);

  return (
    <Section className="section-stack">
      <PageHeader
        eyebrow="Admin"
        title="Cấu trúc khóa học"
        description="Xem và tinh chỉnh skeleton course, module và lesson đã được sinh từ đề cương bằng backend ASP.NET Core Web API."
      />

      {message ? <p className="ui-alert ui-alert--success">{message}</p> : null}
      {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

      {isLoading ? (
        <Card variant="shadowed">
          <p>Đang tải cấu trúc khóa học...</p>
        </Card>
      ) : !course ? (
        <Card variant="shadowed">
          <p>Không tìm thấy cấu trúc khóa học.</p>
        </Card>
      ) : (
        <div className="course-structure-stack">
          <Card tone="saffron" variant="shadowed">
            <div className="detail-header">
              <div>
                <span className="ui-badge">Course draft</span>
                <h2>{course.title}</h2>
                <p>{course.description}</p>
              </div>
              <Button onClick={handleGenerateLessonContent} disabled={isGeneratingContent || isSavingPresentation}>
                {hasActiveJob ? "Đang generate nền..." : "Generate nội dung bài học"}
              </Button>
              <Button onClick={handleGenerateLessonAudio} disabled={isGeneratingContent} variant="ghost">
                {hasActiveJob && isAudioJob(activeJob?.jobType) ? "Đang generate audio..." : "Generate audio khóa học"}
              </Button>
              <Button onClick={handleGenerateLessonVideo} disabled={isGeneratingContent} variant="ghost">
                {hasActiveJob && isVideoJob(activeJob?.jobType) ? "Đang generate video..." : "Generate video khóa học"}
              </Button>
            </div>

            <div className="course-card__stats">
              <span>Category: {course.category || "UiUxDesign"}</span>
              <span>Thumbnail: {course.thumbnailUrl ? "Available" : "Missing"}</span>
            </div>

            <div className="inline-edit-card">
              <h3>Thumbnail & category</h3>
              {course.thumbnailUrl ? (
                <img
                  src={course.thumbnailUrl}
                  alt={`Thumbnail khóa học ${course.title}`}
                  style={{ width: "100%", maxWidth: "320px", borderRadius: "20px", border: "2px solid var(--color-midnight-ink)" }}
                />
              ) : (
                <div className="empty-state">Chưa có thumbnail.</div>
              )}
              <FormField id="course-category" label="Danh mục khóa học">
                <select
                  className="ui-input"
                  id="course-category"
                  value={selectedCategory}
                  onChange={(event) => setSelectedCategory(event.target.value)}
                >
                  {COURSE_CATEGORY_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </FormField>
              <div className="quick-actions">
                <Button onClick={handleSaveCategory} disabled={isSavingPresentation}>Lưu category</Button>
              </div>
              <FormField id="course-thumbnail" label="Ảnh thumbnail">
                <input
                  className="ui-input"
                  id="course-thumbnail"
                  type="file"
                  accept="image/png,image/jpeg,image/webp"
                  onChange={(event) => setThumbnailFile(event.target.files?.[0] ?? null)}
                />
              </FormField>
              <div className="quick-actions">
                <Button onClick={handleUploadThumbnail} disabled={isSavingPresentation || !thumbnailFile}>Upload thumbnail</Button>
              </div>
            </div>

            {activeJob ? (
              <div className="generation-progress" aria-label={resolveJobProgressAriaLabel(activeJob.jobType)}>
                <div className="generation-progress__header">
                  <div>
                    <strong>{resolveJobProgressTitle(activeJob.jobType)}</strong>
                    <p>{activeJob.progressMessage || "Đang chuẩn bị job..."}</p>
                  </div>
                  <span>{progressPercent}%</span>
                </div>
                <div className="generation-progress__bar" aria-hidden="true">
                  <div className="generation-progress__fill" style={{ width: `${progressPercent}%` }} />
                </div>
                <div className="course-card__stats">
                  <span>{processedItems}/{totalItems || 0} lesson đã xử lý</span>
                  <span>{successfulItems} thành công</span>
                  <span>{failedItems} lỗi</span>
                  <span>Trạng thái: {resolveJobStatusLabel(activeJob.status)}</span>
                </div>
                {activeJob.errorMessage ? <p className="generation-progress__error">{activeJob.errorMessage}</p> : null}
              </div>
            ) : null}
          </Card>

          <div className="section-stack">
            {course.modules.map((module) => (
              <Card key={module.id} className="section-stack" variant="shadowed">
                <div className="detail-header">
                  <div>
                    <span className="ui-badge">Module {module.orderIndex}</span>
                    <h2>{module.title}</h2>
                  </div>
                  <Button onClick={() => startEditModule(module)} variant="ghost">
                    Sửa module
                  </Button>
                </div>

                {editingModuleId === module.id ? (
                  <div className="inline-edit-card">
                    <FormField id={`module-title-${module.id}`} label="Tiêu đề module">
                      <input
                        className="ui-input"
                        id={`module-title-${module.id}`}
                        value={moduleForm.title}
                        onChange={(event) => setModuleForm((current) => ({ ...current, title: event.target.value }))}
                      />
                    </FormField>
                    <FormField id={`module-description-${module.id}`} label="Mô tả module">
                      <textarea
                        className="ui-input ui-textarea"
                        id={`module-description-${module.id}`}
                        rows="3"
                        value={moduleForm.description}
                        onChange={(event) => setModuleForm((current) => ({ ...current, description: event.target.value }))}
                      />
                    </FormField>
                    <div className="quick-actions">
                      <Button onClick={handleModuleSave}>Lưu module</Button>
                      <Button onClick={() => setEditingModuleId(null)} variant="ghost">Hủy</Button>
                    </div>
                  </div>
                ) : (
                  <p>{module.description}</p>
                )}

                <div className="lesson-stack">
                  {module.lessons.map((lesson) => (
                    <div className="lesson-card" key={lesson.id}>
                      <div className="detail-header">
                        <div>
                          <span className="ui-badge">Lesson {lesson.orderIndex}</span>
                          <h3>{lesson.title}</h3>
                        </div>
                        <div className="detail-actions">
                          <LessonContentStatusBadge status={lesson.contentGenerationStatus} />
                          <LessonContentStatusBadge status={lesson.audioGenerationStatus} />
                          <LessonContentStatusBadge status={lesson.videoGenerationStatus} />
                          {lesson.contentGenerationStatus === "Failed" ? (
                            <Button
                              onClick={() => handleRegenerateLessonContent(lesson.id)}
                              variant="ghost"
                              disabled={isGeneratingContent}
                            >
                              Generate lại lesson lỗi
                            </Button>
                          ) : null}
                          {lesson.contentGenerationStatus === "Completed" || lesson.contentGenerationStatus === "ManuallyEdited" ? (
                            <Button
                              onClick={() => handleGenerateAudioForLesson(lesson.id, lesson.audioGenerationStatus === "Failed")}
                              variant="ghost"
                              disabled={isGeneratingContent}
                            >
                              {lesson.audioGenerationStatus === "Failed" || lesson.audioUrl ? "Generate lại audio" : "Generate audio"}
                            </Button>
                          ) : null}
                          {lesson.audioUrl ? (
                            <Button onClick={() => handleViewLessonAudio(lesson.id)} variant="ghost">
                              Xem audio
                            </Button>
                          ) : null}
                          {(lesson.audioGenerationStatus === "Completed" || lesson.videoUrl) ? (
                            <Button
                              onClick={() => handleGenerateVideoForLesson(lesson.id, lesson.videoGenerationStatus === "Failed" || Boolean(lesson.videoUrl))}
                              variant="ghost"
                              disabled={isGeneratingContent}
                            >
                              {lesson.videoGenerationStatus === "Failed" || lesson.videoUrl ? "Generate lại video" : "Generate video"}
                            </Button>
                          ) : null}
                          {lesson.videoUrl ? (
                            <Button onClick={() => handleViewLessonVideo(lesson.id)} variant="ghost">
                              Xem video
                            </Button>
                          ) : null}
                          <Button onClick={() => handleViewGeneratedContent(lesson.id)} variant="ghost">
                            Xem nội dung AI
                          </Button>
                          {generatedContentByLessonId[lesson.id] ? (
                            <Button onClick={() => handleStartEditingGeneratedContent(lesson.id)} variant="ghost">
                              Chỉnh nội dung AI
                            </Button>
                          ) : null}
                          <Button onClick={() => startEditLesson(lesson)} variant="ghost">
                            Sửa lesson
                          </Button>
                        </div>
                      </div>

                      {editingLessonId === lesson.id ? (
                        <div className="inline-edit-card">
                          <FormField id={`lesson-title-${lesson.id}`} label="Tiêu đề lesson">
                            <input
                              className="ui-input"
                              id={`lesson-title-${lesson.id}`}
                              value={lessonForm.title}
                              onChange={(event) => setLessonForm((current) => ({ ...current, title: event.target.value }))}
                            />
                          </FormField>
                          <FormField id={`lesson-description-${lesson.id}`} label="Mô tả lesson">
                            <textarea
                              className="ui-input ui-textarea"
                              id={`lesson-description-${lesson.id}`}
                              rows="3"
                              value={lessonForm.description}
                              onChange={(event) => setLessonForm((current) => ({ ...current, description: event.target.value }))}
                            />
                          </FormField>
                          <FormField id={`lesson-content-${lesson.id}`} label="Content seed">
                            <textarea
                              className="ui-input ui-textarea"
                              id={`lesson-content-${lesson.id}`}
                              rows="6"
                              value={lessonForm.contentSeed}
                              onChange={(event) => setLessonForm((current) => ({ ...current, contentSeed: event.target.value }))}
                            />
                          </FormField>
                          <div className="quick-actions">
                            <Button onClick={() => handleLessonSave(module.id)}>Lưu lesson</Button>
                            <Button onClick={() => setEditingLessonId(null)} variant="ghost">Hủy</Button>
                          </div>
                        </div>
                      ) : (
                        <>
                          <p>{lesson.description}</p>
                          {lesson.contentGenerationError ? (
                            <p className="lesson-card__error">Lỗi generate: {lesson.contentGenerationError}</p>
                          ) : null}
                          {lesson.audioGenerationError ? (
                            <p className="lesson-card__error">Lỗi audio: {lesson.audioGenerationError}</p>
                          ) : null}
                          {lesson.videoGenerationError ? (
                            <p className="lesson-card__error">Lỗi video: {lesson.videoGenerationError}</p>
                          ) : null}
                          <pre className="text-preview text-preview--compact">{lesson.contentSeed}</pre>
                          {audioByLessonId[lesson.id]?.audioUrl || lesson.audioUrl ? (
                            <div className="audio-preview-card">
                              <strong>Audio lesson</strong>
                              <audio controls preload="none" src={audioByLessonId[lesson.id]?.audioUrl || lesson.audioUrl}>
                                Trình duyệt không hỗ trợ audio preview.
                              </audio>
                            </div>
                          ) : null}
                          {videoByLessonId[lesson.id]?.videoUrl || lesson.videoUrl ? (
                            <div className="video-preview-card">
                              <strong>Video bài học</strong>
                              <video controls preload="metadata" src={videoByLessonId[lesson.id]?.videoUrl || lesson.videoUrl}>
                                Trình duyệt không hỗ trợ video preview.
                              </video>
                            </div>
                          ) : null}
                          {selectedGeneratedLessonId === lesson.id && generatedContentByLessonId[lesson.id] ? (
                            editingGeneratedLessonId === lesson.id ? (
                              <LessonContentEditor
                                form={generatedContentForm}
                                onChange={(field, value) =>
                                  setGeneratedContentForm((current) => ({ ...current, [field]: value }))
                                }
                                onSave={() => handleSaveGeneratedContent(lesson.id)}
                                onCancel={() => setEditingGeneratedLessonId(null)}
                              />
                            ) : (
                              <LessonContentPreview content={generatedContentByLessonId[lesson.id]} />
                            )
                          ) : null}
                        </>
                      )}
                    </div>
                  ))}
                </div>
              </Card>
            ))}
          </div>
        </div>
      )}
    </Section>
  );
}

function isLessonContentJob(jobType) {
  return jobType === "GenerateLessonContent" || jobType === "RegenerateLessonContent";
}

function isAudioJob(jobType) {
  return jobType === "GenerateLessonAudio" || jobType === "RegenerateLessonAudio";
}

function isVideoJob(jobType) {
  return jobType === "GenerateLessonVideo" || jobType === "RegenerateLessonVideo";
}

function isCourseJobType(jobType) {
  return isLessonContentJob(jobType) || isAudioJob(jobType) || isVideoJob(jobType);
}

function isJobActive(status) {
  return status === "Pending" || status === "GeneratingLessonContent" || status === "RegeneratingLessonContent" || status === "GeneratingLessonAudio" || status === "GeneratingLessonVideo";
}

function getProgressPercent(job) {
  if (!job?.totalItems) {
    return 0;
  }

  return Math.min(100, Math.round((job.processedItems / job.totalItems) * 100));
}

function resolveJobStatusLabel(status) {
  switch (status) {
    case "Pending":
      return "Đang chờ";
    case "GeneratingLessonContent":
      return "Đang generate";
    case "RegeneratingLessonContent":
      return "Đang generate lại lesson lỗi";
    case "GeneratingLessonAudio":
      return "Đang generate audio";
    case "GeneratingLessonVideo":
      return "Đang generate video";
    case "Completed":
      return "Hoàn tất";
    case "CompletedWithWarnings":
      return "Hoàn tất nhưng còn cảnh báo";
    case "Failed":
      return "Thất bại";
    default:
      return status || "Không xác định";
  }
}

function resolveJobProgressTitle(jobType) {
  if (isAudioJob(jobType)) {
    return "Tiến trình generate audio bài học";
  }

  if (isVideoJob(jobType)) {
    return "Tiến trình generate video bài học";
  }

  return "Tiến trình generate nội dung bài học";
}

function resolveJobProgressAriaLabel(jobType) {
  if (isAudioJob(jobType)) {
    return "Tiến trình generate audio bài học";
  }

  if (isVideoJob(jobType)) {
    return "Tiến trình generate video bài học";
  }

  return "Tiến trình generate nội dung bài học";
}
