import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getGenerationJobDetail, getGenerationJobs } from "../api/generationJobService";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import PageHeader from "../components/ui/PageHeader";
import Section from "../components/ui/Section";

function getStatusClassName(status) {
  return `status-badge status-badge--${status.toLowerCase()}`;
}

export default function GenerationJobsPage() {
  const [jobs, setJobs] = useState([]);
  const [selected, setSelected] = useState(null);
  const [errorMessage, setErrorMessage] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    loadJobs();
  }, []);

  async function loadJobs(selectedId = null) {
    setIsLoading(true);
    setErrorMessage("");
    try {
      const items = await getGenerationJobs();
      setJobs(items);
      const targetId = selectedId ?? items[0]?.id;
      if (targetId) {
        const detail = await getGenerationJobDetail(targetId);
        setSelected(detail);
      } else {
        setSelected(null);
      }
    } catch {
      setErrorMessage("Không thể tải danh sách generation jobs.");
    } finally {
      setIsLoading(false);
    }
  }

  async function handleSelect(id) {
    setErrorMessage("");
    try {
      const detail = await getGenerationJobDetail(id);
      setSelected(detail);
    } catch {
      setErrorMessage("Không thể tải chi tiết generation job.");
    }
  }

  return (
    <Section className="section-stack admin-workspace">
      <PageHeader
        eyebrow="Admin"
        title="Generation Jobs"
        description="Theo dõi tiến trình sinh course structure bằng backend ASP.NET Core Web API và OpenRouter từ các đề cương đã import."
      />

      {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

      <div className="split-layout split-layout--balanced">
        <Card className="admin-panel-card admin-panel-card--list" variant="shadowed">
          <h2>Danh sách job</h2>
          {isLoading ? (
            <p>Đang tải generation jobs...</p>
          ) : jobs.length === 0 ? (
            <div className="empty-state compact-empty-state">
              <p>Chưa có generation job nào được tạo.</p>
            </div>
          ) : (
            <div className="list-stack">
              {jobs.map((job) => (
                <button
                  className={`list-item-button admin-list-item${selected?.id === job.id ? " list-item-button--active" : ""}`}
                  key={job.id}
                  onClick={() => handleSelect(job.id)}
                  type="button"
                >
                  <div className="list-item-button__header list-item-button__header--wrap">
                    <strong>{job.syllabusTitle}</strong>
                    <span className={getStatusClassName(job.status)}>{job.status}</span>
                  </div>
                  <span>{job.courseTitle || "Chưa tạo course"}</span>
                </button>
              ))}
            </div>
          )}
        </Card>

        <Card className="admin-panel-card admin-panel-card--detail" variant="shadowed">
          <div className="detail-header">
            <h2>Chi tiết job</h2>
            <div className="detail-actions">
              {selected?.courseId ? (
                <Button as={Link} to={`/admin/courses/${selected.courseId}`} variant="ghost">
                  Xem cấu trúc course
                </Button>
              ) : null}
              {selected ? <span className={getStatusClassName(selected.status)}>{selected.status}</span> : null}
            </div>
          </div>

          {selected ? (
            <div className="section-stack">
              <div className="info-grid two-column-grid">
                <div className="profile-detail">
                  <span className="profile-detail__label">Đề cương nguồn</span>
                  <span className="profile-detail__value">{selected.syllabusTitle}</span>
                </div>
                <div className="profile-detail">
                  <span className="profile-detail__label">Course đã tạo</span>
                  <span className="profile-detail__value">{selected.courseTitle || "Chưa có"}</span>
                </div>
                <div className="profile-detail">
                  <span className="profile-detail__label">Người tạo job</span>
                  <span className="profile-detail__value">{selected.createdByName || "Admin"}</span>
                </div>
                <div className="profile-detail">
                  <span className="profile-detail__label">Thời gian tạo</span>
                  <span className="profile-detail__value">{new Date(selected.createdAt).toLocaleString()}</span>
                </div>
                <div className="profile-detail">
                  <span className="profile-detail__label">Bắt đầu xử lý</span>
                  <span className="profile-detail__value">{selected.startedAt ? new Date(selected.startedAt).toLocaleString() : "Chưa bắt đầu"}</span>
                </div>
                <div className="profile-detail">
                  <span className="profile-detail__label">Hoàn thành</span>
                  <span className="profile-detail__value">{selected.completedAt ? new Date(selected.completedAt).toLocaleString() : "Chưa hoàn thành"}</span>
                </div>
              </div>

              <div className="profile-detail">
                <span className="profile-detail__label">Lỗi</span>
                <span className="profile-detail__value">{selected.errorMessage || "Không có lỗi được ghi nhận."}</span>
              </div>
            </div>
          ) : (
            <div className="empty-state compact-empty-state">
              <p>Chọn một generation job để xem chi tiết.</p>
            </div>
          )}
        </Card>
      </div>
    </Section>
  );
}
