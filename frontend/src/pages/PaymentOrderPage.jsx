import { useEffect, useRef, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { cancelPaymentOrder, getPaymentOrder } from "../api/paymentService";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import Section from "../components/ui/Section";
import styles from "../styles/CheckoutPage.module.css";

export default function PaymentOrderPage() {
  const { orderId } = useParams();
  const navigate = useNavigate();
  const [order, setOrder] = useState(null);
  const [errorMessage, setErrorMessage] = useState("");
  const [qrFailed, setQrFailed] = useState(false);
  const [now, setNow] = useState(() => Date.now());
  const [copiedField, setCopiedField] = useState("");
  const [activeModal, setActiveModal] = useState("");
  const [dismissedModalKinds, setDismissedModalKinds] = useState([]);
  const [isCancelConfirmOpen, setIsCancelConfirmOpen] = useState(false);
  const [isCancelling, setIsCancelling] = useState(false);
  const lastObservedStatusRef = useRef("");

  useEffect(() => {
    if (!orderId) {
      return undefined;
    }

    let cancelled = false;

    async function loadOrder() {
      try {
        const nextOrder = await getPaymentOrder(orderId);
        if (!cancelled) {
          setOrder(nextOrder);
          setErrorMessage("");
          setQrFailed(false);
        }
      } catch (error) {
        if (!cancelled) {
          setErrorMessage(error?.response?.data?.message ?? "Không thể tải order thanh toán.");
        }
      }
    }

    void loadOrder();
    const intervalId = window.setInterval(() => {
      void loadOrder();
    }, 3000);

    return () => {
      cancelled = true;
      window.clearInterval(intervalId);
    };
  }, [orderId]);

  useEffect(() => {
    const timerId = window.setInterval(() => {
      setNow(Date.now());
    }, 1000);

    return () => {
      window.clearInterval(timerId);
    };
  }, []);

  useEffect(() => {
    if (!copiedField) {
      return undefined;
    }

    const timeoutId = window.setTimeout(() => {
      setCopiedField("");
    }, 1500);

    return () => window.clearTimeout(timeoutId);
  }, [copiedField]);

  useEffect(() => {
    if (!order?.status) {
      return;
    }

    const previousStatus = lastObservedStatusRef.current;
    lastObservedStatusRef.current = order.status;

    if (!previousStatus) {
      const initialModalKind = resolveInitialModalKind(order.status);
      if (initialModalKind && !dismissedModalKinds.includes(initialModalKind)) {
        setActiveModal(initialModalKind);
      }
      return;
    }

    if (previousStatus === order.status) {
      return;
    }

    const nextModalKind = resolveModalKind(previousStatus, order.status);
    if (!nextModalKind || dismissedModalKinds.includes(nextModalKind)) {
      return;
    }

    setActiveModal(nextModalKind);
  }, [dismissedModalKinds, order?.status]);

  async function handleCopy(value, field) {
    try {
      await navigator.clipboard.writeText(value);
      setCopiedField(field);
    } catch {
      setCopiedField("");
    }
  }

  function handleDismissModal() {
    if (!activeModal) {
      return;
    }

    setDismissedModalKinds((current) => (current.includes(activeModal) ? current : [...current, activeModal]));
    setActiveModal("");
  }

  async function handleConfirmCancelPayment() {
    if (!orderId) {
      return;
    }

    setIsCancelling(true);
    setErrorMessage("");

    try {
      const cancelledOrder = await cancelPaymentOrder(orderId);
      navigate("/cart", {
        replace: true,
        state: {
          paymentCancelled: {
            orderCode: cancelledOrder.orderCode
          }
        }
      });
    } catch (error) {
      setErrorMessage(error?.response?.data?.message ?? "Không thể hủy thanh toán lúc này.");
      setIsCancelling(false);
      setIsCancelConfirmOpen(false);
    }
  }

  return (
    <Section className={`${styles.page} ${styles.paymentPage}`.trim()}>
      <div className={styles.header}>
        <div className={styles.headerCopy}>
          <p className="page-eyebrow">SePay Transfer</p>
          <h1>Thanh toán đơn hàng</h1>
          <p>Vui lòng quét mã QR hoặc chuyển khoản thủ công để hoàn tất đăng ký khóa học.</p>
        </div>
        <Button as={Link} to="/cart" variant="ghost">Quay lại giỏ hàng</Button>
      </div>

      {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

      {!order ? (
        <Card variant="shadowed"><p>Đang tải thông tin order...</p></Card>
      ) : (
        <div className={styles.paymentGrid}>
          <div className={styles.paymentMain}>
            <Card className={styles.qrCard} variant="shadowed">
              <div className={styles.paymentSafeBadge}>Giao dịch an toàn 100%</div>
              <div className={styles.qrHeading}>
                <h2>Quét mã để thanh toán</h2>
                <p>Sử dụng ứng dụng ngân hàng hoặc ví điện tử để quét mã QR và thanh toán ngay lập tức.</p>
              </div>

              <div className={styles.qrEmbed}>
                {!qrFailed && order.qrImageUrl ? (
                  <img
                    alt={buildQrAlt(order)}
                    className={styles.qrImage}
                    loading="eager"
                    onError={() => setQrFailed(true)}
                    src={order.qrImageUrl}
                  />
                ) : (
                  <div className={styles.qrFallback} role="status">
                    <strong>QR chưa khả dụng</strong>
                    <p>
                      {isSandboxMockBank(order)
                        ? "SePay Test mode đang dùng ngân hàng giả lập, nên hệ thống hiển thị thông tin thanh toán thay cho ảnh QR."
                        : "Không thể tải ảnh QR. Bạn vẫn có thể chuyển khoản bằng đúng số tiền và nội dung ở khung bên phải."}
                    </p>
                  </div>
                )}
              </div>

              <div className={styles.paymentStatusArea}>
                <span className={`${styles.paymentStatus} ${buildStatusToneClass(order.status, styles)}`.trim()}>{buildStatusHeadline(order.status)}</span>
                <div className={styles.countdownPill}>
                  <strong>{formatCountdown(order.expiresAt, order.status, now)}</strong>
                  <span>{buildCountdownLabel(order)}</span>
                </div>
              </div>

              <div className={styles.paymentInfoNote}>
                <p>Hệ thống sẽ tự động xác nhận sau khi nhận được tiền thông qua webhook. Vui lòng không đóng trang này cho đến khi nhận được thông báo hoàn tất.</p>
              </div>

              {shouldAllowCancelPayment(order) ? (
                <div className={styles.paymentCancelActions}>
                  <Button onClick={() => setIsCancelConfirmOpen(true)} variant="ghost">Hủy thanh toán</Button>
                </div>
              ) : null}

              {shouldShowRetryCheckoutAction(order, activeModal) ? (
                <div className={styles.paymentRetryActions}>
                  <Button as={Link} to="/cart">Về giỏ hàng để tạo lại thanh toán</Button>
                </div>
              ) : null}
            </Card>
          </div>

          <div className={styles.paymentSidebar}>
            <Card className={styles.orderDetailCard} variant="shadowed">
              <div className={styles.orderDetailHeader}>
                <h2>Chi tiết đơn hàng</h2>
              </div>

              <div className={styles.courseMiniCard}>
                <div className={styles.courseMiniIcon}>◫</div>
                <div className={styles.courseMiniCopy}>
                  <span>Khóa học</span>
                  <strong>{order.courseTitle}</strong>
                </div>
              </div>

              <div className={styles.detailFields}>
                <div className={styles.infoField}>
                  <div className={styles.infoFieldHeader}>
                    <span>Mã đơn hàng</span>
                    <button className={styles.copyAction} onClick={() => void handleCopy(order.orderCode, "orderCode")} type="button">
                      {copiedField === "orderCode" ? "Đã sao chép" : "Sao chép"}
                    </button>
                  </div>
                  <strong className={styles.mono}>{order.orderCode}</strong>
                </div>

                <div className={styles.infoFieldGrid}>
                  <div className={styles.infoField}>
                    <span>Ngân hàng</span>
                    <strong>{order.bankName || order.bankCode}</strong>
                  </div>
                  <div className={styles.infoField}>
                    <div className={styles.infoFieldHeader}>
                      <span>Số tài khoản</span>
                      <button className={styles.copyAction} onClick={() => void handleCopy(order.bankAccountNumber, "account")} type="button">
                        {copiedField === "account" ? "Đã sao chép" : "Sao chép"}
                      </button>
                    </div>
                    <strong className={styles.mono}>{order.bankAccountNumber}</strong>
                  </div>
                </div>

                <div className={styles.infoField}>
                  <span>Chủ tài khoản</span>
                  <strong>{order.accountHolderName}</strong>
                </div>

                <div className={`${styles.transferField} ${styles.infoField}`.trim()}>
                  <div className={styles.infoFieldHeader}>
                    <span>Nội dung chuyển khoản (bắt buộc)</span>
                    <button className={styles.copyAction} onClick={() => void handleCopy(order.transferContent, "transfer")} type="button">
                      {copiedField === "transfer" ? "Đã sao chép" : "Sao chép"}
                    </button>
                  </div>
                  <strong className={styles.mono}>{order.transferContent}</strong>
                </div>

                <div className={styles.amountPanel}>
                  <span>Số tiền cần thanh toán</span>
                  <strong>{formatCurrency(order.amount)}</strong>
                </div>

                <div className={styles.metaRows}>
                  <div className={styles.row}><span>Trạng thái</span><strong>{translateStatus(order.status)}</strong></div>
                  <div className={styles.row}><span>Hết hạn lúc</span><strong>{formatDateTime(order.expiresAt)}</strong></div>
                  <div className={styles.row}><span>Đã thanh toán</span><strong>{order.paidAt ? formatDateTime(order.paidAt) : "Chưa"}</strong></div>
                </div>

                {order.status === "Paid" || order.status === "LatePaid" ? (
                  <Button as={Link} to={`/courses/${order.courseId}/learn`}>Vào học ngay</Button>
                ) : order.status === "Cancelled" ? (
                  <Button as={Link} to="/cart">Về giỏ hàng</Button>
                ) : order.isExpired ? (
                  <p className="ui-alert ui-alert--warning">Order đã hết hạn. Nếu bạn chuyển tiền muộn, backend vẫn sẽ cập nhật sang thanh toán thành công khi webhook hợp lệ về.</p>
                ) : (
                  <Button disabled variant="ghost">Đang chờ xác nhận giao dịch</Button>
                )}
              </div>
            </Card>
          </div>
        </div>
      )}

      {order && activeModal ? (
        <div
          aria-labelledby="payment-status-modal-title"
          aria-modal="true"
          className={styles.statusModalOverlay}
          role="dialog"
        >
          <Card className={`${styles.statusModalCard} ${activeModal === "success" ? styles.statusModalCardSuccess : styles.statusModalCardExpired}`.trim()} variant="shadowed">
            <button
              aria-label="Đóng thông báo"
              className={styles.statusModalClose}
              onClick={handleDismissModal}
              type="button"
            >
              ×
            </button>

            <div className={`${styles.statusModalIcon} ${activeModal === "success" ? styles.statusModalIconSuccess : styles.statusModalIconExpired}`.trim()} aria-hidden="true">
              {activeModal === "success" ? "✓" : "!"}
            </div>

            <div className={styles.statusModalCopy}>
              <p className={styles.statusModalEyebrow}>
                {activeModal === "success" ? "Thanh toán hoàn tất" : "Đơn hàng hết hạn"}
              </p>
              <h2 id="payment-status-modal-title">
                {activeModal === "success"
                  ? order.status === "LatePaid"
                    ? "Đã ghi nhận thanh toán muộn"
                    : "Thanh toán thành công"
                  : "Đơn hàng đã hết thời gian thanh toán"}
              </h2>
              <p>
                {activeModal === "success"
                  ? "Hệ thống đã xác nhận giao dịch. Bạn có thể vào học ngay bây giờ."
                  : "Vui lòng quay lại giỏ hàng để tạo thanh toán mới nếu bạn vẫn muốn đăng ký khóa học này."}
              </p>
            </div>

            <div className={styles.statusModalActions}>
              {activeModal === "success" ? (
                <Button as={Link} to={`/courses/${order.courseId}/learn`}>Vào học ngay</Button>
              ) : (
                <Button as={Link} to="/cart">Về giỏ hàng</Button>
              )}
              <Button onClick={handleDismissModal} variant="ghost">Xem lại đơn hàng</Button>
            </div>
          </Card>
        </div>
      ) : null}

      {order && isCancelConfirmOpen ? (
        <div
          aria-labelledby="cancel-payment-modal-title"
          aria-modal="true"
          className={styles.statusModalOverlay}
          role="dialog"
        >
          <Card className={styles.statusModalCard} variant="shadowed">
            <button
              aria-label="Đóng xác nhận hủy thanh toán"
              className={styles.statusModalClose}
              onClick={() => setIsCancelConfirmOpen(false)}
              type="button"
            >
              ×
            </button>

            <div className={`${styles.statusModalIcon} ${styles.statusModalIconExpired}`.trim()} aria-hidden="true">
              !
            </div>

            <div className={styles.statusModalCopy}>
              <p className={styles.statusModalEyebrow}>Xác nhận hủy thanh toán</p>
              <h2 id="cancel-payment-modal-title">Bạn có chắc muốn hủy thanh toán này không?</h2>
              <p>
                Đơn hàng sẽ được chuyển sang trạng thái đã hủy thanh toán và bạn sẽ quay về giỏ hàng để tạo thanh toán mới.
              </p>
            </div>

            <div className={styles.statusModalActions}>
              <Button onClick={() => void handleConfirmCancelPayment()}>
                {isCancelling ? "Đang hủy..." : "Xác nhận hủy"}
              </Button>
              <Button onClick={() => setIsCancelConfirmOpen(false)} variant="ghost">Tiếp tục thanh toán</Button>
            </div>
          </Card>
        </div>
      ) : null}
    </Section>
  );
}

function resolveModalKind(previousStatus, nextStatus) {
  if (isPaidStatus(nextStatus) && !isPaidStatus(previousStatus)) {
    return "success";
  }

  if (previousStatus === "Pending" && nextStatus === "Expired") {
    return "expired";
  }

  return "";
}

function resolveInitialModalKind(status) {
  if (isPaidStatus(status)) {
    return "success";
  }

  if (status === "Expired") {
    return "expired";
  }

  return "";
}

function isPaidStatus(status) {
  return status === "Paid" || status === "LatePaid";
}

function isErrorStatus(status) {
  return status === "Expired" || status === "Failed";
}

function shouldShowRetryCheckoutAction(order, activeModal) {
  return Boolean(order?.isExpired || order?.status === "Expired" || order?.status === "Failed" || order?.status === "Cancelled") && activeModal !== "expired";
}

function shouldAllowCancelPayment(order) {
  return order?.status === "Pending" && !order?.isExpired;
}

function buildStatusToneClass(status, styles) {
  if (isPaidStatus(status)) {
    return styles.paymentStatusSuccess;
  }

  if (isErrorStatus(status)) {
    return styles.paymentStatusError;
  }

  if (status === "Cancelled") {
    return styles.paymentStatusError;
  }

  return styles.paymentStatusPending;
}

function isSandboxMockBank(order) {
  const bankCode = (order?.bankCode ?? "").toUpperCase();
  const bankName = (order?.bankName ?? "").toLowerCase();
  return bankCode.startsWith("ACME") || bankName.includes("giả lập") || bankName.includes("gia lap");
}

function buildQrAlt(order) {
  return `QR thanh toán - ${order.bankName || order.bankCode} - ${maskAccountNumber(order.bankAccountNumber)} - ${order.accountHolderName}`;
}

function maskAccountNumber(value) {
  if (!value) {
    return "";
  }

  if (value.length <= 4) {
    return value;
  }

  return `${value.slice(0, 3)}${"*".repeat(Math.max(0, value.length - 5))}${value.slice(-2)}`;
}

function formatCurrency(value) {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0
  }).format(value ?? 0);
}

function formatDateTime(value) {
  if (!value) {
    return "";
  }

  return new Intl.DateTimeFormat("vi-VN", {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(parseUtcDate(value));
}

function translateStatus(status) {
  switch (status) {
    case "Paid":
      return "Đã thanh toán";
    case "LatePaid":
      return "Thanh toán muộn";
    case "Expired":
      return "Hết hạn";
    case "Cancelled":
      return "Đã hủy thanh toán";
    case "Pending":
    default:
      return "Chờ thanh toán";
  }
}

function buildCountdownLabel(order) {
  if (isPaidStatus(order.status)) {
    return "Giao dịch đã được xác nhận";
  }

  if (order.status === "Cancelled") {
    return "Đơn hàng đã bị hủy";
  }

  return order.isExpired ? "Đơn hàng đã hết hạn" : "Đơn hàng sẽ hết hạn";
}

function buildStatusHeadline(status) {
  switch (status) {
    case "Paid":
      return "Thanh toán thành công";
    case "LatePaid":
      return "Đã ghi nhận thanh toán muộn";
    case "Expired":
      return "Đơn hàng đã hết hạn";
    case "Cancelled":
      return "Đã hủy thanh toán";
    case "Pending":
    default:
      return "Đang chờ thanh toán...";
  }
}

function formatCountdown(expiresAt, status, now) {
  if (isPaidStatus(status)) {
    return "Đã thanh toán";
  }

  if (status === "Cancelled") {
    return "Đã hủy";
  }

  const expiresAtMs = parseUtcDate(expiresAt).getTime();
  const remainingMs = Math.max(0, expiresAtMs - now);

  if (remainingMs === 0) {
    return "00:00";
  }

  const totalSeconds = Math.floor(remainingMs / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
}

function parseUtcDate(value) {
  if (!value) {
    return new Date(NaN);
  }

  if (/[zZ]|[+-]\d{2}:\d{2}$/.test(value)) {
    return new Date(value);
  }

  return new Date(`${value}Z`);
}
