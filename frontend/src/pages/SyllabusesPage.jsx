import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getGenerationJobs } from "../api/generationJobService";
import { deleteSyllabus, generateSyllabusCourse, getSyllabusDetail, getSyllabuses, importSyllabus } from "../api/syllabusService";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import FormField from "../components/ui/FormField";
import PageHeader from "../components/ui/PageHeader";
import Section from "../components/ui/Section";

export default function SyllabusesPage() {
  const [formData, setFormData] = useState({ title: "", description: "", file: null });
  const [items, setItems] = useState([]);
  const [selected, setSelected] = useState(null);
  const [completedSyllabusIds, setCompletedSyllabusIds] = useState(new Set());
  const [message, setMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isGenerating, setIsGenerating] = useState(false);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    loadItems();
  }, []);

  async function loadItems(selectedId = null) {
    setIsLoading(true);
    try {
      const [syllabuses, jobs] = await Promise.all([getSyllabuses(), getGenerationJobs()]);
      setItems(syllabuses);
      setCompletedSyllabusIds(new Set(jobs.filter((job) => job.status === "Completed").map((job) => job.syllabusId)));
      const targetId = selectedId ?? syllabuses[0]?.id;
      if (targetId) {
        const detail = await getSyllabusDetail(targetId);
        setSelected(detail);
      } else {
        setSelected(null);
      }
    } catch {
      setErrorMessage("Không thể tải danh sách đề cương.");
    } finally {
      setIsLoading(false);
    }
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setMessage("");
    setErrorMessage("");

    if (!formData.title || !formData.description || !formData.file) {
      setErrorMessage("Vui lòng nhập đầy đủ tiêu đề, mô tả và chọn file đề cương.");
      return;
    }

    const payload = new FormData();
    payload.append("title", formData.title);
    payload.append("description", formData.description);
    payload.append("file", formData.file);

    setIsSubmitting(true);
    try {
      const created = await importSyllabus(payload);
      await loadItems(created.id);
      setMessage("Import đề cương thành công.");
      setFormData({ title: "", description: "", file: null });
      const fileInput = document.getElementById("syllabus-file");
      if (fileInput) {
        fileInput.value = "";
      }
    } catch (error) {
      setErrorMessage(error?.response?.data?.message ?? "Không thể import đề cương.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleSelect(id) {
    setErrorMessage("");
    try {
      const detail = await getSyllabusDetail(id);
      setSelected(detail);
    } catch {
      setErrorMessage("Không thể tải chi tiết đề cương.");
    }
  }

  async function handleGenerate() {
    if (!selected || completedSyllabusIds.has(selected.id)) {
      return;
    }

    setMessage("");
    setErrorMessage("");
    setIsGenerating(true);
    try {
      const result = await generateSyllabusCourse(selected.id);
      setCompletedSyllabusIds((current) => new Set([...current, selected.id]));
      setMessage(`Đã tạo course structure bằng AI cho khóa học: ${result.courseTitle}.`);
    } catch (error) {
      setErrorMessage(error?.response?.data?.message ?? "Không thể generate khóa học từ đề cương.");
    } finally {
      setIsGenerating(false);
    }
  }

  async function handleDelete(id) {
    setMessage("");
    setErrorMessage("");
    try {
      await deleteSyllabus(id);
      setMessage("Đã xóa đề cương.");
      await loadItems(selected?.id === id ? null : selected?.id);
    } catch {
      setErrorMessage("Không thể xóa đề cương.");
    }
  }

  const hasCompletedGeneration = selected ? completedSyllabusIds.has(selected.id) : false;

  return (
    <Section className="section-stack">
      <PageHeader
        eyebrow="Admin"
        title="Đề cương"
        description="Import đề cương để chuẩn bị dữ liệu đầu vào cho bước sinh khóa học bằng backend ASP.NET Core Web API."
      />

      <div className="split-layout">
        <Card className="section-stack" variant="shadowed">
          <h2>Import đề cương</h2>
          <form className="auth-form" onSubmit={handleSubmit}>
            <FormField id="syllabus-title" label="Tiêu đề">
              <input
                className="ui-input"
                id="syllabus-title"
                value={formData.title}
                onChange={(event) => setFormData((current) => ({ ...current, title: event.target.value }))}
              />
            </FormField>

            <FormField id="syllabus-description" label="Mô tả">
              <textarea
                className="ui-input ui-textarea"
                id="syllabus-description"
                rows="4"
                value={formData.description}
                onChange={(event) => setFormData((current) => ({ ...current, description: event.target.value }))}
              />
            </FormField>

            <FormField id="syllabus-file" label="File đề cương">
              <input
                accept=".pdf,.docx,.txt"
                className="ui-input"
                id="syllabus-file"
                type="file"
                onChange={(event) => setFormData((current) => ({ ...current, file: event.target.files?.[0] ?? null }))}
              />
            </FormField>

            {message ? <p className="ui-alert ui-alert--success">{message}</p> : null}
            {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

            <Button disabled={isSubmitting} type="submit">
              {isSubmitting ? "Đang import..." : "Import đề cương"}
            </Button>
          </form>
        </Card>

        <div className="section-stack">
          <Card variant="shadowed">
            <h2>Danh sách đề cương</h2>
            {isLoading ? (
              <p>Đang tải danh sách đề cương...</p>
            ) : items.length === 0 ? (
              <div className="empty-state compact-empty-state">
                <p>Chưa có đề cương nào được import.</p>
              </div>
            ) : (
              <div className="list-stack">
                {items.map((item) => (
                  <button
                    className={`list-item-button${selected?.id === item.id ? " list-item-button--active" : ""}`}
                    key={item.id}
                    onClick={() => handleSelect(item.id)}
                    type="button"
                  >
                    <div className="list-item-button__header">
                      <strong>{item.title}</strong>
                      <span className="ui-badge">{item.fileType}</span>
                    </div>
                    <span>{item.originalFileName}</span>
                  </button>
                ))}
              </div>
            )}
          </Card>

          <Card variant="shadowed">
            <div className="detail-header">
              <h2>Chi tiết đề cương</h2>
              {selected ? (
                <div className="detail-actions">
                  <Button as={Link} to="/admin/generation-jobs" variant="ghost">
                    Xem jobs
                  </Button>
                  <Button
                    disabled={isGenerating || hasCompletedGeneration}
                    onClick={handleGenerate}
                  >
                    {hasCompletedGeneration
                      ? "Đề cương này đã generate"
                      : isGenerating
                        ? "Đang generate..."
                        : "Generate khóa học"}
                  </Button>
                  <Button onClick={() => handleDelete(selected.id)} variant="ghost">
                    Xóa đề cương
                  </Button>
                </div>
              ) : null}
            </div>
            {selected ? (
              <div className="section-stack">
                <div className="info-grid two-column-grid">
                  <div className="profile-detail">
                    <span className="profile-detail__label">Tiêu đề</span>
                    <span className="profile-detail__value">{selected.title}</span>
                  </div>
                  <div className="profile-detail">
                    <span className="profile-detail__label">Loại file</span>
                    <span className="profile-detail__value">{selected.fileType}</span>
                  </div>
                  <div className="profile-detail">
                    <span className="profile-detail__label">Tên file gốc</span>
                    <span className="profile-detail__value">{selected.originalFileName}</span>
                  </div>
                  <div className="profile-detail">
                    <span className="profile-detail__label">Kích thước</span>
                    <span className="profile-detail__value">{selected.fileSize} bytes</span>
                  </div>
                </div>
                <div className="profile-detail">
                  <span className="profile-detail__label">Mô tả</span>
                  <span className="profile-detail__value">{selected.description}</span>
                </div>
                <div className="profile-detail">
                  <span className="profile-detail__label">Nội dung đã trích</span>
                  <pre className="text-preview">{selected.extractedText}</pre>
                </div>
              </div>
            ) : (
              <div className="empty-state compact-empty-state">
                <p>Chọn một đề cương để xem chi tiết và nội dung đã trích.</p>
              </div>
            )}
          </Card>
        </div>
      </div>
    </Section>
  );
}
