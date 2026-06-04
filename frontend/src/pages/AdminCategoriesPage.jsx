import { useEffect, useMemo, useState } from "react";
import {
  createCategory,
  deleteCategory,
  getAdminCategories,
  reorderCategories,
  updateCategory
} from "../api/categoryService";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import Section from "../components/ui/Section";

const STATUS_OPTIONS = [
  { value: "Visible", label: "Hiển thị" },
  { value: "Hidden", label: "Ẩn" },
  { value: "Archived", label: "Lưu trữ" }
];

const SORT_OPTIONS = [
  { value: "latest", label: "Mới nhất" },
  { value: "alpha", label: "A-Z" },
  { value: "manual", label: "Thủ công" }
];

const INITIAL_FORM = {
  id: "",
  name: "",
  description: "",
  status: "Visible"
};

function getStatusLabel(status) {
  return STATUS_OPTIONS.find((option) => option.value === status)?.label ?? status;
}

function formatDate(value) {
  return new Intl.DateTimeFormat("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric"
  }).format(new Date(value));
}

export default function AdminCategoriesPage() {
  const [categories, setCategories] = useState([]);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
  const [sortMode, setSortMode] = useState("latest");
  const [formState, setFormState] = useState(INITIAL_FORM);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  async function loadCategories() {
    setIsLoading(true);
    setErrorMessage("");
    try {
      const data = await getAdminCategories({
        status: statusFilter,
        search: searchTerm.trim() || undefined,
        sort: sortMode
      });
      setCategories(data);
    } catch {
      setErrorMessage("Không thể tải danh sách category.");
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadCategories();
  }, [statusFilter, sortMode]);

  async function handleSearchSubmit(event) {
    event.preventDefault();
    await loadCategories();
  }

  function resetForm() {
    setFormState(INITIAL_FORM);
  }

  function handleEdit(category) {
    setFormState({
      id: category.id,
      name: category.name,
      description: category.description,
      status: category.status
    });
    setSuccessMessage("");
    setErrorMessage("");
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setIsSaving(true);
    setErrorMessage("");
    setSuccessMessage("");

    try {
      if (formState.id) {
        await updateCategory(formState.id, {
          name: formState.name,
          description: formState.description,
          status: formState.status
        });
        setSuccessMessage("Đã cập nhật category.");
      } else {
        await createCategory({
          name: formState.name,
          description: formState.description,
          status: formState.status
        });
        setSuccessMessage("Đã tạo category mới.");
      }

      resetForm();
      await loadCategories();
    } catch (error) {
      setErrorMessage(error?.response?.data?.message ?? "Không thể lưu category.");
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDelete(category) {
    if (category.courseCount > 0) {
      return;
    }

    setErrorMessage("");
    setSuccessMessage("");
    try {
      await deleteCategory(category.id);
      setSuccessMessage("Đã xóa category.");
      if (formState.id === category.id) {
        resetForm();
      }
      await loadCategories();
    } catch (error) {
      setErrorMessage(error?.response?.data?.message ?? "Không thể xóa category.");
    }
  }

  function handleMove(categoryId, direction) {
    setCategories((current) => {
      const index = current.findIndex((item) => item.id === categoryId);
      const targetIndex = direction === "up" ? index - 1 : index + 1;
      if (index < 0 || targetIndex < 0 || targetIndex >= current.length) {
        return current;
      }

      const next = [...current];
      [next[index], next[targetIndex]] = [next[targetIndex], next[index]];
      return next;
    });
  }

  async function handleSaveManualOrder() {
    setErrorMessage("");
    setSuccessMessage("");
    try {
      await reorderCategories(categories.map((category) => category.id));
      setSuccessMessage("Đã lưu thứ tự thủ công.");
      await loadCategories();
    } catch (error) {
      setErrorMessage(error?.response?.data?.message ?? "Không thể lưu thứ tự category.");
    }
  }

  const stats = useMemo(() => {
    return categories.reduce((accumulator, category) => {
      accumulator.total += 1;
      accumulator.courses += category.courseCount;
      if (category.status === "Visible") {
        accumulator.visible += 1;
      }
      if (category.status === "Hidden") {
        accumulator.hidden += 1;
      }
      if (category.status === "Archived") {
        accumulator.archived += 1;
      }
      return accumulator;
    }, {
      total: 0,
      visible: 0,
      hidden: 0,
      archived: 0,
      courses: 0
    });
  }, [categories]);

  return (
    <Section className="admin-page admin-page--stack">
      <div className="admin-page__hero">
        <div>
          <p className="admin-page__eyebrow">Taxonomy quản trị</p>
          <h1>Quản lý danh mục</h1>
          <p className="admin-page__description">
            Tạo, chỉnh sửa, lưu trữ và sắp xếp category dùng cho khóa học trong toàn bộ hệ thống.
          </p>
        </div>
        <div className="admin-page__hero-actions">
          <Button onClick={resetForm} variant="ghost">Form mới</Button>
          <Button onClick={() => void loadCategories()}>Làm mới</Button>
        </div>
      </div>

      <div className="admin-overview-grid">
        <Card className="admin-stat-card" variant="shadowed">
          <span className="admin-stat-card__label">Tổng category</span>
          <strong>{stats.total}</strong>
        </Card>
        <Card className="admin-stat-card" variant="shadowed">
          <span className="admin-stat-card__label">Đang hiển thị</span>
          <strong>{stats.visible}</strong>
        </Card>
        <Card className="admin-stat-card" variant="shadowed">
          <span className="admin-stat-card__label">Đang ẩn</span>
          <strong>{stats.hidden}</strong>
        </Card>
        <Card className="admin-stat-card" variant="shadowed">
          <span className="admin-stat-card__label">Đã lưu trữ</span>
          <strong>{stats.archived}</strong>
        </Card>
      </div>

      <div className="admin-settings-grid admin-settings-grid--categories">
        <Card className="admin-panel" variant="shadowed">
          <div className="admin-panel__split">
            <div>
              <p className="admin-page__eyebrow">{formState.id ? "Chỉnh sửa" : "Tạo mới"}</p>
              <h2>{formState.id ? "Cập nhật category" : "Category mới"}</h2>
            </div>
            {formState.id ? (
              <button className="admin-inline-link admin-inline-link--button" onClick={resetForm} type="button">
                Hủy chỉnh sửa
              </button>
            ) : null}
          </div>

          <form className="admin-category-form" onSubmit={(event) => void handleSubmit(event)}>
            <label className="admin-category-form__field">
              <span>Tên category</span>
              <input
                className="ui-input"
                onChange={(event) => setFormState((current) => ({ ...current, name: event.target.value }))}
                placeholder="Ví dụ: AI cho doanh nghiệp"
                value={formState.name}
              />
            </label>

            <label className="admin-category-form__field">
              <span>Mô tả ngắn</span>
              <textarea
                className="ui-input admin-category-form__textarea"
                onChange={(event) => setFormState((current) => ({ ...current, description: event.target.value }))}
                placeholder="Mô tả ngắn cho category này"
                rows={4}
                value={formState.description}
              />
            </label>

            <label className="admin-category-form__field">
              <span>Trạng thái hiển thị</span>
              <select
                className="ui-input"
                onChange={(event) => setFormState((current) => ({ ...current, status: event.target.value }))}
                value={formState.status}
              >
                {STATUS_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>{option.label}</option>
                ))}
              </select>
            </label>

            <div className="admin-category-form__actions">
              <Button disabled={isSaving} type="submit">
                {formState.id ? "Lưu thay đổi" : "Tạo category"}
              </Button>
            </div>
          </form>
        </Card>

        <Card className="admin-panel admin-panel--toolbar" variant="shadowed">
          <form className="admin-toolbar__search admin-toolbar__search--form" onSubmit={(event) => void handleSearchSubmit(event)}>
            <span aria-hidden="true">⌕</span>
            <input
              onChange={(event) => setSearchTerm(event.target.value)}
              placeholder="Tìm theo tên hoặc mô tả..."
              value={searchTerm}
            />
          </form>

          <div className="admin-toolbar__filters">
            <select className="ui-input admin-filter-select" onChange={(event) => setStatusFilter(event.target.value)} value={statusFilter}>
              <option value="all">Tất cả trạng thái</option>
              {STATUS_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>{option.label}</option>
              ))}
            </select>
            <select className="ui-input admin-filter-select" onChange={(event) => setSortMode(event.target.value)} value={sortMode}>
              {SORT_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>{option.label}</option>
              ))}
            </select>
            {sortMode === "manual" ? (
              <Button onClick={() => void handleSaveManualOrder()} variant="ghost">
                Lưu thứ tự
              </Button>
            ) : null}
          </div>
        </Card>
      </div>

      {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}
      {successMessage ? <p className="ui-alert ui-alert--success">{successMessage}</p> : null}

      <Card className="admin-table-card" variant="shadowed">
        <div className="admin-table">
          <div className="admin-table__header admin-category-row">
            <span>Category</span>
            <span>Trạng thái</span>
            <span>Khóa học đang dùng</span>
            <span>Ngày tạo</span>
            <span>Sắp xếp</span>
            <span>Điều khiển</span>
          </div>

          {isLoading ? (
            <div className="admin-table__empty">Đang tải category...</div>
          ) : categories.length === 0 ? (
            <div className="admin-table__empty">Không có category phù hợp với bộ lọc hiện tại.</div>
          ) : (
            categories.map((category, index) => (
              <div className="admin-table__row admin-category-row" key={category.id}>
                <div className="admin-category-cell">
                  <strong>{category.name}</strong>
                  <span>{category.description}</span>
                </div>
                <span className={`admin-status-badge${category.status === "Visible" ? " admin-status-badge--success" : category.status === "Archived" ? " admin-status-badge--muted" : ""}`}>
                  {getStatusLabel(category.status)}
                </span>
                <span>{category.courseCount}</span>
                <span>{formatDate(category.createdAt)}</span>
                <div className="admin-reorder-controls">
                  {sortMode === "manual" ? (
                    <>
                      <button
                        className="admin-mini-button"
                        disabled={index === 0}
                        onClick={() => handleMove(category.id, "up")}
                        type="button"
                      >
                        ↑
                      </button>
                      <button
                        className="admin-mini-button"
                        disabled={index === categories.length - 1}
                        onClick={() => handleMove(category.id, "down")}
                        type="button"
                      >
                        ↓
                      </button>
                    </>
                  ) : (
                    <span className="admin-muted-inline">#{category.sortOrder}</span>
                  )}
                </div>
                <div className="admin-table__actions">
                  <Button onClick={() => handleEdit(category)} variant="ghost">Sửa</Button>
                  <Button
                    disabled={category.courseCount > 0}
                    onClick={() => void handleDelete(category)}
                    variant="ghost"
                  >
                    Xóa
                  </Button>
                </div>
              </div>
            ))
          )}
        </div>
      </Card>
    </Section>
  );
}
