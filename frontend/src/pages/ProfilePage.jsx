import Card from "../components/ui/Card";
import PageHeader from "../components/ui/PageHeader";
import Section from "../components/ui/Section";
import { useAuth } from "../auth/useAuth";

export default function ProfilePage() {
  const { user } = useAuth();

  return (
    <Section className="section-stack">
      <PageHeader
        eyebrow="Tai khoan"
        title="Hồ sơ"
        description="Tổng hợp thông tin tài khoản hiện tại để người dùng theo dõi vai trò và trạng thái truy cập của mình."
      />

      <Card variant="shadowed">
        <div className="info-grid">
          <div className="profile-detail">
            <span className="profile-detail__label">Họ và tên</span>
            <span className="profile-detail__value">{user?.fullName}</span>
          </div>
          <div className="profile-detail">
            <span className="profile-detail__label">Email</span>
            <span className="profile-detail__value">{user?.email}</span>
          </div>
          <div className="profile-detail">
            <span className="profile-detail__label">Vai trò</span>
            <span className="profile-detail__value">{user?.role}</span>
          </div>
        </div>
      </Card>
    </Section>
  );
}
