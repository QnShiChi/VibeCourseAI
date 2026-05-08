export default function LoginPage() {
  return (
    <section style={{ padding: 40, fontFamily: "Georgia, serif", maxWidth: 480 }}>
      <h1>Đăng nhập</h1>
      <p>Vui lòng nhập thông tin tài khoản để truy cập hệ thống học tập.</p>

      <form style={{ display: "grid", gap: 16, marginTop: 24 }}>
        <label htmlFor="email" style={{ display: "grid", gap: 8 }}>
          <span>Email</span>
          <input id="email" type="email" placeholder="Nhập email của bạn" />
        </label>

        <label htmlFor="password" style={{ display: "grid", gap: 8 }}>
          <span>Mật khẩu</span>
          <input id="password" type="password" placeholder="Nhập mật khẩu" />
        </label>

        <button type="submit">Đăng nhập vào hệ thống</button>
      </form>

      <p style={{ marginTop: 24 }}>
        Bạn chưa có tài khoản? <a href="/register">Đăng ký ngay</a>
      </p>
    </section>
  );
}
