import { useAuth } from "../auth/useAuth";

export default function ProfilePage() {
  const { user } = useAuth();

  return (
    <section>
      <h1 style={{ fontSize: 42, marginBottom: 12 }}>Hồ sơ</h1>
      <p>Họ và tên: {user?.fullName}</p>
      <p>Email: {user?.email}</p>
      <p>Vai trò: {user?.role}</p>
    </section>
  );
}
