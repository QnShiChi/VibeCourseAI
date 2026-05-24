import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { getCourseLearnPayload } from "../api/courseService";
import { useAuth } from "../auth/AuthContext";
import LessonComments from "../components/comments/LessonComments";
import Card from "../components/ui/Card";
import Section from "../components/ui/Section";
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

export default function CourseLearnPage() {
  const { courseId = "" } = useParams();
  const { user } = useAuth();
  const { theme } = useTheme();
  const [course, setCourse] = useState(null);
  const [selectedLessonId, setSelectedLessonId] = useState(null);
  const [expandedModules, setExpandedModules] = useState({});
  const [errorMessage, setErrorMessage] = useState("");
  const [isLoading, setIsLoading] = useState(true);

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
  const progressPercent =
    currentLessonIndex >= 0 && totalLessons ? Math.round(((currentLessonIndex + 1) / totalLessons) * 100) : 0;
  const previousLesson = currentLessonIndex > 0 ? flatLessons[currentLessonIndex - 1] : null;
  const nextLesson =
    currentLessonIndex >= 0 && currentLessonIndex < totalLessons - 1 ? flatLessons[currentLessonIndex + 1] : null;
  const isAdmin = user?.role === "Admin";

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
            <div className="learn-layout">
              <div className="learn-layout__main">
                <section className="learn-hero">
                  <p className="learn-hero__eyebrow">Đang học</p>
                  <h1>{course.courseTitle}</h1>
                  <p>{course.courseDescription}</p>
                </section>

                <article className="learn-stage-card">
                  <div className="learn-stage-card__media">
                    <span className="learn-stage-card__badge">
                      {selectedModule
                        ? `${selectedModule.orderIndex}.${selectedLesson.orderIndex}`
                        : `Bài ${currentLessonIndex + 1}`}
                    </span>
                    {selectedLesson.videoUrl ? (
                      <video controls preload="metadata" src={selectedLesson.videoUrl}>
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

                  <div className="learn-stage-card__summary">
                    <h2>{selectedLesson.lessonTitle}</h2>
                    <p>{selectedLesson.description}</p>
                  </div>
                </article>

                <Card className="learn-reading-card" variant="shadowed">
                  <h2>Nội dung bài học</h2>
                  <pre className="text-preview learn-content-preview">{selectedLesson.contentSeed}</pre>
                </Card>

                <Card className="learn-comments-card" variant="shadowed">
                  <LessonComments isAdmin={isAdmin} lessonId={selectedLesson.lessonId} />
                </Card>

                <div className="learn-footer-nav">
                  <button disabled={!previousLesson} onClick={() => handleNavigateLesson(previousLesson)} type="button">
                    Bài trước
                  </button>
                  <p>
                    <span>Đang học:</span> {selectedLesson.lessonTitle}
                  </p>
                  <button disabled={!nextLesson} onClick={() => handleNavigateLesson(nextLesson)} type="button">
                    Tiếp tục bài học
                  </button>
                </div>
              </div>

              <aside className="learn-sidebar-panel">
                <div className="learn-sidebar-panel__inner">
                  <div className="learn-sidebar-panel__header">
                    <h2>Nội dung khóa học</h2>
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
                            aria-expanded={isExpanded}
                            className={`learn-module__header${isExpanded ? " learn-module__header--expanded" : ""}`}
                            onClick={() => handleToggleModule(module.moduleId)}
                            type="button"
                          >
                            <div>
                              <strong>{module.orderIndex}. {module.moduleTitle}</strong>
                              <span>{module.lessons.length} bài học</span>
                            </div>
                            <span>{isExpanded ? "⌃" : "⌄"}</span>
                          </button>

                          {isExpanded ? (
                            <div className="learn-module__lessons">
                              {sortByOrder(module.lessons).map((lesson) => (
                                <button
                                  className={`learn-lesson-button${selectedLessonId === lesson.lessonId ? " learn-lesson-button--active" : ""}`}
                                  key={lesson.lessonId}
                                  onClick={() => handleSelectLesson(module.moduleId, lesson.lessonId)}
                                  type="button"
                                >
                                  <span className="learn-lesson-button__index">
                                    {String(lesson.orderIndex).padStart(2, "0")}
                                  </span>
                                  <strong>{lesson.lessonTitle}</strong>
                                </button>
                              ))}
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
