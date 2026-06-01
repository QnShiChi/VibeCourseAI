import { useEffect, useMemo, useState } from "react";
import { getAdminCourses } from "../api/courseService";
import { getDashboardStats } from "../api/dashboardService";
import Card from "../components/ui/Card";
import Section from "../components/ui/Section";

function formatMonthLabel(value) {
  return new Intl.DateTimeFormat("vi-VN", { month: "short", year: "2-digit" }).format(value);
}

function buildMonthlySeries(courses) {
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

export default function AdminFinancePage() {
  const [stats, setStats] = useState(null);
  const [courses, setCourses] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    async function loadData() {
      setIsLoading(true);
      setErrorMessage("");
      try {
        const [dashboardStats, adminCourses] = await Promise.all([
          getDashboardStats(),
          getAdminCourses()
        ]);
        setStats(dashboardStats);
        setCourses(adminCourses);
      } catch {
        setErrorMessage("Không thể tải báo cáo vận hành.");
      } finally {
        setIsLoading(false);
      }
    }

    void loadData();
  }, []);

  const monthlySeries = useMemo(() => buildMonthlySeries(courses), [courses]);
  const maxValue = Math.max(...monthlySeries.map((item) => item.value), 1);
  const publishedCount = courses.filter((course) => course.isPublished).length;
  const publishedRate = courses.length > 0 ? Math.round((publishedCount / courses.length) * 100) : 0;
  const latestCourses = [...courses].sort((left, right) => new Date(right.createdAt) - new Date(left.createdAt)).slice(0, 5);

  return (
    <Section className="admin-page admin-page--stack">
      <div className="admin-page__hero">
        <div>
          <p className="admin-page__eyebrow">Báo cáo hệ thống</p>
          <h1>Báo cáo vận hành</h1>
          <p className="admin-page__description">
            Trang này đang dùng dữ liệu thật từ hệ thống học tập hiện có. Hệ thống thanh toán chưa được nối nên chưa hiển thị doanh thu tài chính.
          </p>
        </div>
      </div>

      {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

      <div className="admin-overview-grid">
        <Card className="admin-stat-card" variant="shadowed">
          <span className="admin-stat-card__label">Người dùng hệ thống</span>
          <strong>{stats?.usersCount ?? "--"}</strong>
        </Card>
        <Card className="admin-stat-card" variant="shadowed">
          <span className="admin-stat-card__label">Khóa học hiện có</span>
          <strong>{stats?.coursesCount ?? "--"}</strong>
        </Card>
        <Card className="admin-stat-card" variant="shadowed">
          <span className="admin-stat-card__label">Tỷ lệ publish</span>
          <strong>{publishedRate}%</strong>
        </Card>
        <Card className="admin-stat-card" variant="shadowed">
          <span className="admin-stat-card__label">Generation jobs</span>
          <strong>{stats?.generationJobsCount ?? "--"}</strong>
        </Card>
      </div>

      <div className="admin-report-grid">
        <Card className="admin-chart-card" variant="shadowed">
          <div className="admin-chart-card__header">
            <div>
              <p className="admin-page__eyebrow">Dòng nội dung</p>
              <h2>Tăng trưởng khóa học 6 tháng gần nhất</h2>
            </div>
          </div>

          {isLoading ? (
            <p>Đang tải biểu đồ...</p>
          ) : (
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
          )}
        </Card>

        <Card className="admin-chart-card" variant="shadowed">
          <div className="admin-chart-card__header">
            <div>
              <p className="admin-page__eyebrow">Minh bạch dữ liệu</p>
              <h2>Trạng thái báo cáo</h2>
            </div>
          </div>
          <div className="admin-note-list">
            <div className="admin-note-list__item">
              <strong>Đã có dữ liệu thật</strong>
              <span>Người dùng, khóa học, đề cương, generation jobs.</span>
            </div>
            <div className="admin-note-list__item">
              <strong>Chưa có dữ liệu tài chính</strong>
              <span>Repo hiện chưa nối payment gateway hoặc transaction ledger.</span>
            </div>
            <div className="admin-note-list__item">
              <strong>Khuyến nghị</strong>
              <span>Giữ page này làm báo cáo vận hành cho tới khi backend tài chính sẵn sàng.</span>
            </div>
          </div>
        </Card>
      </div>

      <Card className="admin-table-card" variant="shadowed">
        <div className="admin-table">
          <div className="admin-table__header admin-course-row">
            <span>Khóa học</span>
            <span>Category</span>
            <span>Module</span>
            <span>Bài học</span>
            <span>Trạng thái</span>
          </div>
          {latestCourses.map((course) => (
            <div className="admin-table__row admin-course-row" key={course.id}>
              <span>{course.title}</span>
              <span>{course.category || "Khóa học AI"}</span>
              <span>{course.moduleCount}</span>
              <span>{course.lessonCount}</span>
              <span className={`admin-status-badge${course.isPublished ? " admin-status-badge--success" : " admin-status-badge--muted"}`}>
                {course.isPublished ? "Đã publish" : "Bản nháp"}
              </span>
            </div>
          ))}
        </div>
      </Card>
    </Section>
  );
}
