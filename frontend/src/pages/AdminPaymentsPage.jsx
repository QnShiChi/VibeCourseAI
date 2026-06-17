import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { getAdminPaymentOrders } from "../api/paymentService";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import Section from "../components/ui/Section";
import {
  buildAdminPaymentBadgeClass,
  buildAdminPaymentStatusLabel,
  formatAdminPaymentCurrency,
  formatAdminPaymentDateTime
} from "../utils/adminPayments";

export default function AdminPaymentsPage() {
  const [paymentOrders, setPaymentOrders] = useState([]);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  async function loadPaymentOrders() {
    setIsLoading(true);
    setErrorMessage("");

    try {
      setPaymentOrders(await getAdminPaymentOrders());
    } catch {
      setErrorMessage("Không thể tải danh sách hóa đơn.");
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadPaymentOrders();
  }, []);

  const visibleOrders = useMemo(() => {
    const keyword = searchTerm.trim().toLowerCase();
    const fromTime = dateFrom ? new Date(`${dateFrom}T00:00:00`).getTime() : null;
    const toTime = dateTo ? new Date(`${dateTo}T23:59:59.999`).getTime() : null;

    return paymentOrders.filter((order) => {
      const matchesKeyword = keyword.length === 0
        || `${order.orderCode} ${order.userFullName} ${order.userEmail} ${order.courseTitle}`.toLowerCase().includes(keyword);
      const matchesStatus = statusFilter === "all" || order.status === statusFilter;
      const createdAtTime = new Date(order.createdAt).getTime();
      const matchesDateFrom = fromTime === null || createdAtTime >= fromTime;
      const matchesDateTo = toTime === null || createdAtTime <= toTime;
      return matchesKeyword && matchesStatus && matchesDateFrom && matchesDateTo;
    });
  }, [dateFrom, dateTo, paymentOrders, searchTerm, statusFilter]);

  const paidCount = paymentOrders.filter((order) => order.status === "Paid" || order.status === "LatePaid").length;
  const pendingCount = paymentOrders.filter((order) => order.status === "Pending").length;
  const issueCount = paymentOrders.filter((order) => order.status === "Expired" || order.status === "Failed").length;

  return (
    <Section className="admin-page admin-page--stack">
      <div className="admin-page__hero">
        <div>
          <p className="admin-page__eyebrow">Vận hành thanh toán</p>
          <h1>Quản lý hóa đơn</h1>
          <p className="admin-page__description">
            Tra soát từng đơn thanh toán, theo dõi trạng thái xử lý và mở nhanh vào chi tiết giao dịch.
          </p>
        </div>
        <div className="admin-page__hero-actions">
          <Button onClick={() => void loadPaymentOrders()} variant="ghost">
            {isLoading ? "Đang tải..." : "Làm mới"}
          </Button>
        </div>
      </div>

      <div className="admin-overview-grid">
        <Card className="admin-stat-card" variant="shadowed">
          <span className="admin-stat-card__label">Tổng hóa đơn</span>
          <strong>{paymentOrders.length}</strong>
        </Card>
        <Card className="admin-stat-card" variant="shadowed">
          <span className="admin-stat-card__label">Đã thanh toán</span>
          <strong>{paidCount}</strong>
        </Card>
        <Card className="admin-stat-card" variant="shadowed">
          <span className="admin-stat-card__label">Chờ thanh toán</span>
          <strong>{pendingCount}</strong>
        </Card>
        <Card className="admin-stat-card" variant="shadowed">
          <span className="admin-stat-card__label">Hết hạn / lỗi</span>
          <strong>{issueCount}</strong>
        </Card>
      </div>

      <Card className="admin-panel admin-panel--toolbar" variant="shadowed">
        <label className="admin-toolbar__search">
          <span aria-hidden="true">⌕</span>
          <input
            onChange={(event) => setSearchTerm(event.target.value)}
            placeholder="Tìm theo mã đơn, người mua, email hoặc khóa học..."
            value={searchTerm}
          />
        </label>

        <div className="admin-toolbar__date-range">
          <label className="admin-toolbar__date-field">
            <span>Từ ngày</span>
            <input
              aria-label="Từ ngày"
              max={dateTo || undefined}
              onChange={(event) => setDateFrom(event.target.value)}
              type="date"
              value={dateFrom}
            />
          </label>
          <label className="admin-toolbar__date-field">
            <span>Đến ngày</span>
            <input
              aria-label="Đến ngày"
              min={dateFrom || undefined}
              onChange={(event) => setDateTo(event.target.value)}
              type="date"
              value={dateTo}
            />
          </label>
        </div>

        <div className="admin-toolbar__filters">
          <button className={`admin-filter-pill${statusFilter === "all" ? " admin-filter-pill--active" : ""}`} onClick={() => setStatusFilter("all")} type="button">Tất cả</button>
          <button className={`admin-filter-pill${statusFilter === "Pending" ? " admin-filter-pill--active" : ""}`} onClick={() => setStatusFilter("Pending")} type="button">Chờ thanh toán</button>
          <button className={`admin-filter-pill${statusFilter === "Paid" ? " admin-filter-pill--active" : ""}`} onClick={() => setStatusFilter("Paid")} type="button">Đã thanh toán</button>
          <button className={`admin-filter-pill${statusFilter === "LatePaid" ? " admin-filter-pill--active" : ""}`} onClick={() => setStatusFilter("LatePaid")} type="button">Thanh toán muộn</button>
          <button className={`admin-filter-pill${statusFilter === "Expired" ? " admin-filter-pill--active" : ""}`} onClick={() => setStatusFilter("Expired")} type="button">Hết hạn</button>
          <button className={`admin-filter-pill${statusFilter === "Failed" ? " admin-filter-pill--active" : ""}`} onClick={() => setStatusFilter("Failed")} type="button">Lỗi</button>
        </div>
      </Card>

      {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

      <Card className="admin-table-card" variant="shadowed">
        <div className="admin-table">
          <div className="admin-table__header admin-payment-row">
            <span>Mã đơn</span>
            <span>Người mua</span>
            <span>Khóa học</span>
            <span>Số tiền</span>
            <span>Trạng thái</span>
            <span>Tạo lúc</span>
            <span>Thanh toán</span>
            <span>Chi tiết</span>
          </div>

          {isLoading ? (
            <div className="admin-table__empty">Đang tải danh sách hóa đơn...</div>
          ) : visibleOrders.length === 0 ? (
            <div className="admin-table__empty">Không có hóa đơn nào khớp bộ lọc hiện tại.</div>
          ) : (
            visibleOrders.map((order) => (
              <div className="admin-table__row admin-payment-row" key={order.paymentOrderId}>
                <div className="admin-payment-cell">
                  <strong>{order.orderCode}</strong>
                  <span>{order.paymentOrderId}</span>
                </div>
                <div className="admin-payment-cell">
                  <strong>{order.userFullName}</strong>
                  <span>{order.userEmail}</span>
                </div>
                <div className="admin-payment-cell">
                  <strong>{order.courseTitle}</strong>
                  <span>{formatAdminPaymentDateTime(order.expiresAt)}</span>
                </div>
                <span>{formatAdminPaymentCurrency(order.amount)}</span>
                <span className={buildAdminPaymentBadgeClass(order.status)}>
                  {buildAdminPaymentStatusLabel(order.status)}
                </span>
                <span>{formatAdminPaymentDateTime(order.createdAt)}</span>
                <span>{formatAdminPaymentDateTime(order.paidAt)}</span>
                <div className="admin-table__actions">
                  <Button as={Link} to={`/admin/payments/${order.paymentOrderId}`} variant="ghost">Xem chi tiết</Button>
                </div>
              </div>
            ))
          )}
        </div>
      </Card>
    </Section>
  );
}
