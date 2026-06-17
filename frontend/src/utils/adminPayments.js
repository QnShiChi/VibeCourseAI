export function formatAdminPaymentDateTime(value) {
  if (!value) {
    return "--";
  }

  return new Intl.DateTimeFormat("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
    timeZone: "Asia/Ho_Chi_Minh"
  }).format(new Date(value));
}

export function formatAdminPaymentCurrency(value) {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0
  }).format(value ?? 0);
}

export function buildAdminPaymentStatusLabel(status) {
  if (status === "Paid") {
    return "Đã thanh toán";
  }

  if (status === "LatePaid") {
    return "Thanh toán muộn";
  }

  if (status === "Pending") {
    return "Chờ thanh toán";
  }

  if (status === "Expired") {
    return "Hết hạn";
  }

  if (status === "Cancelled") {
    return "Đã hủy thanh toán";
  }

  if (status === "Failed") {
    return "Lỗi";
  }

  return status || "Không xác định";
}

export function buildAdminPaymentBadgeClass(status) {
  if (status === "Paid" || status === "LatePaid") {
    return "admin-status-badge admin-status-badge--success";
  }

  if (status === "Pending") {
    return "admin-status-badge admin-status-badge--warning";
  }

  if (status === "Expired" || status === "Failed") {
    return "admin-status-badge admin-status-badge--danger";
  }

  if (status === "Cancelled") {
    return "admin-status-badge admin-status-badge--danger";
  }

  return "admin-status-badge admin-status-badge--muted";
}
