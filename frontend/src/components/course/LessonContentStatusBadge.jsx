export default function LessonContentStatusBadge({ status, type = "generic" }) {
  const normalizedStatus = status || "NotGenerated";
  const className = `status-badge ${resolveStatusClassName(normalizedStatus)}`.trim();
  const typeLabel = resolveTypeLabel(type);
  const statusLabel = resolveStatusLabel(normalizedStatus);
  const statusDescription = resolveStatusDescription(type, normalizedStatus);

  return (
    <span
      className={className}
      title={statusDescription}
      aria-label={`${typeLabel}: ${statusLabel}. ${statusDescription}`}
    >
      {typeLabel}: {statusLabel}
    </span>
  );
}

function resolveTypeLabel(type) {
  switch (type) {
    case "content":
      return "Nội dung";
    case "audio":
      return "Audio";
    case "video":
      return "Video";
    default:
      return "Trạng thái";
  }
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
    case "CompletedWithWarnings":
      return "Đã xong";
    case "ManuallyEdited":
      return "Đã sửa tay";
    case "Failed":
      return "Lỗi";
    default:
      return "Chưa tạo";
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
    case "CompletedWithWarnings":
    case "ManuallyEdited":
      return "status-badge--completed";
    case "Failed":
      return "status-badge--failed";
    default:
      return "status-badge--pending";
  }
}

function resolveStatusDescription(type, status) {
  const subject = resolveTypeSubject(type);

  switch (status) {
    case "Pending":
      return `${subject} đang chờ được xử lý.`;
    case "Processing":
    case "GeneratingNarration":
    case "GeneratingAudio":
    case "GeneratingFrames":
    case "RenderingVideo":
    case "GeneratingLessonVideo":
      return `${subject} đang được generate.`;
    case "Completed":
      return `${subject} đã generate xong.`;
    case "CompletedWithWarnings":
      return `${subject} đã generate xong nhưng có cảnh báo cần kiểm tra lại.`;
    case "ManuallyEdited":
      return `${subject} đã được chỉnh sửa thủ công sau khi generate.`;
    case "Failed":
      return `${subject} generate lỗi. Cần chạy lại.`;
    default:
      return `${subject} chưa được generate.`;
  }
}

function resolveTypeSubject(type) {
  switch (type) {
    case "content":
      return "Nội dung bài học";
    case "audio":
      return "Audio bài học";
    case "video":
      return "Video bài học";
    default:
      return "Tiến trình này";
  }
}
