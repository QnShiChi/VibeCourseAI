import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../auth/useAuth";
import { getAdminCourses, getPublishedCourses, publishCourse, unpublishCourse } from "../api/courseService";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import Section from "../components/ui/Section";
import { COURSE_CATEGORY_OPTIONS } from "../constants/coursePresentation";
import styles from "../styles/CoursesPage.module.css";

const ALL_COURSES_FILTER = "All";

export default function CoursesPage() {
  const { user } = useAuth();
  const isAdmin = user?.role === "Admin";
  const [courses, setCourses] = useState([]);
  const [errorMessage, setErrorMessage] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [activeCategory, setActiveCategory] = useState(ALL_COURSES_FILTER);

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

  function handleResetFilters() {
    setSearchTerm("");
    setActiveCategory(ALL_COURSES_FILTER);
  }

  const normalizedSearchTerm = searchTerm.trim().toLowerCase();
  const filteredCourses = courses.filter((course) => {
    const matchesCategory = activeCategory === ALL_COURSES_FILTER || course.category === activeCategory;
    const haystack = `${course.title} ${course.description}`.toLowerCase();
    const matchesSearch = normalizedSearchTerm.length === 0 || haystack.includes(normalizedSearchTerm);
    return matchesCategory && matchesSearch;
  });

  return (
    <Section className={styles.page}>
      <div className={styles.pageInner}>
        <section className={styles.hero}>
          <p className={styles.eyebrow}>Course discovery</p>
          <h1 className={styles.title}>
            Master the Future of <span>Creative Tech</span>
          </h1>
          <p className={styles.description}>
            Khám phá các khóa học thật của VibeCourseAI qua một bề mặt tìm kiếm gọn hơn, trực quan hơn và dễ lọc theo category.
          </p>
          <label className={styles.searchBar}>
            <span className={styles.srOnly}>Tìm khóa học</span>
            <span className={styles.searchIcon} aria-hidden="true">⌕</span>
            <input
              aria-label="Tìm khóa học"
              value={searchTerm}
              onChange={(event) => setSearchTerm(event.target.value)}
              placeholder="Search for courses, tools, or instructors..."
            />
          </label>
          <div className={styles.chipRow}>
            <button
              type="button"
              className={`${styles.chip} ${activeCategory === ALL_COURSES_FILTER ? styles.chipActive : ""}`}
              onClick={() => setActiveCategory(ALL_COURSES_FILTER)}
            >
              All Courses
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
          <div className={styles.grid}>
            {filteredCourses.map((course, index) => (
              <article key={course.id} data-testid="course-card" className={`${styles.courseCard} ${styles[`tone${index % 3}`]}`}>
                <div className={styles.cardMedia}>
                  {course.thumbnailUrl ? (
                    <img src={course.thumbnailUrl} alt={course.title} className={styles.cardImage} />
                  ) : (
                    <div className={styles.mediaFallback}>{course.title}</div>
                  )}
                </div>
                <div className={styles.cardBody}>
                  <div className={styles.cardMetaRow}>
                    <span>{course.moduleCount} modules</span>
                    <span>{course.lessonCount} lessons</span>
                  </div>
                  <h3>{course.title}</h3>
                  <p>{course.description}</p>
                </div>
                <div className={styles.cardFooter}>
                  <span className={styles.cardCategory}>{getCategoryLabel(course.category)}</span>
                  <div className={styles.cardActions}>
                    <Button as={Link} to={`/courses/${course.id}/learn`}>Xem khóa học</Button>
                    {isAdmin ? (
                      <Button onClick={() => handleTogglePublish(course)} variant="ghost">
                        {course.isPublished ? "Unpublish" : "Publish"}
                      </Button>
                    ) : null}
                  </div>
                </div>
              </article>
            ))}
          </div>
        )}
      </div>
    </Section>
  );
}

function getCategoryLabel(categoryValue) {
  return COURSE_CATEGORY_OPTIONS.find((option) => option.value === categoryValue)?.label ?? categoryValue ?? "Uncategorized";
}
