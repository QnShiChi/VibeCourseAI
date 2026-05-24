import { useEffect, useRef, useState } from "react";
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

  // States for Centralized Lesson Panel & Bulk Actions
  const [selectedLessonId, setSelectedLessonId] = useState("");
  const [checkedLessonIds, setCheckedLessonIds] = useState([]);
  const [isProcessingBulk, setIsProcessingBulk] = useState(false);
  const [bulkProgress, setBulkProgress] = useState("");
  const [activeTab, setActiveTab] = useState("actions");
  const [expandedModuleId, setExpandedModuleId] = useState(null);
  const [isPanelFocused, setIsPanelFocused] = useState(false);
  const panelFocusTimerRef = useRef(null);

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

  useEffect(() => {
    return () => {
      if (panelFocusTimerRef.current) {
        window.clearTimeout(panelFocusTimerRef.current);
      }
    };
  }, []);

  useEffect(() => {
    if (!selectedLessonId) {
      return;
    }

    focusCentralizedPanel();
  }, [selectedLessonId]);

  useEffect(() => {
    if (!isPanelFocused) {
      return undefined;
    }

    if (panelFocusTimerRef.current) {
      window.clearTimeout(panelFocusTimerRef.current);
    }

    panelFocusTimerRef.current = window.setTimeout(() => {
      setIsPanelFocused(false);
      panelFocusTimerRef.current = null;
    }, 1200);

    return () => {
      if (panelFocusTimerRef.current) {
        window.clearTimeout(panelFocusTimerRef.current);
      }
    };
  }, [isPanelFocused]);

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

  function getSelectedLesson() {
    if (!course || !selectedLessonId) return null;
    for (const module of course.modules) {
      const lesson = module.lessons.find((l) => l.id === selectedLessonId);
      if (lesson) {
        return {
          ...lesson,
          moduleId: module.id,
          moduleOrder: module.orderIndex,
          moduleTitle: module.title,
          moduleDescription: module.description
        };
      }
    }
    return null;
  }

  async function handleSelectLesson(lessonId) {
    setSelectedLessonId(lessonId);
    setExpandedModuleId(null);
    if (!lessonId) return;

    // Prefetch content, audio, or video if not loaded
    if (!generatedContentByLessonId[lessonId]) {
      try {
        const content = await getLessonGeneratedContent(lessonId);
        setGeneratedContentByLessonId((current) => ({ ...current, [lessonId]: content }));
      } catch { }
    }

    const lessonObj = course?.modules
      ?.flatMap((m) => m.lessons)
      ?.find((l) => l.id === lessonId);

    if (lessonObj?.audioUrl && !audioByLessonId[lessonId]) {
      try {
        const audio = await getLessonAudio(lessonId);
        setAudioByLessonId((current) => ({ ...current, [lessonId]: audio }));
      } catch { }
    }

    if (lessonObj?.videoUrl && !videoByLessonId[lessonId]) {
      try {
        const video = await getLessonVideo(lessonId);
        setVideoByLessonId((current) => ({ ...current, [lessonId]: video }));
      } catch { }
    }
  }

  function focusCentralizedPanel() {
    const panelElement = document.getElementById("centralized-lesson-action-panel");
    if (!panelElement) {
      return;
    }

    const headerOffset = document.querySelector(".app-header")?.getBoundingClientRect().height ?? 0;
    const panelTop = window.scrollY + panelElement.getBoundingClientRect().top - headerOffset - 16;

    window.scrollTo({
      top: Math.max(panelTop, 0),
      behavior: "smooth"
    });
    panelElement.focus({ preventScroll: true });
  }

  function handleControlLesson(lessonId) {
    if (selectedLessonId === lessonId) {
      focusCentralizedPanel();
    }
    setExpandedModuleId(null);
    handleSelectLesson(lessonId);
    setIsPanelFocused(true);
  }

  function toggleModuleDetails(moduleId) {
    setExpandedModuleId((current) => (current === moduleId ? null : moduleId));
  }

  function toggleCheckLesson(lessonId) {
    setCheckedLessonIds((current) =>
      current.includes(lessonId)
        ? current.filter((id) => id !== lessonId)
        : [...current, lessonId]
    );
  }

  function toggleCheckAll() {
    if (!course) return;
    const allLessonIds = course.modules.flatMap((m) => m.lessons.map((l) => l.id));
    if (checkedLessonIds.length === allLessonIds.length) {
      setCheckedLessonIds([]);
    } else {
      setCheckedLessonIds(allLessonIds);
    }
  }

  async function handleBulkGenerateAudio() {
    if (checkedLessonIds.length === 0) return;
    setMessage("");
    setErrorMessage("");
    setIsProcessingBulk(true);
    let processedCount = 0;
    const total = checkedLessonIds.length;

    for (const lessonId of checkedLessonIds) {
      processedCount++;
      setBulkProgress(`Đang tạo Audio cho bài học ${processedCount}/${total}...`);
      try {
        const lessonObj = course?.modules?.flatMap((m) => m.lessons)?.find((l) => l.id === lessonId);
        const regenerate = lessonObj?.audioGenerationStatus === "Failed" || Boolean(lessonObj?.audioUrl);
        if (regenerate) {
          await regenerateLessonAudio(courseId, lessonId);
        } else {
          await generateLessonAudio(courseId, lessonId);
        }
      } catch (err) {
        console.error("Lỗi generate audio bài học: ", lessonId, err);
      }
    }

    setIsProcessingBulk(false);
    setBulkProgress("");
    setCheckedLessonIds([]);
    setMessage(`Đã kích hoạt tiến trình tạo Audio cho ${total} bài học đã chọn.`);
    await loadCourse();
  }

  async function handleBulkGenerateVideo() {
    if (checkedLessonIds.length === 0) return;
    setMessage("");
    setErrorMessage("");
    setIsProcessingBulk(true);
    let processedCount = 0;
    const total = checkedLessonIds.length;

    for (const lessonId of checkedLessonIds) {
      processedCount++;
      setBulkProgress(`Đang tạo Video cho bài học ${processedCount}/${total}...`);
      try {
        const lessonObj = course?.modules?.flatMap((m) => m.lessons)?.find((l) => l.id === lessonId);
        const regenerate = lessonObj?.videoGenerationStatus === "Failed" || Boolean(lessonObj?.videoUrl);
        if (regenerate) {
          await regenerateLessonVideo(courseId, lessonId);
        } else {
          await generateLessonVideo(courseId, lessonId);
        }
      } catch (err) {
        console.error("Lỗi generate video bài học: ", lessonId, err);
      }
    }

    setIsProcessingBulk(false);
    setBulkProgress("");
    setCheckedLessonIds([]);
    setMessage(`Đã kích hoạt tiến trình tạo Video cho ${total} bài học đã chọn.`);
    await loadCourse();
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
  const selectedLesson = getSelectedLesson();

  return (
    <Section className="section-stack course-structure-workspace">
      <PageHeader
        eyebrow="Admin"
        title="Cấu trúc khóa học"
        description="Rà soát cấu trúc khóa học, chỉnh sửa module và lesson, đồng thời theo dõi tiến trình generate nội dung tại một nơi."
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
          <Card className="course-structure-hero-card" tone="saffron" variant="shadowed">
            <div className="course-structure-hero">
              <div className="course-structure-hero__header">
                <div className="course-structure-hero__copy">
                  <span className="ui-badge">Course draft</span>
                  <h2>{course.title}</h2>
                  <p>{course.description}</p>
                </div>
                <div className="course-structure-hero__actions">
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
              </div>
            </div>

            <div className="course-structure-summary">
              <span className="course-structure-summary__pill">
                Category: {course.category || "UiUxDesign"}
              </span>
              <span className="course-structure-summary__pill">
                Thumbnail: {course.thumbnailUrl ? "Available" : "Missing"}
              </span>
            </div>

            <div className="inline-edit-card course-presentation-panel">
              <div className="course-presentation-panel__intro">
                <div>
                  <span className="ui-badge">Presentation</span>
                  <h3>Thumbnail & category</h3>
                  <p>Tinh chỉnh phần trình bày của khóa học trước khi publish cho learner.</p>
                </div>
              </div>

              <div className="course-presentation-panel__grid">
                <div className="course-thumbnail-preview">
                  <span className="course-thumbnail-preview__label">Course thumbnail</span>
                  {course.thumbnailUrl ? (
                    <img
                      className="course-thumbnail-preview__image"
                      src={course.thumbnailUrl}
                      alt={`Thumbnail khóa học ${course.title}`}
                    />
                  ) : (
                    <div className="course-thumbnail-preview__empty">
                      <strong>Chưa có thumbnail</strong>
                      <span>Upload ảnh 16:9 để thẻ khóa học và trang learner đồng nhất hơn.</span>
                    </div>
                  )}
                </div>

                <div className="course-presentation-form">
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
                  <div className="quick-actions course-presentation-form__actions">
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
                  <div className="quick-actions course-presentation-form__actions">
                    <Button onClick={handleUploadThumbnail} disabled={isSavingPresentation || !thumbnailFile}>Upload thumbnail</Button>
                  </div>
                </div>
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

          {/* Bảng điều khiển tác vụ hàng loạt */}
          {checkedLessonIds.length > 0 && (
            <Card className="bulk-action-card" variant="shadowed">
              <div className="bulk-action-card__header">
                <h4>
                  <span className="ui-badge" style={{ backgroundColor: "#84cc16", borderColor: "#76b813", color: "#fff" }}>
                    {checkedLessonIds.length} bài học đã chọn
                  </span>{" "}
                  Thao tác hàng loạt (Bulk Actions)
                </h4>
                <div className="quick-actions">
                  <Button
                    onClick={handleBulkGenerateAudio}
                    disabled={isProcessingBulk || isGeneratingContent}
                    style={{ backgroundColor: "#84cc16", borderColor: "#76b813", color: "#fff" }}
                  >
                    Generate Audio hàng loạt
                  </Button>
                  <Button
                    onClick={handleBulkGenerateVideo}
                    disabled={isProcessingBulk || isGeneratingContent}
                    style={{ backgroundColor: "#0284c7", borderColor: "#0275b0", color: "#fff" }}
                  >
                    Generate Video hàng loạt
                  </Button>
                  <Button
                    onClick={() => setCheckedLessonIds([])}
                    variant="ghost"
                    disabled={isProcessingBulk}
                  >
                    Hủy chọn
                  </Button>
                </div>
              </div>

              {isProcessingBulk && (
                <div className="bulk-action-card__progress-container">
                  <div style={{ display: "flex", alignItems: "center" }}>
                    <span className="bulk-action-card__spinner" />
                    <strong>{bulkProgress}</strong>
                  </div>
                </div>
              )}
            </Card>
          )}

          {/* Bảng điều khiển tác vụ Lesson tập trung */}
          <Card
            id="centralized-lesson-action-panel"
            tabIndex={-1}
            className={`centralized-panel ${isPanelFocused ? "centralized-panel--focused" : ""}`.trim()}
            variant="shadowed"
          >
            <div className="centralized-panel__title-bar">
              <h2>
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ marginRight: "8px" }}><path d="M12 20h9" /><path d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4Z" /></svg>
                Bảng điều khiển tác vụ Lesson tập trung
              </h2>

              <div className="centralized-panel__status-container" style={{ minWidth: "280px" }}>
                <select
                  className="ui-input"
                  aria-label="Chọn bài học để điều khiển"
                  value={selectedLessonId}
                  onChange={(e) => handleSelectLesson(e.target.value)}
                  style={{ fontWeight: "700", borderColor: "#737373" }}
                >
                  <option value="">-- Chọn bài học để điều khiển --</option>
                  {course.modules.map((module) => (
                    <optgroup key={module.id} label={`Module ${module.orderIndex}: ${module.title}`}>
                      {module.lessons.map((lesson) => (
                        <option key={lesson.id} value={lesson.id}>
                          Bài {lesson.orderIndex}: {lesson.title}
                        </option>
                      ))}
                    </optgroup>
                  ))}
                </select>
              </div>
            </div>

            {selectedLesson ? (
              <>
                <div className="centralized-panel__active-lesson-banner">
                  <div>
                    <h3>
                      Bài {selectedLesson.orderIndex}: {selectedLesson.title}
                    </h3>
                    <p style={{ margin: "4px 0 0" }}>Thuộc Module {selectedLesson.moduleOrder}</p>
                  </div>
                  <div className="centralized-panel__status-container">
                    <LessonContentStatusBadge type="content" status={selectedLesson.contentGenerationStatus} />
                    <LessonContentStatusBadge type="audio" status={selectedLesson.audioGenerationStatus} />
                    <LessonContentStatusBadge type="video" status={selectedLesson.videoGenerationStatus} />
                  </div>
                </div>

                <div className="centralized-panel__tabs">
                  <button
                    className={`centralized-panel__tab-btn ${activeTab === "actions" ? "centralized-panel__tab-btn--active" : ""}`}
                    onClick={() => setActiveTab("actions")}
                  >
                    Tác vụ & Trạng thái
                  </button>
                  <button
                    className={`centralized-panel__tab-btn ${activeTab === "ai-content" ? "centralized-panel__tab-btn--active" : ""}`}
                    onClick={() => setActiveTab("ai-content")}
                  >
                    Nội dung AI
                  </button>
                  <button
                    className={`centralized-panel__tab-btn ${activeTab === "edit-details" ? "centralized-panel__tab-btn--active" : ""}`}
                    onClick={() => setActiveTab("edit-details")}
                  >
                    Sửa thông tin
                  </button>
                </div>

                <div className="centralized-panel__content-box">
                  {activeTab === "actions" && (
                    <div className="section-stack">
                      <div className="centralized-panel__actions-grid">
                        {selectedLesson.contentGenerationStatus === "Failed" && (
                          <Button
                            onClick={() => handleRegenerateLessonContent(selectedLesson.id)}
                            disabled={isGeneratingContent}
                          >
                            Generate lại lesson lỗi
                          </Button>
                        )}
                        {(selectedLesson.contentGenerationStatus === "Completed" || selectedLesson.contentGenerationStatus === "ManuallyEdited") && (
                          <Button
                            onClick={() => handleGenerateAudioForLesson(selectedLesson.id, selectedLesson.audioGenerationStatus === "Failed")}
                            disabled={isGeneratingContent}
                          >
                            {selectedLesson.audioGenerationStatus === "Failed" || selectedLesson.audioUrl ? "Generate lại audio" : "Generate audio"}
                          </Button>
                        )}
                        {(selectedLesson.audioGenerationStatus === "Completed" || selectedLesson.videoUrl) && (
                          <Button
                            onClick={() => handleGenerateVideoForLesson(selectedLesson.id, selectedLesson.videoGenerationStatus === "Failed" || Boolean(selectedLesson.videoUrl))}
                            disabled={isGeneratingContent}
                          >
                            {selectedLesson.videoGenerationStatus === "Failed" || selectedLesson.videoUrl ? "Generate lại video" : "Generate video"}
                          </Button>
                        )}
                      </div>

                      {/* Hiển thị lỗi (nếu có) */}
                      {selectedLesson.contentGenerationError && (
                        <p className="lesson-card__error">Lỗi generate: {selectedLesson.contentGenerationError}</p>
                      )}
                      {selectedLesson.audioGenerationError && (
                        <p className="lesson-card__error">Lỗi audio: {selectedLesson.audioGenerationError}</p>
                      )}
                      {selectedLesson.videoGenerationError && (
                        <p className="lesson-card__error">Lỗi video: {selectedLesson.videoGenerationError}</p>
                      )}

                      {/* Preview audio & video */}
                      <div className="card-grid" style={{ gridTemplateColumns: "repeat(auto-fit, minmax(280px, 1fr))", gap: "16px", marginTop: "16px" }}>
                        {(audioByLessonId[selectedLesson.id]?.audioUrl || selectedLesson.audioUrl) ? (
                          <div className="audio-preview-card">
                            <strong>Audio lesson</strong>
                            <audio controls preload="none" src={audioByLessonId[selectedLesson.id]?.audioUrl || selectedLesson.audioUrl}>
                              Trình duyệt không hỗ trợ audio preview.
                            </audio>
                          </div>
                        ) : (
                          <div className="audio-preview-card" style={{ opacity: 0.5, borderStyle: "dashed", placeItems: "center", minHeight: "90px" }}>
                            <span style={{ fontSize: "var(--text-body-sm)", fontWeight: "bold" }}>Chưa có Audio</span>
                          </div>
                        )}

                        {(videoByLessonId[selectedLesson.id]?.videoUrl || selectedLesson.videoUrl) ? (
                          <div className="video-preview-card">
                            <strong>Video bài học</strong>
                            <video controls preload="metadata" src={videoByLessonId[selectedLesson.id]?.videoUrl || selectedLesson.videoUrl}>
                              Trình duyệt không hỗ trợ video preview.
                            </video>
                          </div>
                        ) : (
                          <div className="video-preview-card" style={{ opacity: 0.5, borderStyle: "dashed", placeItems: "center", minHeight: "90px" }}>
                            <span style={{ fontSize: "var(--text-body-sm)", fontWeight: "bold" }}>Chưa có Video</span>
                          </div>
                        )}
                      </div>
                    </div>
                  )}

                  {activeTab === "ai-content" && (
                    <div>
                      {generatedContentByLessonId[selectedLesson.id] ? (
                        editingGeneratedLessonId === selectedLesson.id ? (
                          <LessonContentEditor
                            form={generatedContentForm}
                            onChange={(field, value) =>
                              setGeneratedContentForm((current) => ({ ...current, [field]: value }))
                            }
                            onSave={() => handleSaveGeneratedContent(selectedLesson.id)}
                            onCancel={() => setEditingGeneratedLessonId(null)}
                          />
                        ) : (
                          <div className="section-stack">
                            <div className="quick-actions" style={{ marginBottom: "12px" }}>
                              <Button onClick={() => handleStartEditingGeneratedContent(selectedLesson.id)}>
                                Chỉnh sửa nội dung AI
                              </Button>
                            </div>
                            <LessonContentPreview content={generatedContentByLessonId[selectedLesson.id]} />
                          </div>
                        )
                      ) : (
                        <div style={{ textAlign: "center", padding: "24px", opacity: 0.7 }}>
                          <p>Nội dung AI của bài học này chưa được tải hoặc chưa được tạo.</p>
                          <Button onClick={() => handleViewGeneratedContent(selectedLesson.id)} variant="ghost">
                            Tải nội dung AI
                          </Button>
                        </div>
                      )}
                    </div>
                  )}

                  {activeTab === "edit-details" && (
                    <div>
                      <div className="section-stack">
                        {editingModuleId === selectedLesson.moduleId ? (
                          <div className="inline-edit-card" style={{ background: "transparent", border: "none", padding: 0 }}>
                            <FormField id={`module-title-${selectedLesson.moduleId}`} label="Tiêu đề module">
                              <input
                                className="ui-input"
                                id={`module-title-${selectedLesson.moduleId}`}
                                value={moduleForm.title}
                                onChange={(event) => setModuleForm((current) => ({ ...current, title: event.target.value }))}
                              />
                            </FormField>
                            <FormField id={`module-description-${selectedLesson.moduleId}`} label="Mô tả module">
                              <textarea
                                className="ui-input ui-textarea"
                                id={`module-description-${selectedLesson.moduleId}`}
                                rows="3"
                                value={moduleForm.description}
                                onChange={(event) => setModuleForm((current) => ({ ...current, description: event.target.value }))}
                              />
                            </FormField>
                            <div className="quick-actions" style={{ marginTop: "12px" }}>
                              <Button onClick={handleModuleSave}>Lưu module</Button>
                              <Button onClick={() => setEditingModuleId(null)} variant="ghost">Hủy</Button>
                            </div>
                          </div>
                        ) : (
                          <div className="section-stack">
                            <div className="quick-actions" style={{ marginBottom: "12px" }}>
                              <Button
                                onClick={() => startEditModule({
                                  id: selectedLesson.moduleId,
                                  title: selectedLesson.moduleTitle,
                                  description: selectedLesson.moduleDescription
                                })}
                              >
                                Chỉnh sửa thông tin module
                              </Button>
                            </div>
                            <FormField label="Tiêu đề module">
                              <p>{selectedLesson.moduleTitle || `Module ${selectedLesson.moduleOrder}`}</p>
                            </FormField>
                            <FormField label="Mô tả module">
                              <p>{selectedLesson.moduleDescription || "Chưa có mô tả."}</p>
                            </FormField>
                          </div>
                        )}

                        {editingLessonId === selectedLesson.id ? (
                          <div className="inline-edit-card" style={{ background: "transparent", border: "none", padding: 0 }}>
                            <FormField id={`lesson-title-${selectedLesson.id}`} label="Tiêu đề lesson">
                              <input
                                className="ui-input"
                                id={`lesson-title-${selectedLesson.id}`}
                                value={lessonForm.title}
                                onChange={(event) => setLessonForm((current) => ({ ...current, title: event.target.value }))}
                              />
                            </FormField>
                            <FormField id={`lesson-description-${selectedLesson.id}`} label="Mô tả lesson">
                              <textarea
                                className="ui-input ui-textarea"
                                id={`lesson-description-${selectedLesson.id}`}
                                rows="3"
                                value={lessonForm.description}
                                onChange={(event) => setLessonForm((current) => ({ ...current, description: event.target.value }))}
                              />
                            </FormField>
                            <FormField id={`lesson-content-${selectedLesson.id}`} label="Content seed">
                              <textarea
                                className="ui-input ui-textarea"
                                id={`lesson-content-${selectedLesson.id}`}
                                rows="6"
                                value={lessonForm.contentSeed}
                                onChange={(event) => setLessonForm((current) => ({ ...current, contentSeed: event.target.value }))}
                              />
                            </FormField>
                            <div className="quick-actions" style={{ marginTop: "12px" }}>
                              <Button onClick={() => handleLessonSave(selectedLesson.moduleId)}>Lưu bài học</Button>
                              <Button onClick={() => setEditingLessonId(null)} variant="ghost">Hủy</Button>
                            </div>
                          </div>
                        ) : (
                          <div className="section-stack">
                            <div className="quick-actions" style={{ marginBottom: "12px" }}>
                              <Button onClick={() => startEditLesson(selectedLesson)}>
                                Chỉnh sửa thông tin bài học
                              </Button>
                            </div>
                            <FormField label="Mô tả bài học">
                              <p>{selectedLesson.description || "Chưa có mô tả."}</p>
                            </FormField>
                            <FormField label="Content seed">
                              <pre className="text-preview text-preview--compact">{selectedLesson.contentSeed || "Trống."}</pre>
                            </FormField>
                          </div>
                        )}
                      </div>
                    </div>
                  )}
                </div>
              </>
            ) : (
              <div style={{ textAlign: "center", padding: "40px 20px", color: "rgba(0, 0, 0, 0.5)" }}>
                <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" style={{ marginBottom: "12px", opacity: 0.5 }}><circle cx="12" cy="12" r="10" /><path d="m15 9-6 6" /><path d="m9 9 6 6" /></svg>
                <p style={{ margin: 0, fontWeight: "600" }}>Chưa có bài học nào được chọn điều khiển</p>
                <p style={{ margin: "4px 0 0", fontSize: "var(--text-body-sm)" }}>Vui lòng chọn từ danh sách dropdown ở trên hoặc bấm nút "Điều khiển" ở thẻ bài học bên dưới.</p>
              </div>
            )}
          </Card>

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
