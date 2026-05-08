export default function CoursesPage() {
  return (
    <section>
      <h1 style={{ fontSize: 42, marginBottom: 12 }}>Khóa học</h1>
      <p style={{ maxWidth: 720, lineHeight: 1.6 }}>
        Danh sách khóa học sẽ được lấy từ API `GET /api/courses`. Hiện tại backend
        seed sẵn một khóa học mẫu để kiểm tra luồng kết nối cơ bản.
      </p>
    </section>
  );
}
