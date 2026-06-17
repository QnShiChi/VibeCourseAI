import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import AdminPaymentsPage from "./AdminPaymentsPage";

const mockGetAdminPaymentOrders = vi.fn();

vi.mock("../api/paymentService", () => ({
  getAdminPaymentOrders: (...args) => mockGetAdminPaymentOrders(...args)
}));

function renderPage() {
  render(
    <MemoryRouter>
      <AdminPaymentsPage />
    </MemoryRouter>
  );
}

describe("AdminPaymentsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetAdminPaymentOrders.mockResolvedValue([
      {
        paymentOrderId: "payment-1",
        orderCode: "VCPENDING001",
        userFullName: "Phuong Nguyen",
        userEmail: "phuong@example.com",
        courseTitle: "Tri Tue Nhan Tao Ung Dung",
        amount: 3000,
        status: "Pending",
        createdAt: "2026-06-15T11:00:00Z",
        expiresAt: "2026-06-15T11:15:00Z",
        paidAt: null
      },
      {
        paymentOrderId: "payment-2",
        orderCode: "VCPAID001",
        userFullName: "Phuong Nguyen",
        userEmail: "phuong@example.com",
        courseTitle: "Lap Trinh Huong Doi Tuong",
        amount: 3000,
        status: "Paid",
        createdAt: "2026-06-15T10:30:00Z",
        expiresAt: "2026-06-15T10:45:00Z",
        paidAt: "2026-06-15T10:55:00Z"
      },
      {
        paymentOrderId: "payment-3",
        orderCode: "VCEXPIRED001",
        userFullName: "Second Learner",
        userEmail: "second@example.com",
        courseTitle: "Khoa hoc bi loi",
        amount: 5000,
        status: "Expired",
        createdAt: "2026-06-15T12:00:00Z",
        expiresAt: "2026-06-15T12:15:00Z",
        paidAt: null
      }
    ]);
  });

  it("renders list and supports local filtering", async () => {
    renderPage();

    expect(await screen.findByRole("heading", { name: "Quản lý hóa đơn" })).toBeInTheDocument();
    expect(screen.getByText("VCPENDING001")).toBeInTheDocument();
    expect(screen.getByText("VCPAID001")).toBeInTheDocument();
    expect(screen.getByText("VCEXPIRED001")).toBeInTheDocument();
    expect(screen.getAllByRole("link", { name: "Xem chi tiết" })[0]).toHaveAttribute("href", "/admin/payments/payment-1");

    fireEvent.change(screen.getByPlaceholderText("Tìm theo mã đơn, người mua, email hoặc khóa học..."), {
      target: { value: "second@example.com" }
    });

    expect(screen.queryByText("VCPENDING001")).not.toBeInTheDocument();
    expect(screen.queryByText("VCPAID001")).not.toBeInTheDocument();
    expect(screen.getByText("VCEXPIRED001")).toBeInTheDocument();

    fireEvent.change(screen.getByPlaceholderText("Tìm theo mã đơn, người mua, email hoặc khóa học..."), {
      target: { value: "" }
    });
    fireEvent.click(screen.getByRole("button", { name: "Đã thanh toán" }));

    expect(screen.queryByText("VCPENDING001")).not.toBeInTheDocument();
    expect(screen.getByText("VCPAID001")).toBeInTheDocument();
    expect(screen.queryByText("VCEXPIRED001")).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Tất cả" }));
    fireEvent.change(screen.getByLabelText("Từ ngày"), {
      target: { value: "2026-06-15" }
    });
    fireEvent.change(screen.getByLabelText("Đến ngày"), {
      target: { value: "2026-06-15" }
    });

    expect(screen.getByText("VCPENDING001")).toBeInTheDocument();
    expect(screen.getByText("VCPAID001")).toBeInTheDocument();
    expect(screen.getByText("VCEXPIRED001")).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText("Đến ngày"), {
      target: { value: "2026-06-14" }
    });

    expect(screen.queryByText("VCPENDING001")).not.toBeInTheDocument();
    expect(screen.queryByText("VCPAID001")).not.toBeInTheDocument();
    expect(screen.queryByText("VCEXPIRED001")).not.toBeInTheDocument();
  });
});
