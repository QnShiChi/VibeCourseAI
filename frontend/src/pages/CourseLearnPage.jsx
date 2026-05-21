import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { getCourseLearnPayload } from "../api/courseService";
import Card from "../components/ui/Card";
import PageHeader from "../components/ui/PageHeader";
import Section from "../components/ui/Section";

export default function CourseLearnPage() {
  const { courseId = "" } = useParams();
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
      const defaults = {};
      data.modules.forEach((module, index) => {
        defaults[module.moduleId] = index === 0;
      });
      const selectedContainer = data.modules.find((module) =>
        module.lessons.some((lesson) => lesson.lessonId === data.selectedLessonId)
      );
      if (selectedContainer) {
        defaults[selectedContainer.moduleId] = true;
      }
      setExpandedModules(defaults);
    } catch {
      setErrorMessage("Không thể tải trang học của khóa học này.");
    } finally {
      setIsLoading(false);
    }
  }

  function handleToggleModule(moduleId) {
    setExpandedModules((current) => ({
      ...current,
      [moduleId]: !current[moduleId]
    }));
  }

  function handleSelectLesson(moduleId, lessonId) {
    setSelectedLessonId(lessonId);
    setExpandedModules((current) => ({
      ...current,
      [moduleId]: true
    }));
  }

  const selectedLesson = course?.modules
    ?.flatMap((module) => module.lessons)
    ?.find((lesson) => lesson.lessonId === selectedLessonId) ?? course?.selectedLesson;

  return (
    <Section className="section-stack">
      <PageHeader
        eyebrow="Hoc tap"
        title={course?.courseTitle ?? "Đang tải khóa học"}
        description={course?.courseDescription ?? "Theo dõi lesson theo từng module và học theo đúng cấu trúc khóa học đã publish."}
      />

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
        <div className="learn-layout">
          <div className="learn-layout__main">
            <Card className="learn-stage" tone="saffron" variant="shadowed">
              <span className="ui-badge">Lesson hiện tại</span>
              <div className="learn-stage__player">
                <div className="learn-stage__player-shell">
                  <div className="learn-stage__player-icon">▶</div>
                  <div className="learn-stage__player-text">
                    <strong>{selectedLesson.lessonTitle}</strong>
                    <span>{selectedLesson.duration ? `${selectedLesson.duration} phút` : "Video placeholder"}</span>
                  </div>
                </div>
              </div>
              <h2>{selectedLesson.lessonTitle}</h2>
              <p>{selectedLesson.description}</p>
            </Card>

            <Card variant="shadowed">
              <h2>Nội dung bài học</h2>
              <pre className="text-preview learn-content-preview">{selectedLesson.contentSeed}</pre>
            </Card>
          </div>

          <Card className="learn-layout__sidebar" variant="shadowed">
            <h2>Nội dung khóa học</h2>
            <div className="learn-sidebar">
              {course.modules.map((module) => {
                const isExpanded = Boolean(expandedModules[module.moduleId]);
                return (
                  <div className="learn-module" key={module.moduleId}>
                    <button
                      className="learn-module__header"
                      onClick={() => handleToggleModule(module.moduleId)}
                      type="button"
                    >
                      <div>
                        <strong>{module.orderIndex}. {module.moduleTitle}</strong>
                        <span>{module.lessons.length} bài học</span>
                      </div>
                      <span>{isExpanded ? "−" : "+"}</span>
                    </button>

                    {isExpanded ? (
                      <div className="learn-module__lessons">
                        {module.lessons.map((lesson) => (
                          <button
                            className={`learn-lesson-button${selectedLessonId === lesson.lessonId ? " learn-lesson-button--active" : ""}`}
                            key={lesson.lessonId}
                            onClick={() => handleSelectLesson(module.moduleId, lesson.lessonId)}
                            type="button"
                          >
                            <strong>{lesson.orderIndex}. {lesson.lessonTitle}</strong>
                            <span>{lesson.description}</span>
                          </button>
                        ))}
                      </div>
                    ) : null}
                  </div>
                );
              })}
            </div>
          </Card>
        </div>
      )}
    </Section>
  );
}
