import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
  deleteModerationComment,
  getModerationComments,
  getPositiveCourseHighlights,
  pinComment
} from "../api/adminCommentModerationService";
import { updateUserActive } from "../api/userService";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import Section from "../components/ui/Section";

function formatDateTime(value) {
  return new Intl.DateTimeFormat("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit"
  }).format(new Date(value));
}

export default function CommentModerationPage() {
  const [comments, setComments] = useState([]);
  const [courseHighlights, setCourseHighlights] = useState([]);
  const [activeMode, setActiveMode] = useState("negative");
  const [authorName, setAuthorName] = useState("");
  const [appliedAuthorName, setAppliedAuthorName] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");
  const [activeAction, setActiveAction] = useState({ type: "", id: "" });

  async function loadComments() {
    setIsLoading(true);
    setErrorMessage("");
    try {
      if (activeMode === "positive-courses") {
        setCourseHighlights(await getPositiveCourseHighlights());
        setComments([]);
      } else {
        setComments(await getModerationComments({
          sentiment: activeMode,
          authorName: appliedAuthorName
        }));
        setCourseHighlights([]);
      }
    } catch {
      setErrorMessage(activeMode === "positive-courses"
        ? "Không thể tải danh sách khóa học tích cực nổi bật."
        : `Không thể tải danh sách bình luận ${activeMode === "negative" ? "tiêu cực" : "tích cực"}.`);
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadComments();
  }, [activeMode, appliedAuthorName]);

  async function handleDeleteComment(commentId, lessonId) {
    setErrorMessage("");
    setActiveAction({ type: "delete", id: commentId });

    try {
      await deleteModerationComment(commentId, lessonId);
      setComments((current) => current.filter((item) => item.commentId !== commentId));
    } catch {
      setErrorMessage("Không thể xóa bình luận.");
    } finally {
      setActiveAction({ type: "", id: "" });
    }
  }

  async function handleBanUser(commentId, userId) {
    setErrorMessage("");
    setActiveAction({ type: "ban", id: commentId });

    try {
      await updateUserActive(userId, false);
      setComments((current) => current.filter((item) => item.commentId !== commentId));
    } catch {
      setErrorMessage("Không thể khóa tài khoản người dùng.");
    } finally {
      setActiveAction({ type: "", id: "" });
    }
  }

  async function handlePinComment(commentId) {
    setErrorMessage("");
    setActiveAction({ type: "pin", id: commentId });

    try {
      await pinComment(commentId);
      await loadComments();
    } catch {
      setErrorMessage("Không thể đẩy bình luận lên trước.");
    } finally {
      setActiveAction({ type: "", id: "" });
    }
  }

  function handleFilterSubmit(event) {
    event.preventDefault();
    setAppliedAuthorName(authorName);
  }

  return (
    <Section className="admin-page admin-page--stack">
      <div className="admin-page__hero">
        <div>
          <p className="admin-page__eyebrow">Moderation queue</p>
          <h1>Điều phối bình luận</h1>
          <p className="admin-page__description">
            Theo dõi cả bình luận tiêu cực lẫn tích cực, lọc theo người bình luận và xử lý các điểm cần ưu tiên cho vận hành sản phẩm.
          </p>
        </div>
        <div className="admin-page__hero-actions">
          <Button as={Link} to="/dashboard" variant="ghost">Quay lại dashboard</Button>
          <Button onClick={() => void loadComments()}>{isLoading ? "Đang tải..." : "Làm mới dữ liệu"}</Button>
        </div>
      </div>

      <Card className="admin-panel admin-panel--toolbar" variant="shadowed">
        <div className="admin-toolbar__filters">
          <Button
            onClick={() => setActiveMode("negative")}
            variant={activeMode === "negative" ? "primary" : "ghost"}
          >
            Bình luận tiêu cực
          </Button>
          <Button
            onClick={() => setActiveMode("positive")}
            variant={activeMode === "positive" ? "primary" : "ghost"}
          >
            Bình luận tích cực
          </Button>
          <Button
            onClick={() => setActiveMode("positive-courses")}
            variant={activeMode === "positive-courses" ? "primary" : "ghost"}
          >
            Khóa học tích cực nổi bật
          </Button>
        </div>

        {activeMode !== "positive-courses" ? (
          <form className="admin-toolbar__filters" onSubmit={handleFilterSubmit}>
            <input
              className="ui-input"
              onChange={(event) => setAuthorName(event.target.value)}
              placeholder="Lọc theo tên người bình luận..."
              value={authorName}
            />
            <Button type="submit" variant="ghost">Lọc</Button>
          </form>
        ) : null}
      </Card>

      {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

      <Card className="admin-table-card" variant="shadowed">
        <div className="admin-table">
          {activeMode === "positive-courses" ? (
            <>
              <div className="admin-table__header admin-user-row">
                <span>Khóa học</span>
                <span>Tổng bình luận</span>
                <span>Bình luận tích cực</span>
                <span>Tỉ lệ tích cực</span>
                <span>Bình luận gần nhất</span>
                <span>Thời gian</span>
              </div>

              {isLoading ? (
                <div className="admin-table__empty">Đang tải khóa học tích cực nổi bật...</div>
              ) : courseHighlights.length === 0 ? (
                <div className="admin-table__empty">Chưa có khóa học nào có bình luận để tổng hợp.</div>
              ) : (
                courseHighlights.map((course) => (
                  <div className="admin-table__row admin-user-row" key={course.courseId}>
                    <span>{course.courseTitle}</span>
                    <span>{course.totalCommentCount}</span>
                    <span>{course.positiveCommentCount}</span>
                    <span>{Math.round(course.positiveRatio * 100)}%</span>
                    <span>{course.latestPositiveCommentContent || "--"}</span>
                    <span>{course.latestPositiveCommentAt ? formatDateTime(course.latestPositiveCommentAt) : "--"}</span>
                  </div>
                ))
              )}
            </>
          ) : (
            <>
              <div className="admin-table__header admin-user-row">
                <span>Người dùng</span>
                <span>Khóa học</span>
                <span>Bài học</span>
                <span>Bình luận</span>
                <span>Thời gian</span>
                <span>Điều khiển</span>
              </div>

              {isLoading ? (
                <div className="admin-table__empty">Đang tải bình luận {activeMode === "negative" ? "tiêu cực" : "tích cực"}...</div>
              ) : comments.length === 0 ? (
                <div className="admin-table__empty">
                  Không có bình luận {activeMode === "negative" ? "tiêu cực" : "tích cực"} nào khớp bộ lọc hiện tại.
                </div>
              ) : (
                comments.map((comment) => {
                  const isDeleting = activeAction.type === "delete" && activeAction.id === comment.commentId;
                  const isBanning = activeAction.type === "ban" && activeAction.id === comment.commentId;
                  const isPinning = activeAction.type === "pin" && activeAction.id === comment.commentId;

                  return (
                    <div className="admin-table__row admin-user-row" key={comment.commentId}>
                      <div className="admin-user-cell">
                        <div className="admin-avatar">
                          {(comment.authorName || "U").split(/\s+/).slice(0, 2).map((part) => part[0] ?? "").join("").toUpperCase()}
                        </div>
                        <div>
                          <strong>{comment.authorName}</strong>
                          <span>{comment.authorEmail}</span>
                        </div>
                      </div>
                      <span>{comment.courseTitle}</span>
                      <span>{comment.lessonTitle}</span>
                      <span>{comment.content}</span>
                      <span>{formatDateTime(comment.createdAt)}</span>
                      <div className="admin-table__actions">
                        {activeMode === "negative" ? (
                          <>
                            <Button
                              disabled={Boolean(activeAction.id)}
                              onClick={() => void handleDeleteComment(comment.commentId, comment.lessonId)}
                              variant="ghost"
                            >
                              {isDeleting ? "Đang xóa..." : "Xóa bình luận"}
                            </Button>
                            <Button
                              disabled={Boolean(activeAction.id)}
                              onClick={() => void handleBanUser(comment.commentId, comment.authorUserId)}
                            >
                              {isBanning ? "Đang khóa..." : "Khóa tài khoản"}
                            </Button>
                          </>
                        ) : (
                          <Button
                            disabled={Boolean(activeAction.id)}
                            onClick={() => void handlePinComment(comment.commentId)}
                          >
                            {isPinning ? "Đang đẩy..." : "Đẩy lên trước"}
                          </Button>
                        )}
                      </div>
                    </div>
                  );
                })
              )}
            </>
          )}
        </div>
      </Card>
    </Section>
  );
}
