import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/useAuth";
import { getAdminCourses, getPublishedCourses, publishCourse, unpublishCourse } from "../api/courseService";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import Section from "../components/ui/Section";
import { COURSE_CATEGORY_OPTIONS } from "../constants/coursePresentation";
import { useTheme } from "../theme/ThemeContext";
import styles from "../styles/CoursesPage.module.css";

const ALL_COURSES_FILTER = "All";
const FAVORITE_COURSE_IDS_STORAGE_KEY = "favorite-course-ids";

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

export default function CoursesPage() {
  const { user } = useAuth();
  const { theme } = useTheme();
  const isAdmin = user?.role === "Admin";
  const [courses, setCourses] = useState([]);
  const [errorMessage, setErrorMessage] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [activeCategory, setActiveCategory] = useState(ALL_COURSES_FILTER);
  const [favoriteCourseIds, setFavoriteCourseIds] = useState(() => loadFavoriteCourseIds());
  const [sortOption, setSortOption] = useState("latest");

  useEffect(() => {
    loadCourses();
  }, [isAdmin]);

  useEffect(() => {
    window.localStorage.setItem(FAVORITE_COURSE_IDS_STORAGE_KEY, JSON.stringify(favoriteCourseIds));
  }, [favoriteCourseIds]);

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

  function handleResetFilters() {
    setSearchTerm("");
    setActiveCategory(ALL_COURSES_FILTER);
    setSortOption("latest");
  }

  function handleToggleFavorite(courseId) {
    setFavoriteCourseIds((current) =>
      current.includes(courseId) ? current.filter((id) => id !== courseId) : [...current, courseId]
    );
  }

  const normalizedSearchTerm = searchTerm.trim().toLowerCase();
  const visibleCourses = courses.filter((course) => {
    const matchesCategory = activeCategory === ALL_COURSES_FILTER || course.category === activeCategory;
    const haystack = `${course.title} ${course.description}`.toLowerCase();
    const matchesSearch = normalizedSearchTerm.length === 0 || haystack.includes(normalizedSearchTerm);
    return matchesCategory && matchesSearch;
  });

  const filteredCourses = sortCourses(visibleCourses, sortOption);
  const featuredCourses = filteredCourses.slice(0, 3);
  const heroCourse = filteredCourses.length >= 3 ? featuredCourses[0] : null;
  const spotlightCourses = featuredCourses.slice(1, 3);
  const recommendationCourse = filteredCourses.length >= 3 ? filteredCourses[1] ?? null : null;

  return (
    <div className={styles.coursesPage} data-testid="courses-page-shell" data-theme={theme}>
      <Section className={styles.page}>
        <div className={styles.pageInner}>
          <section className={styles.hero}>
            <div className={styles.heroBackdrop}>
              <div className={styles.heroOverlay} aria-hidden="true" />
              <div className={styles.heroContent}>
                <h1 className={styles.title}>Khám phá tương lai của tri thức cùng AI</h1>
                <label className={styles.searchBar}>
                  <span className={styles.srOnly}>Tìm khóa học</span>
                  <span className={styles.searchIcon} aria-hidden="true">⌕</span>
                  <input
                    aria-label="Tìm khóa học"
                    value={searchTerm}
                    onChange={(event) => setSearchTerm(event.target.value)}
                    placeholder="Bạn muốn học gì hôm nay?"
                  />
                  <button className={styles.searchButton} type="button">Tìm kiếm</button>
                </label>
              </div>
            </div>
          </section>

          <section className={styles.filterSection}>
            <div className={styles.chipRow}>
              <button
                type="button"
                className={`${styles.chip} ${activeCategory === ALL_COURSES_FILTER ? styles.chipActive : ""}`}
                onClick={() => setActiveCategory(ALL_COURSES_FILTER)}
              >
                Tất cả
              </button>
              {COURSE_CATEGORY_OPTIONS.map((option) => (
                <button
                  key={option.value}
                  type="button"
                  className={`${styles.chip} ${activeCategory === option.value ? styles.chipActive : ""}`}
                  onClick={() => setActiveCategory(option.value)}
                >
                  {option.label}
                </button>
              ))}
            </div>
          </section>

          {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

          {isLoading ? (
            <Card variant="shadowed">
              <p>Đang tải danh sách khóa học...</p>
            </Card>
          ) : filteredCourses.length === 0 ? (
            <Card className={styles.emptyState} variant="shadowed">
              <h2>Không có khóa học phù hợp</h2>
              <p>Thử đổi category hoặc từ khóa tìm kiếm để mở rộng kết quả.</p>
              <Button onClick={handleResetFilters}>Xóa bộ lọc</Button>
            </Card>
          ) : (
            <>
              {heroCourse ? (
                <section className={styles.featureSection}>
                  <h2 className={styles.sectionTitle}>Khóa học nổi bật</h2>
                  <div className={styles.featureGrid}>
                    <article className={`${styles.featureCard} ${styles.featureCardLarge}`}>
                      <div className={styles.featureMedia}>
                        {heroCourse.thumbnailUrl ? (
                          <img src={heroCourse.thumbnailUrl} alt={heroCourse.title} className={styles.cardImage} />
                        ) : (
                          <div className={styles.mediaFallback}>{heroCourse.title}</div>
                        )}
                        <div className={styles.featureShade} aria-hidden="true" />
                      </div>
                      <div className={styles.featureContent}>
                        <span className={styles.featureTag}>Best seller</span>
                        {!heroCourse.thumbnailUrl && (
                          <>
                            <h3>{heroCourse.title}</h3>
                            <p>{heroCourse.description}</p>
                          </>
                        )}
                        <div className={styles.featureMetaRow}>
                          <span>{heroCourse.moduleCount} modules</span>
                          <span>{heroCourse.lessonCount} bài học</span>
                        </div>
                        <Button as={Link} to={`/courses/${heroCourse.id}/learn`} className={styles.featureButton}>
                          Đăng ký ngay
                        </Button>
                      </div>
                    </article>

                    <div className={styles.featureStack}>
                      {spotlightCourses.map((course) => (
                        <article className={styles.featureCard} key={course.id}>
                          <div className={styles.featureMedia}>
                            {course.thumbnailUrl ? (
                              <img src={course.thumbnailUrl} alt={course.title} className={styles.cardImage} />
                            ) : (
                              <div className={styles.mediaFallback}>{course.title}</div>
                            )}
                            <div className={styles.featureShade} aria-hidden="true" />
                          </div>
                          {!course.thumbnailUrl && (
                            <div className={styles.featureContentCompact}>
                              <h3>{course.title}</h3>
                              <p>{course.description}</p>
                            </div>
                          )}
                        </article>
                      ))}
                    </div>
                  </div>
                </section>
              ) : null}

              {recommendationCourse ? (
                <section className={styles.recommendSection}>
                  <div className={styles.recommendCard}>
                    <div className={styles.recommendIcon} aria-hidden="true">✦</div>
                    <div className={styles.recommendCopy}>
                      <h3>Gợi ý riêng cho bạn</h3>
                      <p>
                        Dựa trên lịch sử của bạn, chúng tôi nghĩ bạn sẽ thích khóa học
                        {" "}
                        <strong>"{recommendationCourse.title}"</strong>.
                      </p>
                    </div>
                    <Button as={Link} to={`/courses/${recommendationCourse.id}/learn`} className={styles.recommendButton}>
                      Xem gợi ý
                    </Button>
                  </div>
                </section>
              ) : null}

              <section className={styles.catalogSection}>
                <div className={styles.catalogHeader}>
                  <h2 className={styles.sectionTitle}>Tất cả khóa học</h2>
                  <label className={styles.catalogSortControl}>
                    <span className={styles.catalogMeta}>Sắp xếp theo:</span>
                    <select
                      aria-label="Sắp xếp khóa học"
                      className={styles.catalogSortSelect}
                      onChange={(event) => setSortOption(event.target.value)}
                      value={sortOption}
                    >
                      <option value="latest">Mới nhất</option>
                      <option value="title">Tên A-Z</option>
                      <option value="lessons">Nhiều bài học</option>
                    </select>
                  </label>
                </div>

                <div className={styles.grid} data-testid="course-grid">
                  {filteredCourses.map((course, index) => (
                    <article
                      key={course.id}
                      data-testid="course-card"
                      data-layout="compact"
                      className={`${styles.courseCard} ${styles[`tone${index % 3}`]}`}
                    >
                      <div className={styles.cardMedia}>
                        <span className={styles.cardEyebrow}>Khóa học</span>
                        {course.thumbnailUrl ? (
                          <img src={course.thumbnailUrl} alt={course.title} className={styles.cardImage} />
                        ) : (
                          <div className={styles.mediaFallback}>{course.title}</div>
                        )}
                        <button
                          aria-label={`Lưu ${course.title}`}
                          aria-pressed={favoriteCourseIds.includes(course.id)}
                          className={`${styles.cardWishButton}${favoriteCourseIds.includes(course.id) ? ` ${styles.cardWishButtonActive}` : ""}`}
                          onClick={() => handleToggleFavorite(course.id)}
                          type="button"
                        >
                          {favoriteCourseIds.includes(course.id) ? "♥" : "♡"}
                        </button>
                      </div>
                      <div className={styles.cardBody}>
                        <span className={styles.cardCategoryLabel}>{getCategoryLabel(course.category)}</span>
                        <h3>{course.title}</h3>
                        <p>{course.description}</p>
                        <div className={styles.cardMetaRow}>
                          <span className={styles.cardRating}>★ 4.9</span>
                          <span className={styles.cardMetaText}>({course.lessonCount * 120} học viên)</span>
                        </div>
                      </div>
                      <div className={styles.cardFooter}>
                        <div className={styles.cardPrice}>Từ 599.000đ</div>
                        <div className={styles.cardActions}>
                          <Button as={Link} to={`/courses/${course.id}/learn`} className={styles.primaryAction}>
                            Xem khóa học
                          </Button>
                          {isAdmin ? (
                            <Button onClick={() => handleTogglePublish(course)} variant="ghost" className={styles.secondaryAction}>
                              {course.isPublished ? "Unpublish" : "Publish"}
                            </Button>
                          ) : null}
                        </div>
                      </div>
                    </article>
                  ))}
                </div>
              </section>
            </>
          )}
        </div>
      </Section>
    </div>
  );
}

function getCategoryLabel(categoryValue) {
  return COURSE_CATEGORY_OPTIONS.find((option) => option.value === categoryValue)?.label ?? categoryValue ?? "Uncategorized";
}

function sortCourses(courses, sortOption) {
  const items = [...courses];

  switch (sortOption) {
    case "title":
      return items.sort((left, right) => left.title.localeCompare(right.title, "vi"));
    case "lessons":
      return items.sort((left, right) => right.lessonCount - left.lessonCount);
    case "latest":
    default:
      return items;
  }
}
