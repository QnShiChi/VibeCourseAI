export default function LessonContentStatusBadge({ status }) {
  const normalizedStatus = status || "NotGenerated";
  const className = `status-badge ${resolveStatusClassName(normalizedStatus)}`.trim();

  return <span className={className}>{resolveStatusLabel(normalizedStatus)}</span>;
}

function resolveStatusLabel(status) {
  switch (status) {
    case "Pending":
      return "Đang chờ";
    case "Processing":
    case "GeneratingNarration":
    case "GeneratingAudio":
    case "GeneratingFrames":
    case "RenderingVideo":
    case "GeneratingLessonVideo":
      return "Đang xử lý";
    case "Completed":
      return "Đã xong";
    case "ManuallyEdited":
      return "Đã sửa tay";
    case "Failed":
      return "Lỗi";
    default:
      return "Chưa generate";
  }
}

function resolveStatusClassName(status) {
  switch (status) {
    case "Pending":
      return "status-badge--pending";
    case "Processing":
    case "GeneratingNarration":
    case "GeneratingAudio":
    case "GeneratingFrames":
    case "RenderingVideo":
    case "GeneratingLessonVideo":
      return "status-badge--processing";
    case "Completed":
    case "ManuallyEdited":
      return "status-badge--completed";
    case "Failed":
      return "status-badge--failed";
    default:
      return "status-badge--pending";
  }
}
