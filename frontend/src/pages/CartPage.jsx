import { useEffect, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { createCheckoutOrders } from "../api/paymentService";
import { getCart, removeCartItem } from "../api/cartService";
import { useAuth } from "../auth/useAuth";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import Section from "../components/ui/Section";
import styles from "../styles/CheckoutPage.module.css";
import { getGuestCartToken } from "../utils/cartStorage";

export default function CartPage() {
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [bannerMessage, setBannerMessage] = useState(() => {
    const cancelledOrderCode = location.state?.paymentCancelled?.orderCode;
    return cancelledOrderCode ? `Đã hủy thanh toán cho đơn ${cancelledOrderCode}.` : "";
  });
  const [cart, setCart] = useState({ guestCartToken: "", items: [] });
  const [selectedCourseId, setSelectedCourseId] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    void loadCart();
  }, [isAuthenticated]);

  useEffect(() => {
    if (!location.state?.paymentCancelled) {
      return;
    }

    navigate(location.pathname, { replace: true, state: null });
  }, [location.pathname, location.state, navigate]);

  async function loadCart() {
    setIsLoading(true);
    setErrorMessage("");

    try {
      const guestCartToken = getGuestCartToken();
      setCart(await getCart(guestCartToken));
    } catch {
      setErrorMessage("Không thể tải giỏ hàng.");
    } finally {
      setIsLoading(false);
    }
  }

  async function handleRemove(courseId) {
    try {
      const guestCartToken = getGuestCartToken();
      const nextCart = await removeCartItem(courseId, guestCartToken);
      setCart(nextCart);
      setSelectedCourseId((current) => (current === courseId ? "" : current));
    } catch {
      setErrorMessage("Không thể xóa khóa học khỏi giỏ.");
    }
  }

  async function handleCheckout() {
    if (!isAuthenticated) {
      navigate("/login", {
        state: {
          from: location
        }
      });
      return;
    }

    setIsSubmitting(true);
    setErrorMessage("");

    try {
      const orders = await createCheckoutOrders(cart.items.filter((item) => !item.alreadyOwned).map((item) => item.courseId));
      const firstOrder = orders[0];

      if (!firstOrder) {
        setErrorMessage("Không còn khóa học cần thanh toán.");
        return;
      }

      navigate(`/payment-orders/${firstOrder.id}`);
    } catch (error) {
      setErrorMessage(error?.response?.data?.message ?? "Không thể tạo đơn thanh toán.");
    } finally {
      setIsSubmitting(false);
    }
  }

  const totalAmount = cart.items
    .filter((item) => !item.alreadyOwned)
    .reduce((sum, item) => sum + item.price, 0);
  const payableItems = cart.items.filter((item) => !item.alreadyOwned);
  const ownedItems = cart.items.filter((item) => item.alreadyOwned);
  const hasSelection = cart.items.some((item) => item.courseId === selectedCourseId);

  return (
    <Section className={styles.page}>
      <div className={styles.header}>
        <div className={styles.headerCopy}>
          <p className="page-eyebrow">Checkout</p>
          <h1>Giỏ hàng</h1>
          <p>Quản lý các khóa học bạn đã chọn để bắt đầu hành trình học tập.</p>
        </div>
        <Button as={Link} to="/courses" variant="ghost">Tiếp tục xem khóa học</Button>
      </div>

      {bannerMessage ? <p className="ui-alert ui-alert--cancelled">{bannerMessage}</p> : null}
      {errorMessage ? <p className="ui-alert ui-alert--error">{errorMessage}</p> : null}

      {isLoading ? (
        <Card variant="shadowed"><p>Đang tải giỏ hàng...</p></Card>
      ) : cart.items.length === 0 ? (
        <Card variant="shadowed">
          <h2>Giỏ hàng đang trống</h2>
          <p>Chưa có khóa học nào cần thanh toán. Hãy quay lại danh sách khóa học để chọn thêm nội dung phù hợp.</p>
        </Card>
      ) : (
        <div className={styles.cartGrid}>
          <div className={styles.items}>
            {cart.items.map((item) => (
              <Card
                className={`${styles.cartItemCard} ${item.alreadyOwned ? styles.cartItemCardOwned : ""} ${selectedCourseId === item.courseId ? styles.cartItemCardSelected : ""}`.trim()}
                key={item.courseId}
                onClick={() => setSelectedCourseId(item.courseId)}
                variant="shadowed"
              >
                <div className={styles.cartItemMedia}>
                  {item.thumbnailUrl ? (
                    <img
                      alt={`Thumbnail khóa học ${item.courseTitle}`}
                      className={styles.cartThumbnail}
                      src={item.thumbnailUrl}
                    />
                  ) : (
                    <div className={styles.cartThumbnailFallback}>
                      <span>{item.category || "Course"}</span>
                    </div>
                  )}
                </div>

                <div className={styles.cartItemBody}>
                  <div className={styles.cartItemTop}>
                    <div className={styles.cartItemSummary}>
                      <h3>{item.courseTitle}</h3>
                      {item.alreadyOwned ? <span className={styles.ownedPill}>Đã sở hữu</span> : null}
                    </div>

                    <div className={styles.cartItemPriceBlock}>
                      <strong className={styles.cartPrice}>
                        {item.alreadyOwned ? "0đ" : formatCurrency(item.price)}
                      </strong>
                    </div>
                  </div>

                  <div className={styles.cartItemBottom}>
                    <button
                      className={styles.removeAction}
                      onClick={(event) => {
                        event.stopPropagation();
                        void handleRemove(item.courseId);
                      }}
                      type="button"
                    >
                      {item.alreadyOwned ? "Loại bỏ khỏi giỏ" : "Xóa"}
                    </button>
                  </div>
                </div>
              </Card>
            ))}
          </div>

          {hasSelection ? (
            <aside className={styles.sidebar}>
              <Card className={styles.summaryCard} variant="shadowed">
                <div className={styles.stack}>
                  <h2>Tổng kết đơn hàng</h2>
                  <div className={styles.summaryRows}>
                    <div className={styles.row}>
                      <span>Số lượng khóa học</span>
                      <strong>{payableItems.length}</strong>
                    </div>
                    <div className={styles.row}>
                      <span>Khóa học đã sở hữu</span>
                      <strong>{ownedItems.length}{ownedItems.length > 0 ? " (Không tính phí)" : ""}</strong>
                    </div>
                    <div className={`${styles.row} ${styles.totalRow}`.trim()}>
                      <span>Tổng cộng</span>
                      <strong className={styles.totalPrice}>{formatCurrency(totalAmount)}</strong>
                    </div>
                  </div>

                  <div className={`${styles.guestNotice} surface-card surface-card--lavender`.trim()}>
                    <p>
                      Bạn đang thanh toán với tư cách <strong>{isAuthenticated ? "Học viên" : "Khách"}</strong>.
                      {!isAuthenticated ? " Vui lòng đăng nhập để lưu trữ vĩnh viễn tiến trình học tập của mình." : " Sau khi thanh toán thành công, bạn có thể vào học ngay."}
                    </p>
                  </div>

                  <Button disabled={isSubmitting || totalAmount <= 0} onClick={() => void handleCheckout()}>
                    {isSubmitting ? "Đang tạo order..." : isAuthenticated ? "Tạo order thanh toán" : "Đăng nhập để checkout"}
                  </Button>
                  <Button as={Link} to="/courses" variant="ghost">Tiếp tục xem khóa học</Button>

                  <div className={styles.summaryFooter}>
                    <span>Thanh toán an toàn với mã hóa SSL</span>
                  </div>
                </div>
              </Card>
            </aside>
          ) : null}
        </div>
      )}
    </Section>
  );
}

function formatCurrency(value) {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0
  }).format(value ?? 0);
}
