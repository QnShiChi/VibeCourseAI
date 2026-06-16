import { act, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import CartPage from "./CartPage";
import PaymentOrderPage from "./PaymentOrderPage";

const { mockGetPaymentOrder, mockCancelPaymentOrder, mockGetCart, mockUseAuth } = vi.hoisted(() => ({
  mockGetPaymentOrder: vi.fn(),
  mockCancelPaymentOrder: vi.fn(),
  mockGetCart: vi.fn(),
  mockUseAuth: vi.fn()
}));

vi.mock("../api/paymentService", () => ({
  getPaymentOrder: (...args) => mockGetPaymentOrder(...args),
  cancelPaymentOrder: (...args) => mockCancelPaymentOrder(...args)
}));

vi.mock("../api/cartService", () => ({
  getCart: (...args) => mockGetCart(...args),
  removeCartItem: vi.fn()
}));

vi.mock("../auth/useAuth", () => ({
  useAuth: () => mockUseAuth()
}));

vi.mock("../utils/cartStorage", () => ({
  getGuestCartToken: () => "guest-token"
}));

function buildOrder(overrides = {}) {
  return {
    id: "order-1",
    orderCode: "ORD-001",
    courseId: "course-1",
    courseTitle: "React nâng cao",
    bankCode: "TPBANK",
    bankName: "TPBank",
    bankAccountNumber: "1234567890",
    accountHolderName: "Vibe Course",
    transferContent: "PAY-ORDER-001",
    amount: 150000,
    qrImageUrl: "https://example.com/qr.png",
    paidAt: null,
    expiresAt: "2026-06-16T10:10:00",
    isExpired: false,
    status: "Pending",
    ...overrides
  };
}

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/payment-orders/order-1"]}>
      <Routes>
        <Route path="/payment-orders/:orderId" element={<PaymentOrderPage />} />
        <Route path="/cart" element={<CartPage />} />
      </Routes>
    </MemoryRouter>
  );
}

function getStatusBadge(text) {
  return screen.getAllByText(text, { selector: "span" }).find((element) => element.className.includes("paymentStatus"));
}

async function flushPromises() {
  await act(async () => {
    await Promise.resolve();
  });
}

beforeEach(() => {
  vi.useFakeTimers();
});

afterEach(() => {
  vi.useRealTimers();
  mockGetPaymentOrder.mockReset();
  mockCancelPaymentOrder.mockReset();
  mockGetCart.mockReset();
  mockUseAuth.mockReset();
});

describe("PaymentOrderPage", () => {
  beforeEach(() => {
    mockUseAuth.mockReturnValue({
      isAuthenticated: true
    });
    mockGetCart.mockResolvedValue({
      guestCartToken: "guest-token",
      items: []
    });
  });

  it("does not open a modal on the initial pending load", async () => {
    mockGetPaymentOrder.mockResolvedValue(buildOrder());

    renderPage();
    await flushPromises();

    expect(screen.getByText("Đang chờ thanh toán...")).toBeInTheDocument();
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("opens the expired modal when the first loaded order is already expired", async () => {
    mockGetPaymentOrder.mockResolvedValue(buildOrder({ status: "Expired", isExpired: true }));

    renderPage();
    await flushPromises();

    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Đơn hàng đã hết thời gian thanh toán" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Về giỏ hàng" })).toHaveAttribute("href", "/cart");
  });

  it("opens the expired modal after polling detects the order timed out and does not reopen it for repeated expired polls", async () => {
    mockGetPaymentOrder
      .mockResolvedValueOnce(buildOrder())
      .mockResolvedValueOnce(buildOrder({ status: "Expired", isExpired: true }))
      .mockResolvedValue(buildOrder({ status: "Expired", isExpired: true }));

    renderPage();
    await flushPromises();

    expect(screen.getByText("Đang chờ thanh toán...")).toBeInTheDocument();

    await act(async () => {
      await vi.advanceTimersByTimeAsync(3000);
    });
    await flushPromises();

    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Đơn hàng đã hết thời gian thanh toán" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Về giỏ hàng" })).toHaveAttribute("href", "/cart");
    expect(getStatusBadge("Đơn hàng đã hết hạn")?.className).toContain("paymentStatusError");

    fireEvent.click(screen.getByRole("button", { name: "Đóng thông báo" }));
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Về giỏ hàng để tạo lại thanh toán" })).toHaveAttribute("href", "/cart");

    await act(async () => {
      await vi.advanceTimersByTimeAsync(3000);
    });
    await flushPromises();

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("opens the success modal after payment is confirmed and keeps the learn CTA in the background after dismiss", async () => {
    mockGetPaymentOrder
      .mockResolvedValueOnce(buildOrder())
      .mockResolvedValueOnce(buildOrder({
        status: "Paid",
        paidAt: "2026-06-16T10:03:00"
      }))
      .mockResolvedValue(buildOrder({
        status: "Paid",
        paidAt: "2026-06-16T10:03:00"
      }));

    renderPage();
    await flushPromises();

    expect(screen.getByText("Đang chờ thanh toán...")).toBeInTheDocument();

    await act(async () => {
      await vi.advanceTimersByTimeAsync(3000);
    });
    await flushPromises();

    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Thanh toán thành công" })).toBeInTheDocument();
    expect(getStatusBadge("Thanh toán thành công")?.className).toContain("paymentStatusSuccess");

    const learnLinks = screen.getAllByRole("link", { name: "Vào học ngay" });
    expect(learnLinks.length).toBeGreaterThanOrEqual(1);
    learnLinks.forEach((link) => {
      expect(link).toHaveAttribute("href", "/courses/course-1/learn");
    });

    fireEvent.click(screen.getByRole("button", { name: "Đóng thông báo" }));
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();

    expect(screen.getByRole("link", { name: "Vào học ngay" })).toHaveAttribute("href", "/courses/course-1/learn");
  });

  it("shows cancel payment action below qr for pending orders and redirects to cart with cancelled banner after confirmation", async () => {
    mockGetPaymentOrder.mockResolvedValue(buildOrder());
    mockCancelPaymentOrder.mockResolvedValue(buildOrder({ status: "Cancelled" }));

    renderPage();
    await flushPromises();

    expect(screen.getByRole("button", { name: "Hủy thanh toán" })).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Hủy thanh toán" }));

    expect(screen.getByRole("heading", { name: "Bạn có chắc muốn hủy thanh toán này không?" })).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Xác nhận hủy" }));
    await flushPromises();

    expect(mockCancelPaymentOrder).toHaveBeenCalledWith("order-1");
    expect(screen.getByText("Đã hủy thanh toán cho đơn ORD-001.")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Giỏ hàng" })).toBeInTheDocument();
  });
});
