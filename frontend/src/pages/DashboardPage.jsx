import { useEffect, useMemo, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/useAuth";
import { getAdminCourses } from "../api/courseService";
import { getDashboardPaymentOverview, getDashboardStats } from "../api/dashboardService";
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
    minute: "2-digit",
    hour12: false,
    timeZone: "Asia/Ho_Chi_Minh"
  }).format(new Date(value));
}

function buildWaveChartSeries(timeline = [], progress = 1) {
  const chartWidth = 720;
  const chartHeight = 280;
  const chartPaddingX = 18;
  const chartPaddingTop = 18;
  const chartPaddingBottom = 34;
  const usableHeight = chartHeight - chartPaddingTop - chartPaddingBottom;
  const maxValue = Math.max(
    ...timeline.flatMap((item) => [item.paidOrders, item.failedOrExpiredOrders]),
    1
  );
  const buildSeries = (key) => timeline.map((item, index) => {
    const x =
      timeline.length === 1
        ? chartWidth / 2
        : chartPaddingX + (index / Math.max(timeline.length - 1, 1)) * (chartWidth - chartPaddingX * 2);
    const animatedValue = item[key] * progress;
    const y = chartPaddingTop + usableHeight - (animatedValue / maxValue) * usableHeight;
    return { label: item.label, value: animatedValue, x, y };
  });

  return {
    chartWidth,
    chartHeight,
    series: {
      paid: buildSeries("paidOrders"),
      failed: buildSeries("failedOrExpiredOrders")
    }
  };
}

function buildLinePath(points) {
  return points.map((point, index) => `${index === 0 ? "M" : "L"} ${point.x.toFixed(2)} ${point.y.toFixed(2)}`).join(" ");
}

function buildAreaPath(points, chartHeight) {
  if (points.length === 0) {
    return "";
  }

  return `${buildLinePath(points)} L ${points[points.length - 1].x.toFixed(2)} ${(chartHeight - 34).toFixed(2)} L ${points[0].x.toFixed(2)} ${(chartHeight - 34).toFixed(2)} Z`;
}

export default function DashboardPage() {
  const { user } = useAuth();
  const [stats, setStats] = useState(null);
  const [courses, setCourses] = useState([]);
  const [users, setUsers] = useState([]);
  const [paymentOverview, setPaymentOverview] = useState(null);
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");
  const [paymentErrorMessage, setPaymentErrorMessage] = useState("");
  const [animationSeed, setAnimationSeed] = useState(0);
  const [animationProgress, setAnimationProgress] = useState(0);
  const animationFrameRef = useRef(0);

  const restartDashboardAnimation = () => {
    setAnimationSeed((current) => current + 1);
  };

  const fetchStats = async () => {
    setLoading(true);
    setErrorMessage("");
    setPaymentErrorMessage("");
    try {
      const [dashboardStats, adminCourses, adminUsers, paymentOverviewResult] = await Promise.all([
        getDashboardStats(),
        getAdminCourses(),
        getUsers(),
        getDashboardPaymentOverview().catch(() => null)
      ]);
      setStats(dashboardStats);
      setCourses(adminCourses);
      setUsers(adminUsers);
      setPaymentOverview(paymentOverviewResult);
      if (!paymentOverviewResult) {
        setPaymentErrorMessage("Không thể tải dữ liệu hóa đơn trên dashboard.");
      }
      restartDashboardAnimation();
    } catch {
      setErrorMessage("Không thể tải dashboard quản trị.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void fetchStats();
  }, []);

  useEffect(() => {
    if (loading) {
      return undefined;
    }

    const duration = 1100;
    let startTime = null;

    setAnimationProgress(0);
    cancelAnimationFrame(animationFrameRef.current);

    const tick = (timestamp) => {
      if (startTime === null) {
        startTime = timestamp;
      }

      const nextProgress = Math.min((timestamp - startTime) / duration, 1);
      setAnimationProgress(nextProgress);

      if (nextProgress < 1) {
        animationFrameRef.current = requestAnimationFrame(tick);
      }
    };

    animationFrameRef.current = requestAnimationFrame(tick);

    return () => cancelAnimationFrame(animationFrameRef.current);
  }, [animationSeed, loading]);

  useEffect(() => {
    const handleVisibilityChange = () => {
      if (document.visibilityState === "visible" && !loading) {
        restartDashboardAnimation();
      }
    };

    document.addEventListener("visibilitychange", handleVisibilityChange);

    return () => document.removeEventListener("visibilitychange", handleVisibilityChange);
  }, [loading]);

  const easedAnimationProgress = easeOutCubic(animationProgress);

  const metrics = [
    { label: "Người dùng hệ thống", value: stats?.usersCount ?? "--" },
    { label: "Đề cương", value: stats?.syllabusesCount ?? "--" },
    { label: "Khóa học", value: stats?.coursesCount ?? "--" },
    { label: "Generation jobs", value: stats?.generationJobsCount ?? "--" }
  ];
  const animatedMetrics = metrics.map((metric) => ({
    ...metric,
    value: animateDashboardNumber(metric.value, easedAnimationProgress)
  }));

  const publishedCourses = courses.filter((course) => course.isPublished).length;
  const activeUsers = users.filter((item) => item.isActive).length;
  const animatedPublishedCourses = animateDashboardNumber(publishedCourses, easedAnimationProgress);
  const animatedActiveUsers = animateDashboardNumber(activeUsers, easedAnimationProgress);
  const monthlySeries = useMemo(() => buildMonthlyCourseSeries(courses), [courses]);
  const maxValue = Math.max(...monthlySeries.map((item) => item.value), 1);
  const animatedMonthlySeries = monthlySeries.map((item) => ({
    ...item,
    animatedValue: item.value * easedAnimationProgress
  }));
  const paymentDistribution = [
    {
      key: "paid",
      label: "Đã thanh toán",
      value: paymentOverview?.paidOrders ?? 0,
      dotClass: "admin-payment-legend__dot--success",
      note: "Đơn hoàn tất hoặc ghi nhận thanh toán muộn"
    },
    {
      key: "failed",
      label: "Hết hạn / hủy / lỗi",
      value: paymentOverview?.failedOrExpiredOrders ?? 0,
      dotClass: "admin-payment-legend__dot--danger",
      note: "Đơn hết thời gian, bị hủy hoặc phát sinh lỗi thanh toán"
    }
  ];
  const animatedPaymentDistribution = paymentDistribution.map((item) => ({
    ...item,
    value: animateDashboardNumber(item.value, easedAnimationProgress)
  }));
  const animatedPaymentTotalOrders = animateDashboardNumber(paymentOverview?.totalOrders ?? 0, easedAnimationProgress);
  const paymentWaveChart = useMemo(
    () => buildWaveChartSeries(paymentOverview?.timeline ?? [], easedAnimationProgress),
    [paymentOverview, easedAnimationProgress]
  );
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
  const recentOrders = (paymentOverview?.recentOrders ?? []).filter((order) => order.status !== "Pending");

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
            <strong>{animatedActiveUsers}</strong>
          </div>
          <div>
            <span>Khóa học đã publish</span>
            <strong>{animatedPublishedCourses}</strong>
          </div>
        </div>
      </Card>

      <div className="admin-overview-grid">
        {animatedMetrics.map((metric) => (
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
              <p className="admin-page__eyebrow">Thanh toán</p>
              <h2>Tổng quan hóa đơn</h2>
            </div>
          </div>

          {paymentErrorMessage ? <p className="ui-alert ui-alert--error">{paymentErrorMessage}</p> : null}

          <div className="admin-payment-overview">
            <div className="admin-payment-overview__summary">
              <span className="admin-mini-stat__label">Tổng hóa đơn</span>
              <strong>{paymentOverview ? animatedPaymentTotalOrders : "--"}</strong>
              <p>Biểu đồ gợn sóng thể hiện biến động hóa đơn theo trạng thái trong 7 ngày gần nhất.</p>
            </div>

            <div className="admin-payment-wave-chart">
              <svg
                aria-label="Biểu đồ gợn sóng hóa đơn 7 ngày gần nhất"
                className="admin-payment-wave-chart__svg"
                role="img"
                viewBox={`0 0 ${paymentWaveChart.chartWidth} ${paymentWaveChart.chartHeight}`}
              >
                <path
                  className="admin-payment-wave-chart__area admin-payment-wave-chart__area--failed"
                  d={buildAreaPath(paymentWaveChart.series.failed, paymentWaveChart.chartHeight)}
                />
                <path
                  className="admin-payment-wave-chart__line admin-payment-wave-chart__line--failed"
                  d={buildLinePath(paymentWaveChart.series.failed)}
                />
                <path
                  className="admin-payment-wave-chart__area admin-payment-wave-chart__area--paid"
                  d={buildAreaPath(paymentWaveChart.series.paid, paymentWaveChart.chartHeight)}
                />
                <path
                  className="admin-payment-wave-chart__line admin-payment-wave-chart__line--paid"
                  d={buildLinePath(paymentWaveChart.series.paid)}
                />

                {paymentWaveChart.series.paid.map((point) => (
                  <text className="admin-payment-wave-chart__label" key={point.label} textAnchor="middle" x={point.x} y={paymentWaveChart.chartHeight - 10}>
                    {point.label}
                  </text>
                ))}
              </svg>
            </div>

            <div className="admin-payment-legend">
              {animatedPaymentDistribution.map((item) => (
                <div className="admin-payment-legend__item" key={item.key}>
                  <div className="admin-payment-legend__topline">
                    <span className={`admin-payment-legend__dot ${item.dotClass}`.trim()} />
                    <strong>{item.label}</strong>
                    <span>{item.value}</span>
                  </div>
                  <small>{item.note}</small>
                </div>
              ))}
            </div>
          </div>
        </Card>

        <Card className="admin-panel" variant="shadowed">
          <div className="admin-panel__split">
            <div>
              <p className="admin-page__eyebrow">Hóa đơn gần đây</p>
              <h2>Theo dõi giao dịch</h2>
            </div>
          </div>

          {recentOrders.length === 0 ? (
            <p>Chưa có hóa đơn nào được ghi nhận.</p>
          ) : (
            <div className="admin-activity-list">
              {recentOrders.slice(0, 5).map((order) => (
                <div className="admin-activity-list__item" key={order.paymentOrderId}>
                  <div className={`admin-activity-list__dot ${buildAdminPaymentDotClass(order.status)}`.trim()} />
                  <div className="admin-payment-item">
                    <div className="admin-payment-item__header">
                      <strong>{order.userFullName}</strong>
                      <span className={buildAdminPaymentBadgeClass(order.status)}>
                        {buildAdminPaymentStatusLabel(order.status)}
                      </span>
                    </div>
                    <span>{order.courseTitle}</span>
                    <small>
                      {order.orderCode} • {formatCurrency(order.amount)} • {formatDateTime(order.paidAt ?? order.createdAt)}
                    </small>
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
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
            {animatedMonthlySeries.map((item) => (
              <div className="admin-bar-chart__item" key={item.key}>
                <div className="admin-bar-chart__value">
                  <span
                    style={{
                      height: `${Math.max((item.animatedValue / maxValue) * 100, item.value > 0 ? 4 * easedAnimationProgress : 0)}%`
                    }}
                  />
                </div>
                <strong>{item.label}</strong>
                <small>{animateDashboardNumber(item.value, easedAnimationProgress)} khóa học</small>
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
            <strong>{animateDashboardNumber(stats?.negativeCommentsCount ?? 0, easedAnimationProgress)}</strong>
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
            <div><span>Tổng jobs</span><strong>{animateDashboardNumber(stats?.generationJobsCount ?? "--", easedAnimationProgress)}</strong></div>
            <div><span>Khóa học đã publish</span><strong>{animatedPublishedCourses}</strong></div>
            <div><span>Đề cương hiện có</span><strong>{animateDashboardNumber(stats?.syllabusesCount ?? "--", easedAnimationProgress)}</strong></div>
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

function formatCurrency(value) {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0
  }).format(value ?? 0);
}

function animateDashboardNumber(value, progress) {
  if (typeof value !== "number" || Number.isNaN(value)) {
    return value;
  }

  return Math.round(value * progress);
}

function easeOutCubic(value) {
  return 1 - (1 - value) ** 3;
}

function buildAdminPaymentStatusLabel(status) {
  if (status === "Paid") {
    return "Đã thanh toán";
  }

  if (status === "LatePaid") {
    return "Thanh toán muộn";
  }

  if (status === "Pending") {
    return "Chờ thanh toán";
  }

  if (status === "Expired") {
    return "Hết hạn";
  }

  if (status === "Cancelled") {
    return "Đã hủy thanh toán";
  }

  if (status === "Failed") {
    return "Lỗi";
  }

  return status || "Không xác định";
}

function buildAdminPaymentBadgeClass(status) {
  if (status === "Paid" || status === "LatePaid") {
    return "admin-status-badge admin-status-badge--success";
  }

  if (status === "Pending") {
    return "admin-status-badge admin-status-badge--warning";
  }

  if (status === "Expired" || status === "Failed" || status === "Cancelled") {
    return "admin-status-badge admin-status-badge--danger";
  }

  return "admin-status-badge admin-status-badge--muted";
}

function buildAdminPaymentDotClass(status) {
  if (status === "Paid" || status === "LatePaid") {
    return "";
  }

  if (status === "Expired" || status === "Failed" || status === "Cancelled") {
    return "admin-activity-list__dot--danger";
  }

  return "admin-activity-list__dot--pending";
}
