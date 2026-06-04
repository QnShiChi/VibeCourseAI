import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/useAuth";
import { getAdminCourses } from "../api/courseService";
import { getDashboardStats } from "../api/dashboardService";
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

function getInitials(name = "") {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0] ?? "")
    .join("")
    .toUpperCase() || "AD";
}

export default function AdminProfilePage() {
  const { user } = useAuth();
  const [stats, setStats] = useState(null);
  const [courses, setCourses] = useState([]);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    async function loadData() {
      try {
        const [dashboardStats, adminCourses] = await Promise.all([
          getDashboardStats(),
          getAdminCourses()
        ]);
        setStats(dashboardStats);
        setCourses(adminCourses);
      } catch {
        setErrorMessage("Không thể tải Admin Profile.");
      }
    }

    void loadData();
  }, []);

  const publishedCourses = courses.filter((course) => course.isPublished).length;
  const topCategories = useMemo(() => {
    const counts = new Map();
    courses.forEach((course) => {
      const key = course.category || "Khác";
      counts.set(key, (counts.get(key) ?? 0) + 1);
    });

    return [...counts.entries()].sort((left, right) => right[1] - left[1]).slice(0, 3);
  }, [courses]);

  const latestCourses = [...courses].sort((left, right) => new Date(right.createdAt) - new Date(left.createdAt)).slice(0, 3);

  return (
    <Section className="admin-page admin-page--stack">
      <div className="admin-profile-layout">
        <Card className="admin-profile-hero admin-profile-hero--expanded" variant="shadowed">
          <div className="admin-profile-hero__identity">
            <div className="admin-avatar admin-avatar--xl">{getInitials(user?.fullName)}</div>
            <div>
              <p className="admin-page__eyebrow">Admin Profile</p>
              <h1>{user?.fullName || "Admin User"}</h1>
              <span>{user?.email || "Không có email đăng nhập"}</span>
              <div className="admin-role-badge admin-role-badge--admin">Administrator</div>
            </div>
          </div>

          <div className="admin-profile-hero__actions">
            <Button as={Link} to="/change-password">Đổi mật khẩu</Button>
            <Button as={Link} to="/admin/settings" variant="ghost">Mở cài đặt</Button>
          </div>
        </Card>

        <Card className="admin-panel" variant="shadowed">
          <p className="admin-page__eyebrow">Dữ liệu thật</p>
          <h2>Tóm tắt điều hành</h2>
          <div className="admin-detail-list">
            <div><span>Tổng khóa học</span><strong>{stats?.coursesCount ?? "--"}</strong></div>
            <div><span>Đã publish</span><strong>{publishedCourses}</strong></div>
            <div><span>Người dùng hệ thống</span><strong>{stats?.usersCount ?? "--"}</strong></div>
            <div><span>Generation jobs</span><strong>{stats?.generationJobsCount ?? "--"}</strong></div>
          </div>
        </Card>
      </div>

      {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

      <div className="admin-settings-grid admin-settings-grid--profile">
        <Card className="admin-panel" variant="shadowed">
          <div className="admin-panel__split">
            <div>
              <p className="admin-page__eyebrow">Khóa học gần đây</p>
              <h2>Đang quản lý</h2>
            </div>
            <Link className="admin-inline-link" to="/admin/courses">Xem tất cả</Link>
          </div>

          <div className="admin-course-list">
            {latestCourses.map((course) => (
              <Link className="admin-course-list__item" key={course.id} to={`/admin/courses/${course.id}`}>
                <div>
                  <strong>{course.title}</strong>
                  <span>{course.moduleCount} module • {course.lessonCount} bài học • {formatDate(course.createdAt)}</span>
                </div>
                <span className={`admin-status-badge${course.isPublished ? " admin-status-badge--success" : " admin-status-badge--muted"}`}>
                  {course.isPublished ? "Đã publish" : "Bản nháp"}
                </span>
              </Link>
            ))}
          </div>
        </Card>

        <Card className="admin-panel" variant="shadowed">
          <p className="admin-page__eyebrow">Phân bổ category</p>
          <h2>Nhóm nội dung nổi bật</h2>
          <div className="admin-distribution-list">
            {topCategories.map(([category, count]) => (
              <div className="admin-distribution-list__item" key={category}>
                <span>{category}</span>
                <strong>{count} khóa học</strong>
              </div>
            ))}
          </div>
        </Card>
      </div>
    </Section>
  );
}
