import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { getAdminPaymentOrderDetail } from "../api/paymentService";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import Section from "../components/ui/Section";
import {
  buildAdminPaymentBadgeClass,
  buildAdminPaymentStatusLabel,
  formatAdminPaymentCurrency,
  formatAdminPaymentDateTime
} from "../utils/adminPayments";

export default function AdminPaymentDetailPage() {
  const { paymentOrderId } = useParams();
  const [paymentOrder, setPaymentOrder] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    async function loadPaymentOrder() {
      setIsLoading(true);
      setErrorMessage("");

      try {
        setPaymentOrder(await getAdminPaymentOrderDetail(paymentOrderId));
      } catch {
        setErrorMessage("Không thể tải chi tiết hóa đơn.");
      } finally {
        setIsLoading(false);
      }
    }

    if (paymentOrderId) {
      void loadPaymentOrder();
    }
  }, [paymentOrderId]);

  return (
    <Section className="admin-page admin-page--stack">
      <div className="admin-page__hero">
        <div>
          <p className="admin-page__eyebrow">Chi tiết hóa đơn</p>
          <h1>Tra soát giao dịch</h1>
          <p className="admin-page__description">
            Xem đầy đủ thông tin đơn hàng, người mua và dữ liệu thanh toán liên quan.
          </p>
        </div>
        <div className="admin-page__hero-actions">
          <Button as={Link} to="/admin/payments" variant="ghost">Quay lại danh sách</Button>
        </div>
      </div>

      {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

      {isLoading ? (
        <Card className="admin-empty-card" variant="shadowed">
          <p>Đang tải chi tiết hóa đơn...</p>
        </Card>
      ) : paymentOrder ? (
        <div className="admin-settings-grid admin-settings-grid--profile">
          <Card className="admin-panel" variant="shadowed">
            <div className="admin-panel__split">
              <div>
                <p className="admin-page__eyebrow">Đơn hàng</p>
                <h2>{paymentOrder.orderCode}</h2>
              </div>
              <span className={buildAdminPaymentBadgeClass(paymentOrder.status)}>
                {buildAdminPaymentStatusLabel(paymentOrder.status)}
              </span>
            </div>
            <div className="admin-detail-list">
              <div><span>Người mua</span><strong>{paymentOrder.userFullName}</strong></div>
              <div><span>Email</span><strong>{paymentOrder.userEmail}</strong></div>
              <div><span>Khóa học</span><strong>{paymentOrder.courseTitle}</strong></div>
              <div><span>Số tiền</span><strong>{formatAdminPaymentCurrency(paymentOrder.amount)}</strong></div>
              <div><span>Tạo lúc</span><strong>{formatAdminPaymentDateTime(paymentOrder.createdAt)}</strong></div>
              <div><span>Hết hạn</span><strong>{formatAdminPaymentDateTime(paymentOrder.expiresAt)}</strong></div>
              <div><span>Thanh toán</span><strong>{formatAdminPaymentDateTime(paymentOrder.paidAt)}</strong></div>
            </div>
          </Card>

          <Card className="admin-panel" variant="shadowed">
            <div>
              <p className="admin-page__eyebrow">Thông tin thanh toán</p>
              <h2>Ngân hàng và đối soát</h2>
            </div>
            <div className="admin-detail-list">
              <div><span>Mã ngân hàng</span><strong>{paymentOrder.bankCode || "--"}</strong></div>
              <div><span>Tên ngân hàng</span><strong>{paymentOrder.bankName || "--"}</strong></div>
              <div><span>Số tài khoản</span><strong>{paymentOrder.bankAccountNumber || "--"}</strong></div>
              <div><span>Chủ tài khoản</span><strong>{paymentOrder.accountHolderName || "--"}</strong></div>
              <div><span>Nội dung chuyển khoản</span><strong>{paymentOrder.transferContent || "--"}</strong></div>
              <div><span>SePay transaction</span><strong>{paymentOrder.sepayTransactionId ?? "--"}</strong></div>
            </div>
          </Card>
        </div>
      ) : null}
    </Section>
  );
}
