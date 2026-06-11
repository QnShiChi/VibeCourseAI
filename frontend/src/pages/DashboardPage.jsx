import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/useAuth";
import { getAdminCourses } from "../api/courseService";
import { getDashboardStats } from "../api/dashboardService";
import { getUsers } from "../api/userService";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import Section from "../components/ui/Section";

function formatMonthLabel(date) {
  return new Intl.DateTimeFormat("vi-VN", { month: "short" }).format(date);
}

function buildMonthlyCourseSeries(courses) {
  const now = new Date();
  const months = Array.from({ length: 6 }, (_, index) => {
    const date = new Date(now.getFullYear(), now.getMonth() - (5 - index), 1);
    return {
      key: `${date.getFullYear()}-${date.getMonth()}`,
      label: formatMonthLabel(date),
      value: 0
    };
  });

  courses.forEach((course) => {
    const createdAt = new Date(course.createdAt);
    const key = `${createdAt.getFullYear()}-${createdAt.getMonth()}`;
    const bucket = months.find((item) => item.key === key);
    if (bucket) {
      bucket.value += 1;
    }
  });

  return months;
}

function formatDateTime(value) {
  return new Intl.DateTimeFormat("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit"
  }).format(new Date(value));
}

export default function DashboardPage() {
  const { user } = useAuth();
  const [stats, setStats] = useState(null);
  const [courses, setCourses] = useState([]);
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  const fetchStats = async () => {
    setLoading(true);
    setErrorMessage("");
    try {
      const [dashboardStats, adminCourses, adminUsers] = await Promise.all([
        getDashboardStats(),
        getAdminCourses(),
        getUsers()
      ]);
      setStats(dashboardStats);
      setCourses(adminCourses);
      setUsers(adminUsers);
    } catch {
      setErrorMessage("Không thể tải dashboard quản trị.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void fetchStats();
  }, []);

  const metrics = [
    { label: "Người dùng hệ thống", value: stats?.usersCount ?? "--" },
    { label: "Đề cương", value: stats?.syllabusesCount ?? "--" },
    { label: "Khóa học", value: stats?.coursesCount ?? "--" },
    { label: "Generation jobs", value: stats?.generationJobsCount ?? "--" }
  ];

  const publishedCourses = courses.filter((course) => course.isPublished).length;
  const activeUsers = users.filter((item) => item.isActive).length;
  const monthlySeries = useMemo(() => buildMonthlyCourseSeries(courses), [courses]);
  const maxValue = Math.max(...monthlySeries.map((item) => item.value), 1);
  const activityFeed = useMemo(() => {
    const courseFeed = courses.slice(0, 3).map((course) => ({
      id: course.id,
      title: `Khóa học mới: "${course.title}"`,
      subtitle: `${course.lessonCount} bài học • ${course.isPublished ? "Đã publish" : "Bản nháp"}`,
      timestamp: course.createdAt
    }));
    const userFeed = users.slice(0, 3).map((item) => ({
      id: item.id,
      title: `Người dùng mới: ${item.fullName}`,
      subtitle: `${item.role} • ${item.isActive ? "Đang hoạt động" : "Đã khóa"}`,
      timestamp: item.createdAt
    }));

    return [...courseFeed, ...userFeed]
      .sort((left, right) => new Date(right.timestamp) - new Date(left.timestamp))
      .slice(0, 5);
  }, [courses, users]);

  return (
    <Section className="admin-page admin-page--stack">
      <div className="admin-page__hero">
        <div>
          <p className="admin-page__eyebrow">Bảng điều khiển quản trị</p>
          <h1>Dashboard</h1>
          <p className="admin-page__description">
            Theo dõi toàn cảnh người dùng, khóa học và pipeline AI trên một giao diện dark glassmorphism đồng bộ với hệ thiết kế mới.
          </p>
        </div>
        <div className="admin-page__hero-actions">
          <Button onClick={() => void fetchStats()} variant="ghost">{loading ? "Đang tải..." : "Làm mới dữ liệu"}</Button>
          <Button as={Link} to="/admin/syllabuses">Tạo khóa học mới</Button>
        </div>
      </div>

      {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

      <Card className="admin-profile-hero admin-profile-hero--dashboard" variant="shadowed">
        <div className="admin-profile-hero__identity">
          <div className="admin-avatar admin-avatar--xl">
            {(user?.fullName || "AD").split(/\s+/).slice(0, 2).map((part) => part[0] ?? "").join("").toUpperCase()}
          </div>
          <div>
            <p className="admin-page__eyebrow">Tài khoản hiện tại</p>
            <h2>{user?.fullName || "Administrator"}</h2>
            <span>{user?.email || "Không có email"}</span>
          </div>
        </div>
        <div className="admin-profile-hero__meta">
          <div>
            <span>Vai trò</span>
            <strong>{user?.role === "Admin" ? "Administrator" : user?.role || "User"}</strong>
          </div>
          <div>
            <span>Người dùng đang hoạt động</span>
            <strong>{activeUsers}</strong>
          </div>
          <div>
            <span>Khóa học đã publish</span>
            <strong>{publishedCourses}</strong>
          </div>
        </div>
      </Card>

      <div className="admin-overview-grid">
        {metrics.map((metric) => (
          <Card className="admin-stat-card" key={metric.label} variant="shadowed">
            <span className="admin-stat-card__label">{metric.label}</span>
            <strong>{metric.value}</strong>
          </Card>
        ))}
      </div>

      <div className="admin-dashboard-grid">
        <Card className="admin-chart-card" variant="shadowed">
          <div className="admin-chart-card__header">
            <div>
              <p className="admin-page__eyebrow">Tăng trưởng nội dung</p>
              <h2>Khóa học tạo mới 6 tháng gần nhất</h2>
            </div>
          </div>

          <div className="admin-bar-chart">
            {monthlySeries.map((item) => (
              <div className="admin-bar-chart__item" key={item.key}>
                <div className="admin-bar-chart__value">
                  <span style={{ height: `${Math.max((item.value / maxValue) * 100, item.value > 0 ? 14 : 4)}%` }} />
                </div>
                <strong>{item.label}</strong>
                <small>{item.value} khóa học</small>
              </div>
            ))}
          </div>
        </Card>

        <Card className="admin-panel" variant="shadowed">
          <div className="admin-panel__split">
            <div>
              <p className="admin-page__eyebrow">Hoạt động gần đây</p>
              <h2>Dòng sự kiện</h2>
            </div>
          </div>

          <div className="admin-activity-list">
            {activityFeed.map((item) => (
              <div className="admin-activity-list__item" key={item.id}>
                <div className="admin-activity-list__dot" />
                <div>
                  <strong>{item.title}</strong>
                  <span>{item.subtitle}</span>
                  <small>{formatDateTime(item.timestamp)}</small>
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>

      <div className="admin-settings-grid admin-settings-grid--triple">
        <Card className="admin-panel" variant="shadowed">
          <p className="admin-page__eyebrow">Quick actions</p>
          <h2>Điều hướng nhanh</h2>
          <div className="admin-action-list">
            <Link className="admin-action-list__item" to="/admin/courses">
              <strong>Đi tới khóa học</strong>
              <span>Quản lý publish, module, lesson và điều khiển nội dung.</span>
            </Link>
            <Link className="admin-action-list__item" to="/admin/users">
              <strong>Đi tới người dùng</strong>
              <span>Kiểm tra vai trò, trạng thái hoạt động và quyền truy cập.</span>
            </Link>
          </div>
        </Card>

        <Card className="admin-panel" variant="shadowed">
          <div className="admin-panel__split">
            <div>
              <p className="admin-page__eyebrow">Moderation queue</p>
              <h2>Cảnh báo bình luận tiêu cực</h2>
            </div>
            <strong>{stats?.negativeCommentsCount ?? 0}</strong>
          </div>
          <p>
            {stats?.negativeCommentsCount
              ? `Hiện có ${stats.negativeCommentsCount} bình luận tiêu cực chưa xử lý cần admin xem xét.`
              : "Chưa có bình luận tiêu cực cần xử lý."}
          </p>
          <div className="admin-action-list">
            <Button as={Link} to="/admin/comment-moderation" variant="ghost">
              Xem chi tiết
            </Button>
          </div>
        </Card>

        <Card className="admin-panel" variant="shadowed">
          <p className="admin-page__eyebrow">Pipeline AI</p>
          <h2>Trạng thái job</h2>
          <div className="admin-detail-list">
            <div><span>Tổng jobs</span><strong>{stats?.generationJobsCount ?? "--"}</strong></div>
            <div><span>Khóa học đã publish</span><strong>{publishedCourses}</strong></div>
            <div><span>Đề cương hiện có</span><strong>{stats?.syllabusesCount ?? "--"}</strong></div>
          </div>
        </Card>

        <Card className="admin-panel" variant="shadowed">
          <p className="admin-page__eyebrow">Hệ thống</p>
          <h2>Sức khỏe quản trị</h2>
          <div className="admin-detail-list">
            <div><span>Phiên đăng nhập</span><strong>Ổn định</strong></div>
            <div><span>Dữ liệu dashboard</span><strong>{loading ? "Đang đồng bộ" : "Đã cập nhật"}</strong></div>
            <div><span>Theme</span><strong>Dark-first UI</strong></div>
          </div>
        </Card>
      </div>
    </Section>
  );
}
