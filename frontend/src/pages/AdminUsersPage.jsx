import { useEffect, useMemo, useState } from "react";
import { getUsers, updateUserActive } from "../api/userService";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import Section from "../components/ui/Section";

function formatDate(value) {
  return new Intl.DateTimeFormat("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric"
  }).format(new Date(value));
}

function getInitials(name = "") {
  return name
    .trim()
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? "")
    .join("") || "U";
}

function getRoleLabel(role = "") {
  if (role === "Admin") {
    return "Quản trị viên";
  }

  if (role === "Instructor") {
    return "Giảng viên";
  }

  return "Học viên";
}

export default function AdminUsersPage() {
  const [users, setUsers] = useState([]);
  const [searchTerm, setSearchTerm] = useState("");
  const [roleFilter, setRoleFilter] = useState("all");
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  async function loadUsers() {
    setIsLoading(true);
    setErrorMessage("");
    try {
      setUsers(await getUsers());
    } catch {
      setErrorMessage("Không thể tải danh sách người dùng.");
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadUsers();
  }, []);

  async function handleToggleActive(userId, isActive) {
    const previousUsers = users;
    setUsers((current) => current.map((user) => (user.id === userId ? { ...user, isActive } : user)));

    try {
      await updateUserActive(userId, isActive);
    } catch {
      setUsers(previousUsers);
      setErrorMessage("Không thể cập nhật trạng thái người dùng.");
    }
  }

  const visibleUsers = useMemo(() => {
    const keyword = searchTerm.trim().toLowerCase();
    return users.filter((user) => {
      const matchesKeyword = keyword.length === 0
        || `${user.fullName} ${user.email} ${user.role}`.toLowerCase().includes(keyword);
      const matchesRole = roleFilter === "all" || user.role === roleFilter;
      return matchesKeyword && matchesRole;
    });
  }, [users, searchTerm, roleFilter]);

  const activeUsers = users.filter((user) => user.isActive).length;
  const adminUsers = users.filter((user) => user.role === "Admin").length;
  const newestUsers = [...users].sort((left, right) => new Date(right.createdAt) - new Date(left.createdAt));

  return (
    <Section className="admin-page admin-page--stack">
      <div className="admin-page__hero">
        <div>
          <p className="admin-page__eyebrow">Quản trị thành viên</p>
          <h1>Quản lý người dùng</h1>
          <p className="admin-page__description">
            Theo dõi vai trò, trạng thái hoạt động và điều tiết quyền truy cập của học viên, giảng viên và admin.
          </p>
        </div>
        <div className="admin-page__hero-actions">
          <Button onClick={() => void loadUsers()}>Làm mới dữ liệu</Button>
        </div>
      </div>

      <div className="admin-overview-grid">
        <Card className="admin-stat-card" variant="shadowed">
          <span className="admin-stat-card__label">Tổng người dùng</span>
          <strong>{users.length}</strong>
        </Card>
        <Card className="admin-stat-card" variant="shadowed">
          <span className="admin-stat-card__label">Đang hoạt động</span>
          <strong>{activeUsers}</strong>
        </Card>
        <Card className="admin-stat-card" variant="shadowed">
          <span className="admin-stat-card__label">Quản trị viên</span>
          <strong>{adminUsers}</strong>
        </Card>
        <Card className="admin-stat-card" variant="shadowed">
          <span className="admin-stat-card__label">Mới nhất</span>
          <strong>{newestUsers[0] ? formatDate(newestUsers[0].createdAt) : "--"}</strong>
        </Card>
      </div>

      <Card className="admin-panel admin-panel--toolbar" variant="shadowed">
        <label className="admin-toolbar__search">
          <span aria-hidden="true">⌕</span>
          <input
            onChange={(event) => setSearchTerm(event.target.value)}
            placeholder="Tìm theo tên, email hoặc vai trò..."
            value={searchTerm}
          />
        </label>

        <div className="admin-toolbar__filters">
          <button className={`admin-filter-pill${roleFilter === "all" ? " admin-filter-pill--active" : ""}`} onClick={() => setRoleFilter("all")} type="button">Tất cả</button>
          <button className={`admin-filter-pill${roleFilter === "Admin" ? " admin-filter-pill--active" : ""}`} onClick={() => setRoleFilter("Admin")} type="button">Admin</button>
          <button className={`admin-filter-pill${roleFilter === "Instructor" ? " admin-filter-pill--active" : ""}`} onClick={() => setRoleFilter("Instructor")} type="button">Giảng viên</button>
          <button className={`admin-filter-pill${roleFilter === "User" ? " admin-filter-pill--active" : ""}`} onClick={() => setRoleFilter("User")} type="button">Học viên</button>
        </div>
      </Card>

      {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

      <Card className="admin-table-card" variant="shadowed">
        <div className="admin-table">
          <div className="admin-table__header admin-user-row">
            <span>Người dùng</span>
            <span>Email</span>
            <span>Vai trò</span>
            <span>Ngày tham gia</span>
            <span>Trạng thái</span>
            <span>Điều khiển</span>
          </div>

          {isLoading ? (
            <div className="admin-table__empty">Đang tải dữ liệu người dùng...</div>
          ) : visibleUsers.length === 0 ? (
            <div className="admin-table__empty">Không có người dùng phù hợp với bộ lọc hiện tại.</div>
          ) : (
            visibleUsers.map((user) => (
              <div className="admin-table__row admin-user-row" key={user.id}>
                <div className="admin-user-cell">
                  <div className="admin-avatar">{getInitials(user.fullName)}</div>
                  <div>
                    <strong>{user.fullName}</strong>
                    <span>{user.isActive ? "Đang có quyền truy cập" : "Đã bị khóa quyền truy cập"}</span>
                  </div>
                </div>
                <span>{user.email}</span>
                <span className={`admin-role-badge admin-role-badge--${user.role.toLowerCase()}`}>{getRoleLabel(user.role)}</span>
                <span>{formatDate(user.createdAt)}</span>
                <span className={`admin-status-badge${user.isActive ? " admin-status-badge--success" : " admin-status-badge--muted"}`}>
                  {user.isActive ? "Hoạt động" : "Đã khóa"}
                </span>
                <div className="admin-table__actions">
                  <Button
                    onClick={() => void handleToggleActive(user.id, !user.isActive)}
                    variant={user.isActive ? "ghost" : "primary"}
                  >
                    {user.isActive ? "Khóa tài khoản" : "Mở lại"}
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
