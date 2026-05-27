import { useEffect, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { getCourseLearnPayload } from "../api/courseService";
import { useAuth } from "../auth/AuthContext";
import LessonComments from "../components/comments/LessonComments";
import LessonVoiceTutorPanel from "../components/course/LessonVoiceTutorPanel";
import Card from "../components/ui/Card";
import Section from "../components/ui/Section";
import { useLessonVoiceTutor } from "../hooks/useLessonVoiceTutor";
import { useTheme } from "../theme/ThemeContext";

function sortByOrder(items) {
  return [...items].sort((left, right) => left.orderIndex - right.orderIndex);
}

function flattenLessons(modules) {
  return sortByOrder(modules).flatMap((module) =>
    sortByOrder(module.lessons).map((lesson) => ({
      ...lesson,
      moduleId: module.moduleId,
      moduleTitle: module.moduleTitle,
      moduleOrderIndex: module.orderIndex
    }))
  );
}

function buildExpandedModuleState(modules, selectedLessonId) {
  const defaults = {};
  const selectedContainer = modules.find((module) =>
    module.lessons.some((lesson) => lesson.lessonId === selectedLessonId)
  );

  modules.forEach((module, index) => {
    defaults[module.moduleId] = selectedContainer
      ? module.moduleId === selectedContainer.moduleId
      : index === 0;
  });
  return defaults;
}

function buildSingleExpandedState(modules, moduleId, shouldExpand = true) {
  const next = {};
  modules.forEach((module) => {
    next[module.moduleId] = shouldExpand && module.moduleId === moduleId;
  });
  return next;
}

function getLessonStage(lesson, selectedLessonId, completedLessonIds) {
  if (completedLessonIds.includes(lesson.lessonId)) {
    return "complete";
  }

  if (lesson.lessonId === selectedLessonId) {
    return "active";
  }

  return "upcoming";
}

function getLessonStageLabel(stage) {
  if (stage === "complete") {
    return "Da hoc";
  }

  if (stage === "active") {
    return "Dang hoc";
  }

  return "Sap hoc";
}

function getLessonActionLabel(stage) {
  if (stage === "complete") {
    return "✓";
  }

  if (stage === "active") {
    return "▶";
  }

  return "•";
}

function getVideoStatusLabel(selectedLesson) {
  if (selectedLesson.videoUrl) {
    return "San sang hoc ngay";
  }

  if (selectedLesson.videoGenerationStatus === "Failed") {
    return "Video dang gap loi";
  }

  return "Dang chuan bi video";
}

export default function CourseLearnPage() {
  const { courseId = "" } = useParams();
  const { user } = useAuth();
  const { theme } = useTheme();
  const [course, setCourse] = useState(null);
  const [selectedLessonId, setSelectedLessonId] = useState(null);
  const [expandedModules, setExpandedModules] = useState({});
  const [errorMessage, setErrorMessage] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const videoRef = useRef(null);
  const pausedTimeRef = useRef(0);
  
  const [completedLessonIds, setCompletedLessonIds] = useState(() => {
    try {
      const saved = localStorage.getItem(`course_progress_${courseId}`);
      return saved ? JSON.parse(saved) : [];
    } catch {
      return [];
    }
  });

  useEffect(() => {
    if (courseId) {
      localStorage.setItem(`course_progress_${courseId}`, JSON.stringify(completedLessonIds));
    }
  }, [completedLessonIds, courseId]);

  useEffect(() => {
    if (courseId) {
      loadCourse();
    }
  }, [courseId]);

  async function loadCourse() {
    setIsLoading(true);
    setErrorMessage("");
    try {
      const data = await getCourseLearnPayload(courseId);
      setCourse(data);
      setSelectedLessonId(data.selectedLessonId);
      setExpandedModules(buildExpandedModuleState(sortByOrder(data.modules ?? []), data.selectedLessonId));
    } catch {
      setErrorMessage("Không thể tải trang học của khóa học này.");
    } finally {
      setIsLoading(false);
    }
  }

  function handleToggleModule(moduleId) {
    setExpandedModules((current) =>
      buildSingleExpandedState(modules, moduleId, !current[moduleId])
    );
  }

  function handleSelectLesson(moduleId, lessonId) {
    setSelectedLessonId(lessonId);
    setExpandedModules(buildSingleExpandedState(modules, moduleId, true));
  }

  function handleNavigateLesson(targetLesson) {
    if (!targetLesson) {
      return;
    }

    setSelectedLessonId(targetLesson.lessonId);
    setExpandedModules(buildSingleExpandedState(modules, targetLesson.moduleId, true));
  }

  function handleTimeUpdate(e) {
    const video = e.target;
    if (!video.duration) return;
    
    if (video.currentTime >= Math.max(0, video.duration - 5)) {
      if (selectedLessonId && !completedLessonIds.includes(selectedLessonId)) {
        setCompletedLessonIds((prev) => [...prev, selectedLessonId]);
      }
    }
  }

  const modules = sortByOrder(course?.modules ?? []);
  const flatLessons = flattenLessons(modules);
  const selectedLesson =
    flatLessons.find((lesson) => lesson.lessonId === selectedLessonId) ?? course?.selectedLesson ?? null;
  const selectedModule =
    modules.find((module) => module.moduleId === selectedLesson?.moduleId) ??
    modules.find((module) => module.lessons.some((lesson) => lesson.lessonId === selectedLessonId)) ??
    null;
  const currentLessonIndex = flatLessons.findIndex((lesson) => lesson.lessonId === selectedLessonId);
  const totalLessons = flatLessons.length;
  
  const validCompletedLessons = completedLessonIds.filter(id => flatLessons.some(l => l.lessonId === id));
  const progressPercent = totalLessons ? Math.round((validCompletedLessons.length / totalLessons) * 100) : 0;
  const completedLessons = validCompletedLessons.length;
  const previousLesson = currentLessonIndex > 0 ? flatLessons[currentLessonIndex - 1] : null;
  const nextLesson =
    currentLessonIndex >= 0 && currentLessonIndex < totalLessons - 1 ? flatLessons[currentLessonIndex + 1] : null;
  const isAdmin = user?.role === "Admin";
  const tutor = useLessonVoiceTutor({
    lessonId: selectedLesson?.lessonId ?? "",
    enabled: Boolean(selectedLesson?.videoUrl),
    onPauseVideo(playbackTimeSeconds) {
      pausedTimeRef.current = playbackTimeSeconds;
      videoRef.current?.pause();
    },
    onResumeVideo() {
      if (videoRef.current) {
        videoRef.current.currentTime = pausedTimeRef.current;
        const playPromise = videoRef.current.play?.();
        if (playPromise?.catch) {
          playPromise.catch(() => {});
        }
      }
    }
  });

  return (
    <div className="learn-workspace" data-testid="course-learn-shell" data-theme={theme}>
      <Section className="section-stack">
        {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

        {isLoading ? (
          <Card variant="shadowed">
            <p>Đang tải nội dung khóa học...</p>
          </Card>
        ) : !course || !selectedLesson ? (
          <Card variant="shadowed">
            <p>Khóa học này chưa có lesson khả dụng để học.</p>
          </Card>
        ) : (
          <div className="learn-shell">
            <div className="learn-topbar">
              <Link className="learn-topbar__back" to="/courses">
                <span aria-hidden="true">←</span>
                <span>Quay lại khóa học</span>
              </Link>
              <div className="learn-topbar__brand">
                <span className="learn-topbar__brand-mark">VibeCourseAI</span>
                <span className="learn-topbar__brand-meta">Trang học tập</span>
              </div>
            </div>

            <div className="learn-layout">
              <div className="learn-layout__main">
                <article className="learn-stage-card learn-stage-card--hero">
                  <div className="learn-stage-card__hero-copy">
                    <section className="learn-hero">
                      <p className="learn-hero__eyebrow">Khóa học AI nâng cao</p>
                      <h1>{course.courseTitle}</h1>
                    </section>

                    <div className="learn-stage-card__meta-row">
                      <span className="learn-stat-pill">Giảng viên: Dr. Vibe</span>
                      <span className="learn-stat-pill">{totalLessons} bài học</span>
                      <span className="learn-stat-pill">4.9 ★</span>
                    </div>
                  </div>

                  <div className="learn-stage-card__media-panel">
                    <div className="learn-stage-card__media">
                      <span className="learn-stage-card__badge">
                        {selectedModule
                          ? `Module ${selectedModule.orderIndex} • Bài ${selectedLesson.orderIndex}`
                          : `Bài ${currentLessonIndex + 1}`}
                      </span>
                      {selectedLesson.videoUrl ? (
                        <video
                          ref={videoRef}
                          controls
                          preload="metadata"
                          src={selectedLesson.videoUrl}
                          onTimeUpdate={handleTimeUpdate}>
                          Trình duyệt của bạn không hỗ trợ phát video.
                        </video>
                      ) : (
                        <div className="learn-stage-card__placeholder">
                          <strong>{selectedLesson.lessonTitle}</strong>
                          <span>
                            {selectedLesson.videoGenerationStatus === "Failed"
                              ? "Video lesson đang lỗi, vui lòng thử lại sau."
                              : "Bài học đang được chuẩn bị video."}
                          </span>
                        </div>
                      )}
                    </div>
                    <div className="learn-stage-card__video-meta">
                      <span>{getVideoStatusLabel(selectedLesson)}</span>
                      <span>{selectedModule ? selectedModule.moduleTitle : "Dang cap nhat module"}</span>
                      <span>{completedLessons}/{totalLessons} bài học</span>
                    </div>
                    {selectedLesson.videoUrl ? (
                      <LessonVoiceTutorPanel
                        state={tutor.state}
                        transcriptText={tutor.transcriptText}
                        answerText={tutor.answerText}
                        errorMessage={tutor.errorMessage}
                        onStartRecording={() => tutor.startRecording(videoRef.current?.currentTime ?? 0)}
                        onStopRecording={tutor.stopRecording}
                        onFollowUp={tutor.requestFollowUp}
                        onResume={tutor.resumeLearning}
                      />
                    ) : null}
                  </div>

                  <div className="learn-stage-card__summary">
                    <p className="learn-stage-card__summary-label">Đang học</p>
                    <h2>{selectedLesson.lessonTitle}</h2>
                    <p>{selectedLesson.description || course.courseDescription}</p>
                  </div>
                </article>

                <Card className="learn-reading-card" variant="shadowed">
                  <div className="learn-section-heading">
                    <div>
                      <p>Nội dung bài học</p>
                      <h2>{selectedLesson.lessonTitle}</h2>
                    </div>
                    <span className="learn-section-heading__meta">
                      {selectedModule ? `Module ${selectedModule.orderIndex}` : "Lesson hiện tại"}
                    </span>
                  </div>
                  <pre className="text-preview learn-content-preview">{selectedLesson.contentSeed}</pre>
                </Card>

                <Card className="learn-comments-card" variant="shadowed">
                  <LessonComments isAdmin={isAdmin} lessonId={selectedLesson.lessonId} />
                </Card>

                <div className="learn-footer-nav">
                  <button disabled={!previousLesson} onClick={() => handleNavigateLesson(previousLesson)} type="button">
                    ← Bài trước
                  </button>
                  <p>
                    <span>Đang học:</span> {selectedLesson.lessonTitle}
                  </p>
                  <button
                    aria-label="Tiếp tục bài học"
                    disabled={!nextLesson}
                    onClick={() => handleNavigateLesson(nextLesson)}
                    type="button"
                  >
                    Tiếp tục học →
                  </button>
                </div>
              </div>

              <aside className="learn-sidebar-panel">
                <div className="learn-sidebar-panel__inner">
                  <div className="learn-sidebar-panel__header">
                    <div className="learn-sidebar-panel__eyebrow">Lộ trình học</div>
                    <h2>Nội dung khóa học</h2>
                    <p>
                      {selectedModule ? `Đang ở ${selectedModule.moduleTitle}` : "Theo dõi toàn bộ lesson của khóa học."}
                    </p>
                    <div className="learn-progress">
                      <div aria-hidden="true" className="learn-progress__track">
                        <span className="learn-progress__value" style={{ width: `${progressPercent}%` }} />
                      </div>
                      <p>Tiến độ: {progressPercent}%</p>
                    </div>
                  </div>

                  <div className="learn-sidebar-panel__modules">
                    {modules.map((module) => {
                      const isExpanded = Boolean(expandedModules[module.moduleId]);
                      return (
                        <div className="learn-module" key={module.moduleId}>
                          <button
                            aria-label={`${module.orderIndex}. ${module.moduleTitle}`}
                            aria-expanded={isExpanded}
                            className={`learn-module__header${isExpanded ? " learn-module__header--expanded" : ""}`}
                            onClick={() => handleToggleModule(module.moduleId)}
                            type="button"
                          >
                            <div className="learn-module__header-copy">
                              <span className="learn-module__index">{module.orderIndex}</span>
                              <div>
                                <p>Module {String(module.orderIndex).padStart(2, "0")}</p>
                                <strong>{module.moduleTitle}</strong>
                                <span>{module.lessons.length} bài học</span>
                              </div>
                            </div>
                            <span>{isExpanded ? "⌃" : "⌄"}</span>
                          </button>

                          {isExpanded ? (
                            <div className="learn-module__lessons">
                              {sortByOrder(module.lessons).map((lesson) => {
                                const stage = getLessonStage(lesson, selectedLessonId, completedLessonIds);
                                return (
                                  <button
                                    className={`learn-lesson-button${selectedLessonId === lesson.lessonId ? " learn-lesson-button--active" : ""}`}
                                    key={lesson.lessonId}
                                    onClick={() => handleSelectLesson(module.moduleId, lesson.lessonId)}
                                    type="button"
                                  >
                                    <span className={`learn-lesson-button__index learn-lesson-button__index--${stage}`}>
                                      {getLessonActionLabel(stage)}
                                    </span>
                                    <span className="learn-lesson-button__body">
                                      <span className="learn-lesson-button__title">
                                        {module.orderIndex}.{lesson.orderIndex} {lesson.lessonTitle}
                                      </span>
                                      <span className="learn-lesson-button__meta">
                                        {getLessonStageLabel(stage)}
                                      </span>
                                    </span>
                                  </button>
                                );
                              })}
                            </div>
                          ) : null}
                        </div>
                      );
                    })}
                  </div>
                </div>
              </aside>
            </div>
          </div>
        )}
      </Section>
    </div>
  );
}
