import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { getAdminCourses, publishCourse, unpublishCourse } from "../api/courseService";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import Section from "../components/ui/Section";

function formatDate(value) {
  return new Intl.DateTimeFormat("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric"
  }).format(new Date(value));
}

export default function AdminCoursesPage() {
  const [courses, setCourses] = useState([]);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  async function loadCourses() {
    setIsLoading(true);
    setErrorMessage("");
    try {
      setCourses(await getAdminCourses());
    } catch {
      setErrorMessage("Không thể tải danh sách khóa học quản trị.");
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadCourses();
  }, []);

  async function handleTogglePublish(course) {
    try {
      if (course.isPublished) {
        await unpublishCourse(course.id);
      } else {
        await publishCourse(course.id);
      }

      await loadCourses();
    } catch {
      setErrorMessage("Không thể cập nhật trạng thái khóa học.");
    }
  }

  const filteredCourses = useMemo(() => {
    const keyword = searchTerm.trim().toLowerCase();
    return courses.filter((course) => {
      const matchesKeyword = keyword.length === 0
        || `${course.title} ${course.description} ${course.category}`.toLowerCase().includes(keyword);
      const matchesStatus = statusFilter === "all"
        || (statusFilter === "published" && course.isPublished)
        || (statusFilter === "draft" && !course.isPublished);
      return matchesKeyword && matchesStatus;
    });
  }, [courses, searchTerm, statusFilter]);

  const publishedCount = courses.filter((course) => course.isPublished).length;
  const draftCount = courses.length - publishedCount;
  const lessonCount = courses.reduce((sum, course) => sum + course.lessonCount, 0);

  return (
    <Section className="admin-page admin-page--stack">
      <div className="admin-page__hero">
        <div>
          <p className="admin-page__eyebrow">Kho nội dung</p>
          <h1>Quản lý khóa học</h1>
          <p className="admin-page__description">
            Theo dõi vòng đời khóa học, trạng thái publish và truy cập nhanh tới khu điều khiển nội dung.
          </p>
        </div>
        <div className="admin-page__hero-actions">
          <Button as={Link} to="/admin/syllabuses">Nhập đề cương mới</Button>
          <Button onClick={() => void loadCourses()} variant="ghost">Làm mới</Button>
        </div>
      </div>

      <div className="admin-overview-grid">
        <Card className="admin-stat-card" variant="shadowed">
          <span className="admin-stat-card__label">Tổng khóa học</span>
          <strong>{courses.length}</strong>
        </Card>
        <Card className="admin-stat-card" variant="shadowed">
          <span className="admin-stat-card__label">Đã publish</span>
          <strong>{publishedCount}</strong>
        </Card>
        <Card className="admin-stat-card" variant="shadowed">
          <span className="admin-stat-card__label">Bản nháp</span>
          <strong>{draftCount}</strong>
        </Card>
        <Card className="admin-stat-card" variant="shadowed">
          <span className="admin-stat-card__label">Tổng bài học</span>
          <strong>{lessonCount}</strong>
        </Card>
      </div>

      <Card className="admin-panel admin-panel--toolbar" variant="shadowed">
        <label className="admin-toolbar__search">
          <span aria-hidden="true">⌕</span>
          <input
            onChange={(event) => setSearchTerm(event.target.value)}
            placeholder="Tìm theo tên, mô tả hoặc category..."
            value={searchTerm}
          />
        </label>

        <div className="admin-toolbar__filters">
          <button className={`admin-filter-pill${statusFilter === "all" ? " admin-filter-pill--active" : ""}`} onClick={() => setStatusFilter("all")} type="button">Tất cả</button>
          <button className={`admin-filter-pill${statusFilter === "published" ? " admin-filter-pill--active" : ""}`} onClick={() => setStatusFilter("published")} type="button">Đã publish</button>
          <button className={`admin-filter-pill${statusFilter === "draft" ? " admin-filter-pill--active" : ""}`} onClick={() => setStatusFilter("draft")} type="button">Bản nháp</button>
        </div>
      </Card>

      {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

      <div className="admin-card-grid">
        {isLoading ? (
          <Card className="admin-empty-card" variant="shadowed">
            <p>Đang tải dữ liệu khóa học...</p>
          </Card>
        ) : filteredCourses.length === 0 ? (
          <Card className="admin-empty-card" variant="shadowed">
            <h2>Không có khóa học phù hợp</h2>
            <p>Thử đổi bộ lọc hoặc từ khóa tìm kiếm để mở rộng kết quả.</p>
          </Card>
        ) : (
          filteredCourses.map((course) => (
            <Card className="admin-course-card" key={course.id} variant="shadowed">
              <div className="admin-course-card__media">
                {course.thumbnailUrl ? (
                  <img alt={course.title} src={course.thumbnailUrl} />
                ) : (
                  <div className="admin-course-card__fallback">{course.title}</div>
                )}
                <span className={`admin-status-badge${course.isPublished ? " admin-status-badge--success" : ""}`}>
                  {course.isPublished ? "Đã publish" : "Bản nháp"}
                </span>
              </div>

              <div className="admin-course-card__body">
                <div className="admin-course-card__heading">
                  <p>{course.category || "Khóa học AI"}</p>
                  <h2>{course.title}</h2>
                </div>
                <p className="admin-course-card__description">{course.description || "Chưa có mô tả cho khóa học này."}</p>
                <div className="admin-course-card__meta">
                  <span>{course.moduleCount} module</span>
                  <span>{course.lessonCount} bài học</span>
                  <span>{formatDate(course.createdAt)}</span>
                </div>
              </div>

              <div className="admin-course-card__actions">
                <Button as={Link} to={`/admin/courses/${course.id}`}>Điều khiển</Button>
                <Button onClick={() => void handleTogglePublish(course)} variant="ghost">
                  {course.isPublished ? "Ẩn khóa học" : "Publish"}
                </Button>
              </div>
            </Card>
          ))
        )}
      </div>
    </Section>
  );
}
