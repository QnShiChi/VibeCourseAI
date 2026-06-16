import { describe, expect, it } from "vitest";
import { buildAdminPaymentBadgeClass, buildAdminPaymentStatusLabel } from "./adminPayments";

describe("adminPayments", () => {
  it("maps payment statuses to the synced admin badge colors", () => {
    expect(buildAdminPaymentBadgeClass("Paid")).toBe("admin-status-badge admin-status-badge--success");
    expect(buildAdminPaymentBadgeClass("LatePaid")).toBe("admin-status-badge admin-status-badge--success");
    expect(buildAdminPaymentBadgeClass("Pending")).toBe("admin-status-badge admin-status-badge--warning");
    expect(buildAdminPaymentBadgeClass("Cancelled")).toBe("admin-status-badge admin-status-badge--danger");
    expect(buildAdminPaymentBadgeClass("Expired")).toBe("admin-status-badge admin-status-badge--danger");
    expect(buildAdminPaymentBadgeClass("Failed")).toBe("admin-status-badge admin-status-badge--danger");
  });

  it("keeps the cancelled label localized", () => {
    expect(buildAdminPaymentStatusLabel("Cancelled")).toBe("Đã hủy thanh toán");
  });
});
