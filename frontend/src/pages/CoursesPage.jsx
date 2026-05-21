import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/useAuth";
import { getAdminCourses, getPublishedCourses, publishCourse, unpublishCourse } from "../api/courseService";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import PageHeader from "../components/ui/PageHeader";
import Section from "../components/ui/Section";

export default function CoursesPage() {
  const { user } = useAuth();
  const isAdmin = user?.role === "Admin";
  const [courses, setCourses] = useState([]);
  const [errorMessage, setErrorMessage] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    loadCourses();
  }, [isAdmin]);

  async function loadCourses() {
    setIsLoading(true);
    setErrorMessage("");
    try {
      const items = isAdmin ? await getAdminCourses() : await getPublishedCourses();
      setCourses(items);
    } catch {
      setErrorMessage("Không thể tải danh sách khóa học.");
    } finally {
      setIsLoading(false);
    }
  }

  async function handleTogglePublish(course) {
    try {
      if (course.isPublished) {
        await unpublishCourse(course.id);
      } else {
        await publishCourse(course.id);
      }

      await loadCourses();
    } catch {
      setErrorMessage("Không thể cập nhật trạng thái publish của khóa học.");
    }
  }

  return (
    <Section className="section-stack">
      <PageHeader
        eyebrow="Khoa hoc"
        title="Khóa học"
        description="Khám phá các khóa học đã publish hoặc quản lý toàn bộ draft/published course nếu bạn là admin."
      />

      {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

      {isLoading ? (
        <Card variant="shadowed">
          <p>Đang tải danh sách khóa học...</p>
        </Card>
      ) : courses.length === 0 ? (
        <Card className="empty-state">
          <h2>Chưa có khóa học phù hợp</h2>
          <p>{isAdmin ? "Chưa có course nào được generate hoặc lưu trong hệ thống." : "Hiện chưa có course nào được publish cho người học."}</p>
        </Card>
      ) : (
        <div className="card-grid">
          {courses.map((course, index) => (
            <Card key={course.id} tone={index % 3 === 0 ? "mint" : index % 3 === 1 ? "lavender" : "saffron"} variant="shadowed">
              <div className="course-cover course-cover--gradient">
                <span className="ui-badge">{course.isPublished ? "Published" : "Draft"}</span>
                <h2>{course.title}</h2>
              </div>
              <div className="course-card__meta course-card__meta--stack">
                <p>{course.description}</p>
                <div className="course-card__stats">
                  <span>{course.moduleCount} module</span>
                  <span>{course.lessonCount} lesson</span>
                </div>
              </div>
              <div className="quick-actions">
                <Button as={Link} to={`/courses/${course.id}/learn`}>
                  Xem khóa học
                </Button>
                {isAdmin ? (
                  <Button onClick={() => handleTogglePublish(course)} variant="ghost">
                    {course.isPublished ? "Unpublish" : "Publish"}
                  </Button>
                ) : null}
              </div>
            </Card>
          ))}
        </div>
      )}
    </Section>
  );
}
