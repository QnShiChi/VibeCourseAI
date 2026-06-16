import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getAdminCourses, getPublishedCourses } from "../api/courseService";
import { getPurchaseHistory } from "../api/paymentService";
import { useAuth } from "../auth/useAuth";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import Section from "../components/ui/Section";
import { readCurrentLearningProgress } from "../utils/learningProgress";
import { formatActivityDuration, readWebActivitySeries } from "../utils/webActivity";

const FAVORITE_COURSE_IDS_STORAGE_KEY = "favorite-course-ids";
const WEB_ACTIVITY_FILTER_OPTIONS = ["7D", "30D", "1Y"];

function getInitials(fullName = "", email = "") {
  const parts = fullName.trim().split(/\s+/).filter(Boolean);

  if (parts.length >= 2) {
    return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
  }

  if (parts.length === 1) {
    return parts[0].slice(0, 2).toUpperCase();
  }

  return (email || "U").slice(0, 2).toUpperCase();
}

function getRoleLabel(role = "") {
  if (role === "Admin") {
    return "Quản trị viên";
  }

  if (role === "User") {
    return "Học viên";
  }

  return role || "Chưa xác định";
}

function loadFavoriteCourseIds() {
  try {
    const rawValue = window.localStorage.getItem(FAVORITE_COURSE_IDS_STORAGE_KEY);
    if (!rawValue) {
      return [];
    }

    const parsed = JSON.parse(rawValue);
    return Array.isArray(parsed) ? parsed.filter((item) => typeof item === "string") : [];
  } catch {
    return [];
  }
}

function getBarHeight(value, maxValue) {
  if (!maxValue || value <= 0) {
    return 28;
  }

  return Math.max(28, Math.round((value / maxValue) * 180));
}

function formatHoursLabel(seconds) {
  const hours = seconds / 3600;
  if (hours === 0) {
    return "0h";
  }

  if (hours >= 10 || Number.isInteger(hours)) {
    return `${Math.round(hours)}h`;
  }

  return `${hours.toFixed(1)}h`;
}

function buildActivityTicks(maxSeconds) {
  const normalizedMax = Math.max(3600, Math.ceil(maxSeconds / 1800) * 1800);
  return Array.from({ length: 5 }, (_, index) => {
    const value = Math.round((normalizedMax / 4) * (4 - index));
    return {
      value,
      label: formatHoursLabel(value)
    };
  });
}

function formatCourseOwnershipDate(course) {
  if (course.grantedAt) {
    return `Mua ngày ${new Date(course.grantedAt).toLocaleDateString("vi-VN")}`;
  }

  return `Tạo ngày ${new Date(course.createdAt).toLocaleDateString("vi-VN")}`;
}

function formatCurrency(value) {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0
  }).format(value ?? 0);
}

function formatPurchaseDateTime(value) {
  return new Date(value).toLocaleString("vi-VN");
}

function getPurchaseStatusLabel(status) {
  if (status === "LatePaid") {
    return "Thanh toán muộn";
  }

  if (status === "Paid") {
    return "Đã thanh toán";
  }

  return status || "Không xác định";
}

export default function ProfilePage() {
  const { user, isAuthenticated } = useAuth();
  const isAdmin = user?.role === "Admin";
  const fullName = user?.fullName || "Người dùng";
  const email = user?.email || "Chưa có email";
  const mailtoHref = user?.email ? `mailto:${user.email}` : "#";
  const roleCode = user?.role || "Unknown";
  const roleLabel = getRoleLabel(user?.role);
  const initials = getInitials(user?.fullName, user?.email);
  const [courses, setCourses] = useState([]);
  const [favoriteCourseIds, setFavoriteCourseIds] = useState(() => loadFavoriteCourseIds());
  const [isLoadingCourses, setIsLoadingCourses] = useState(true);
  const [courseErrorMessage, setCourseErrorMessage] = useState("");
  const [purchaseHistory, setPurchaseHistory] = useState([]);
  const [isLoadingPurchaseHistory, setIsLoadingPurchaseHistory] = useState(!isAdmin);
  const [purchaseHistoryErrorMessage, setPurchaseHistoryErrorMessage] = useState("");
  const [webActivityFilter, setWebActivityFilter] = useState("7D");
  const [webActivity, setWebActivity] = useState(() => readWebActivitySeries("7D"));
  const [currentLearningProgress, setCurrentLearningProgress] = useState(() => readCurrentLearningProgress());

  useEffect(() => {
    window.localStorage.setItem(FAVORITE_COURSE_IDS_STORAGE_KEY, JSON.stringify(favoriteCourseIds));
  }, [favoriteCourseIds]);

  useEffect(() => {
    let isMounted = true;

    async function loadProfileData() {
      setIsLoadingCourses(true);
      setCourseErrorMessage("");
      setCourses([]);
      setPurchaseHistoryErrorMessage("");
      setPurchaseHistory([]);

      if (isAdmin) {
        setPurchaseHistory([]);
        setIsLoadingPurchaseHistory(false);
      } else {
        setIsLoadingPurchaseHistory(true);
      }

      if (isAdmin) {
        try {
          const items = await getAdminCourses();
          if (isMounted) {
            setCourses(items);
          }
        } catch {
          if (isMounted) {
            setCourseErrorMessage("Không thể tải dữ liệu khóa học để hiển thị trên hồ sơ.");
          }
        } finally {
          if (isMounted) {
            setIsLoadingCourses(false);
          }
        }

        return;
      }

      const [coursesResult, purchaseHistoryResult] = await Promise.allSettled([
        getPublishedCourses(),
        getPurchaseHistory()
      ]);

      if (!isMounted) {
        return;
      }

      if (coursesResult.status === "fulfilled") {
        setCourses(coursesResult.value);
      } else {
        setCourseErrorMessage("Không thể tải dữ liệu khóa học để hiển thị trên hồ sơ.");
      }

      if (purchaseHistoryResult.status === "fulfilled") {
        setPurchaseHistory(purchaseHistoryResult.value);
      } else {
        setPurchaseHistoryErrorMessage("Không thể tải lịch sử mua hàng của bạn.");
      }

      if (isMounted) {
        setIsLoadingCourses(false);
        setIsLoadingPurchaseHistory(false);
      }
    }

    loadProfileData();

    function handleStorageChange() {
      setFavoriteCourseIds(loadFavoriteCourseIds());
      setWebActivity(readWebActivitySeries(webActivityFilter));
      setCurrentLearningProgress(readCurrentLearningProgress());
    }

    window.addEventListener("storage", handleStorageChange);
    const activityRefreshIntervalId = window.setInterval(() => {
      setWebActivity(readWebActivitySeries(webActivityFilter));
    }, 15000);
    handleStorageChange();

    return () => {
      isMounted = false;
      window.clearInterval(activityRefreshIntervalId);
      window.removeEventListener("storage", handleStorageChange);
    };
  }, [isAdmin, webActivityFilter]);

  const newestCourses = [...courses]
    .sort((left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime())
    .slice(0, 3);
  const recentlyOwnedCourses = [...courses]
    .filter((course) => course.alreadyOwned)
    .sort((left, right) => new Date(right.grantedAt ?? right.createdAt).getTime() - new Date(left.grantedAt ?? left.createdAt).getTime())
    .slice(0, 3);
  const ownedCourseCount = isAdmin ? courses.length : courses.filter((course) => course.alreadyOwned).length;
  const featuredCourses = recentlyOwnedCourses.length > 0 ? recentlyOwnedCourses : newestCourses;
  const featuredSectionEyebrow = recentlyOwnedCourses.length > 0 ? "Khóa học của bạn" : "Khóa học mới";
  const featuredSectionTitle = recentlyOwnedCourses.length > 0 ? "Tiếp tục học" : "Tiếp tục khám phá";
  const savedCourses = courses.filter((course) => favoriteCourseIds.includes(course.id)).slice(0, 4);
  const maxActivitySeconds = webActivity.reduce((current, item) => Math.max(current, item.seconds), 0);
  const totalActivitySeconds = webActivity.reduce((current, item) => current + item.seconds, 0);
  const mostActiveDay = [...webActivity].sort((left, right) => right.seconds - left.seconds)[0] || null;
  const useCompactActivityChart = webActivity.length > 12;
  const activityTicks = buildActivityTicks(maxActivitySeconds);
  const chartHeight = 220;
  const chartInnerHeight = 180;
  const chartStep = useCompactActivityChart ? (webActivityFilter === "1Y" ? 54 : 28) : 72;
  const chartPaddingX = 20;
  const chartWidth = Math.max(420, webActivity.length * chartStep);
  const chartPoints = webActivity.map((activity, index) => {
    const x =
      webActivity.length === 1
        ? chartWidth / 2
        : chartPaddingX + (index / Math.max(webActivity.length - 1, 1)) * (chartWidth - chartPaddingX * 2);
    const y = chartHeight - (maxActivitySeconds > 0 ? (activity.seconds / Math.max(activityTicks[0].value, 1)) * chartInnerHeight : 0);
    return { ...activity, x, y };
  });
  const chartLinePath = chartPoints
    .map((point, index) => `${index === 0 ? "M" : "L"} ${point.x.toFixed(2)} ${point.y.toFixed(2)}`)
    .join(" ");
  const chartAreaPath = `${chartLinePath} L ${chartWidth} ${chartHeight} L 0 ${chartHeight} Z`;

  function handleRemoveFavorite(courseId) {
    setFavoriteCourseIds((current) => current.filter((id) => id !== courseId));
  }

  return (
    <Section className="section-stack profile-workspace">
      <div className="profile-shell">
        <div className="profile-shell__intro">
          <p className="profile-shell__eyebrow">Tài khoản</p>
          <h1>Hồ sơ cá nhân</h1>
          <p className="profile-shell__description">
            Thông tin trên trang này được lấy trực tiếp từ phiên đăng nhập hiện tại của bạn trong VibeCourseAI.
          </p>
        </div>

        <div className="profile-shell__hero">
          <Card className="profile-hero-card" variant="shadowed">
            <div className="profile-hero-card__identity">
              <div aria-hidden="true" className="profile-avatar">
                <span>{initials}</span>
              </div>

              <div className="profile-hero-card__copy">
                <div className="profile-hero-card__badges">
                  <span className="profile-pill">Hồ sơ thật</span>
                  <span className="profile-pill profile-pill--muted">{roleLabel}</span>
                </div>
                <h2>{fullName}</h2>
                <p>{email}</p>
              </div>
            </div>

            <div className="profile-hero-card__actions">
              <Button as={Link} to="/change-password">
                Đổi mật khẩu
              </Button>
              <Button as="a" href={mailtoHref} variant="ghost">
                Gửi email
              </Button>
            </div>
          </Card>

          <div className="profile-stat-grid">
            <Card className="profile-stat-card" variant="shadowed">
              <span className="profile-stat-card__label">Vai trò</span>
              <strong>{roleLabel}</strong>
              <p>Mã hệ thống: {roleCode}</p>
            </Card>

            <Card className="profile-stat-card" variant="shadowed">
              <span className="profile-stat-card__label">Trạng thái</span>
              <strong>{isAuthenticated ? "Đang hoạt động" : "Chưa xác thực"}</strong>
              <p>Phiên đăng nhập hiện tại</p>
            </Card>

            <Card className="profile-stat-card" variant="shadowed">
              <span className="profile-stat-card__label">Khóa học khả dụng</span>
              <strong>{ownedCourseCount}</strong>
              <p>{isAdmin ? "Dữ liệu lấy từ danh sách khóa học thật" : "Số khóa học bạn đã mua thành công"}</p>
            </Card>

          </div>
        </div>

        <div className="profile-content-grid">
          <Card className="profile-panel profile-panel--details" variant="shadowed">
            <div className="profile-panel__header">
              <p className="profile-panel__eyebrow">Thông tin chính</p>
              <h3>Chi tiết tài khoản</h3>
            </div>

            <div className="profile-detail-grid">
              <div className="profile-detail-card">
                <span className="profile-detail-card__label">Họ và tên</span>
                <strong className="profile-detail-card__value">{fullName}</strong>
                <p>Tên hiển thị hiện tại trong hệ thống.</p>
              </div>

              <div className="profile-detail-card">
                <span className="profile-detail-card__label">Email đăng nhập</span>
                <strong className="profile-detail-card__value">{email}</strong>
                <p>Email dùng để đăng nhập và nhận thông báo.</p>
              </div>

              <div className="profile-detail-card">
                <span className="profile-detail-card__label">Vai trò hiển thị</span>
                <strong className="profile-detail-card__value">{roleLabel}</strong>
                <p>Quyền truy cập được gán cho tài khoản hiện tại.</p>
              </div>

              <div className="profile-detail-card">
                <span className="profile-detail-card__label">Mã vai trò</span>
                <strong className="profile-detail-card__value">{roleCode}</strong>
                <p>Giá trị raw được trả về trực tiếp từ phiên người dùng.</p>
              </div>
            </div>
          </Card>

          <Card className="profile-panel profile-panel--context" variant="shadowed">
            <div className="profile-panel__header">
              <p className="profile-panel__eyebrow">Tác vụ nhanh</p>
              <h3>Điều hướng hữu ích</h3>
            </div>

            <div className="profile-action-list">
              {currentLearningProgress ? (
                <Link className="profile-action-card" to={`/courses/${currentLearningProgress.courseId}/learn`}>
                  <span className="profile-action-card__label">Tiếp tục học</span>
                  <strong>{currentLearningProgress.courseTitle}</strong>
                  <p>{currentLearningProgress.selectedLessonTitle}</p>
                </Link>
              ) : null}

              <Link className="profile-action-card" to="/courses">
                <span className="profile-action-card__label">Khám phá khóa học</span>
                <strong>Thư viện khóa học</strong>
                <p>
                  {isAdmin
                    ? `Hiện có ${courses.length} khóa học khả dụng để truy cập.`
                    : `Bạn hiện sở hữu ${ownedCourseCount} khóa học đã mua.`}
                </p>
              </Link>

              <Link className="profile-action-card" to="/change-password">
                <span className="profile-action-card__label">Bảo mật tài khoản</span>
                <strong>Đổi mật khẩu</strong>
                <p>Cập nhật mật khẩu để bảo vệ phiên đăng nhập hiện tại.</p>
              </Link>
            </div>
          </Card>
        </div>

        {courseErrorMessage ? <p className="ui-alert ui-alert--error">{courseErrorMessage}</p> : null}
        {purchaseHistoryErrorMessage ? <p className="ui-alert ui-alert--error">{purchaseHistoryErrorMessage}</p> : null}

        <div className="profile-analytics-grid">
          <Card className="profile-panel profile-panel--chart" variant="shadowed">
            <div className="profile-panel__header profile-panel__header--inline">
              <div>
                <p className="profile-panel__eyebrow">Hoạt động web</p>
                <h3>Thời gian hoạt động trên web</h3>
              </div>
              <div className="profile-chart-filter" role="tablist" aria-label="Lọc thời gian hoạt động">
                {WEB_ACTIVITY_FILTER_OPTIONS.map((option) => (
                  <button
                    aria-selected={webActivityFilter === option}
                    className={`profile-chart-filter__button${webActivityFilter === option ? " profile-chart-filter__button--active" : ""}`}
                    key={option}
                    onClick={() => setWebActivityFilter(option)}
                    type="button"
                  >
                    {option}
                  </button>
                ))}
              </div>
            </div>

            {webActivity.every((item) => item.seconds === 0) ? (
              <p>Chưa có đủ dữ liệu hoạt động. Biểu đồ sẽ bắt đầu ghi nhận từ lúc bạn dùng web.</p>
            ) : (
              <div className="profile-axis-chart">
                <div className="profile-axis-chart__frame">
                  <div className="profile-axis-chart__y-axis" aria-hidden="true">
                    {activityTicks.map((tick) => (
                      <span key={tick.label}>{tick.label}</span>
                    ))}
                  </div>

                  <div className="profile-axis-chart__canvas">
                    <div className="profile-axis-chart__scroll">
                      <svg
                        aria-label={`Biểu đồ thời gian hoạt động ${webActivityFilter}`}
                        className="profile-axis-chart__svg"
                        role="img"
                        viewBox={`0 0 ${chartWidth} ${chartHeight + 44}`}
                      >
                        {activityTicks.map((tick, index) => {
                          const y = (index / Math.max(activityTicks.length - 1, 1)) * chartInnerHeight;
                          return (
                            <line
                              className="profile-axis-chart__grid-line"
                              key={tick.label}
                              x1="0"
                              x2={chartWidth}
                              y1={y}
                              y2={y}
                            />
                          );
                        })}

                        <path className="profile-axis-chart__area" d={chartAreaPath} />
                        <path className="profile-axis-chart__line" d={chartLinePath} />

                        {chartPoints.map((point) => (
                          <g key={point.dayKey}>
                            <circle className="profile-axis-chart__dot" cx={point.x} cy={point.y} r="4.5" />
                            <text className="profile-axis-chart__x-label" textAnchor="middle" x={point.x} y={chartHeight + 20}>
                              {point.label}
                            </text>
                            <text className="profile-axis-chart__x-meta" textAnchor="middle" x={point.x} y={chartHeight + 36}>
                              {formatHoursLabel(point.seconds)}
                            </text>
                          </g>
                        ))}
                      </svg>
                    </div>
                  </div>
                </div>

                <div className="profile-insight-card">
                  <p className="profile-note-card__title">Gợi ý hiển thị</p>
                  <p>
                    Trong mốc
                    {" "}
                    <strong>{webActivityFilter}</strong>
                    , tổng thời gian hoạt động là
                    {" "}
                    <strong>{formatActivityDuration(totalActivitySeconds)}</strong>
                    {" "}
                    và cao nhất ở
                    {" "}
                    <strong>{mostActiveDay?.label ?? "chưa có dữ liệu"}</strong>
                    .
                  </p>
                </div>
              </div>
            )}
          </Card>

          <Card className="profile-panel profile-panel--insight" variant="shadowed">
            <div className="profile-panel__header">
              <p className="profile-panel__eyebrow">Tiến độ hiện tại</p>
              <h3>Đang học</h3>
            </div>

            {!currentLearningProgress ? (
              <p>Chưa có dữ liệu tiến độ học. Hãy mở một khóa học và bắt đầu học để hệ thống ghi nhận.</p>
            ) : (
              <>
                <div className="profile-progress-ring-card">
                  <div
                    aria-label={`Tiến độ hiện tại ${currentLearningProgress.progressPercent}%`}
                    className="profile-progress-ring"
                    style={{ "--profile-progress-value": currentLearningProgress.progressPercent }}
                  >
                    <div className="profile-progress-ring__inner">
                      <strong>{currentLearningProgress.progressPercent}%</strong>
                      <span>Hoàn thành</span>
                    </div>
                  </div>

                  <div className="profile-progress-ring-card__copy">
                    <strong>{currentLearningProgress.courseTitle}</strong>
                    <p>{currentLearningProgress.selectedLessonTitle}</p>
                    <span>
                      {currentLearningProgress.completedLessons}/{currentLearningProgress.totalLessons} bài học
                    </span>
                  </div>
                </div>

                <div className="profile-insight-card">
                  <p className="profile-note-card__title">Gợi ý hiển thị</p>
                  <p>
                    Bạn đang học ở bài
                    {" "}
                    <strong>{currentLearningProgress.selectedLessonTitle}</strong>
                    {" "}
                    trong khóa
                    {" "}
                    <strong>{currentLearningProgress.courseTitle}</strong>.
                  </p>
                </div>
              </>
            )}
          </Card>
        </div>

        <div className="profile-course-section">
          <div className="profile-section-heading">
            <div>
              <p className="profile-panel__eyebrow">{featuredSectionEyebrow}</p>
              <h3>{featuredSectionTitle}</h3>
            </div>
          </div>

          {isLoadingCourses ? (
            <Card className="profile-panel" variant="shadowed">
              <p>Đang tải danh sách khóa học...</p>
            </Card>
          ) : featuredCourses.length === 0 ? (
            <Card className="profile-panel" variant="shadowed">
              <p>Chưa có khóa học nào để hiển thị.</p>
            </Card>
          ) : (
            <div className="profile-course-grid profile-course-grid--featured">
              {featuredCourses.map((course) => (
                <article className="profile-course-card profile-course-card--featured" key={course.id}>
                  <div className="profile-course-card__media">
                    {course.thumbnailUrl ? (
                      <img alt={course.title} className="profile-course-card__image" src={course.thumbnailUrl} />
                    ) : (
                      <div className="profile-course-card__fallback">{course.title}</div>
                    )}
                    <span className="profile-course-card__badge">{course.lessonCount} bài học</span>
                  </div>

                  <div className="profile-course-card__body">
                    <p className="profile-course-card__eyebrow">{course.category || "Chưa phân loại"}</p>
                    <h4>{course.title}</h4>
                  </div>

                  <div className="profile-course-card__footer">
                    <div className="profile-course-card__meta">
                      <span>{course.moduleCount} module</span>
                      <span>{formatCourseOwnershipDate(course)}</span>
                    </div>
                    <Button as={Link} className="profile-course-card__action" to={`/courses/${course.id}/learn`}>
                      Vào học
                    </Button>
                  </div>
                </article>
              ))}
            </div>
          )}
        </div>

        <div className="profile-course-section">
          <div className="profile-section-heading">
            <div>
              <p className="profile-panel__eyebrow">Danh sách đã lưu</p>
              <h3>Để học sau</h3>
            </div>
          </div>

          {isLoadingCourses ? (
            <Card className="profile-panel" variant="shadowed">
              <p>Đang tải danh sách đã lưu...</p>
            </Card>
          ) : savedCourses.length === 0 ? (
            <Card className="profile-panel" variant="shadowed">
              <p>Bạn chưa lưu khóa học nào. Hãy nhấn biểu tượng tim ở trang khóa học để thêm vào danh sách này.</p>
            </Card>
          ) : (
            <div className="profile-course-grid profile-course-grid--saved">
              {savedCourses.map((course) => (
                <article className="profile-course-card profile-course-card--saved" key={course.id}>
                  <div className="profile-course-card__media profile-course-card__media--compact">
                    {course.thumbnailUrl ? (
                      <img alt={course.title} className="profile-course-card__image" src={course.thumbnailUrl} />
                    ) : (
                      <div className="profile-course-card__fallback">{course.title}</div>
                    )}
                  </div>

                  <div className="profile-course-card__body profile-course-card__body--compact">
                    <h4>{course.title}</h4>
                    <p>{course.moduleCount} module • {course.lessonCount} bài học</p>
                    <div className="profile-course-card__saved-actions">
                      <span className="profile-course-card__saved-tag">Đã lưu</span>
                      <Button
                        className="profile-course-card__remove"
                        onClick={() => handleRemoveFavorite(course.id)}
                        variant="ghost"
                      >
                        Bỏ lưu
                      </Button>
                    </div>
                  </div>
                </article>
              ))}
            </div>
          )}
        </div>

        {!isAdmin ? (
          <div className="profile-course-section">
            <div className="profile-section-heading">
              <div>
                <p className="profile-panel__eyebrow">Lịch sử thanh toán</p>
                <h3>Lịch sử mua hàng</h3>
              </div>
            </div>

            {isLoadingPurchaseHistory ? (
              <Card className="profile-panel" variant="shadowed">
                <p>Đang tải lịch sử mua hàng...</p>
              </Card>
            ) : purchaseHistory.length === 0 ? (
              <Card className="profile-panel" variant="shadowed">
                <p>Bạn chưa có giao dịch thanh toán thành công nào.</p>
              </Card>
            ) : (
              <Card className="profile-panel profile-purchase-panel" variant="shadowed">
                <div className="profile-purchase-list">
                  {purchaseHistory.map((item) => (
                    <article className="profile-purchase-item" key={item.paymentOrderId}>
                      <div className="profile-purchase-item__media">
                        {item.courseThumbnailUrl ? (
                          <img alt={item.courseTitle} className="profile-purchase-item__image" src={item.courseThumbnailUrl} />
                        ) : (
                          <div className="profile-purchase-item__fallback">{item.courseTitle}</div>
                        )}
                      </div>

                      <div className="profile-purchase-item__body">
                        <div className="profile-purchase-item__header">
                          <div>
                            <p className="profile-course-card__eyebrow">Đơn hàng {item.orderCode}</p>
                            <h4>{item.courseTitle}</h4>
                          </div>
                        </div>

                        <div className="profile-purchase-item__meta">
                          <span>Số tiền {formatCurrency(item.amount)}</span>
                          <span>Mua lúc {formatPurchaseDateTime(item.purchasedAt)}</span>
                          <span>{item.paidAt ? `Thanh toán lúc ${formatPurchaseDateTime(item.paidAt)}` : "Đã ghi nhận thanh toán"}</span>
                        </div>
                      </div>

                      <div className="profile-purchase-item__status-wrap">
                        <span className="profile-purchase-item__status">{getPurchaseStatusLabel(item.status)}</span>
                      </div>

                      <div className="profile-purchase-item__actions">
                        <Button as={Link} to={`/courses/${item.courseId}/learn`}>
                          Vào học
                        </Button>
                      </div>
                    </article>
                  ))}
                </div>
              </Card>
            )}
          </div>
        ) : null}
      </div>
    </Section>
  );
}
