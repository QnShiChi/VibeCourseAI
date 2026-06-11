import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { deleteNegativeComment, getNegativeComments } from "../api/adminCommentModerationService";
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
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");
  const [activeAction, setActiveAction] = useState({ type: "", id: "" });

  async function loadComments() {
    setIsLoading(true);
    setErrorMessage("");
    try {
      setComments(await getNegativeComments());
    } catch {
      setErrorMessage("Không thể tải danh sách bình luận tiêu cực.");
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadComments();
  }, []);

  async function handleDeleteComment(commentId, lessonId) {
    setErrorMessage("");
    setActiveAction({ type: "delete", id: commentId });

    try {
      await deleteNegativeComment(commentId, lessonId);
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

  return (
    <Section className="admin-page admin-page--stack">
      <div className="admin-page__hero">
        <div>
          <p className="admin-page__eyebrow">Moderation queue</p>
          <h1>Điều phối bình luận tiêu cực</h1>
          <p className="admin-page__description">
            Xem các bình luận có cảm xúc tiêu cực, xác định ngữ cảnh khóa học và xử lý nhanh bằng xóa bình luận hoặc khóa tài khoản.
          </p>
        </div>
        <div className="admin-page__hero-actions">
          <Button as={Link} to="/dashboard" variant="ghost">Quay lại dashboard</Button>
          <Button onClick={() => void loadComments()}>{isLoading ? "Đang tải..." : "Làm mới dữ liệu"}</Button>
        </div>
      </div>

      <div className="admin-overview-grid">
        <Card className="admin-stat-card" variant="shadowed">
          <span className="admin-stat-card__label">Cần xử lý</span>
          <strong>{comments.length}</strong>
        </Card>
      </div>

      {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

      <Card className="admin-table-card" variant="shadowed">
        <div className="admin-table">
          <div className="admin-table__header admin-user-row">
            <span>Người dùng</span>
            <span>Khóa học</span>
            <span>Bài học</span>
            <span>Bình luận</span>
            <span>Thời gian</span>
            <span>Điều khiển</span>
          </div>

          {isLoading ? (
            <div className="admin-table__empty">Đang tải bình luận tiêu cực...</div>
          ) : comments.length === 0 ? (
            <div className="admin-table__empty">Không còn bình luận tiêu cực nào cần xử lý.</div>
          ) : (
            comments.map((comment) => {
              const isDeleting = activeAction.type === "delete" && activeAction.id === comment.commentId;
              const isBanning = activeAction.type === "ban" && activeAction.id === comment.commentId;

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
                  </div>
                </div>
              );
            })
          )}
        </div>
      </Card>
    </Section>
  );
}
