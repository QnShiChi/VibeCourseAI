import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import CartPage from "./CartPage";

const mockGetCart = vi.fn();
const mockUseAuth = vi.fn();

vi.mock("../api/cartService", () => ({
  getCart: (...args) => mockGetCart(...args),
  removeCartItem: vi.fn()
}));

vi.mock("../api/paymentService", () => ({
  createCheckoutOrders: vi.fn()
}));

vi.mock("../auth/useAuth", () => ({
  useAuth: () => mockUseAuth()
}));

vi.mock("../utils/cartStorage", () => ({
  getGuestCartToken: () => "guest-token"
}));

describe("CartPage", () => {
  it("shows cancelled payment banner when redirected from payment page", async () => {
    mockUseAuth.mockReturnValue({
      isAuthenticated: true
    });
    mockGetCart.mockResolvedValue({
      guestCartToken: "guest-token",
      items: []
    });

    render(
      <MemoryRouter
        initialEntries={[
          {
            pathname: "/cart",
            state: {
              paymentCancelled: {
                orderCode: "ORD-001"
              }
            }
          }
        ]}
      >
        <CartPage />
      </MemoryRouter>
    );

    expect(await screen.findByText("Đã hủy thanh toán cho đơn ORD-001.")).toBeInTheDocument();
  });
});
