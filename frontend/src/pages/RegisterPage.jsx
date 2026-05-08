export default function RegisterPage() {
  return (
    <section style={{ padding: 40, fontFamily: "Georgia, serif", maxWidth: 480 }}>
      <h1>Đăng ký</h1>
      <p>Tạo tài khoản mới để bắt đầu học các khóa học trên hệ thống.</p>

      <form style={{ display: "grid", gap: 16, marginTop: 24 }}>
        <label htmlFor="fullName" style={{ display: "grid", gap: 8 }}>
          <span>Họ và tên</span>
          <input id="fullName" type="text" placeholder="Nhập họ và tên" />
        </label>

        <label htmlFor="email" style={{ display: "grid", gap: 8 }}>
          <span>Email</span>
          <input id="email" type="email" placeholder="Nhập email của bạn" />
        </label>

        <label htmlFor="password" style={{ display: "grid", gap: 8 }}>
          <span>Mật khẩu</span>
          <input id="password" type="password" placeholder="Tạo mật khẩu" />
        </label>

        <button type="submit">Tạo tài khoản</button>
      </form>

      <p style={{ marginTop: 24 }}>
        Bạn đã có tài khoản? <a href="/login">Đăng nhập</a>
      </p>
    </section>
  );
}
