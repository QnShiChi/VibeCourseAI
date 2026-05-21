import { Link } from "react-router-dom";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import PageHeader from "../components/ui/PageHeader";
import Section from "../components/ui/Section";

const metrics = [
  { label: "Người dùng", value: "--", tone: "mint" },
  { label: "Đề cương", value: "--", tone: "lavender" },
  { label: "Khóa học", value: "01", tone: "saffron" },
  { label: "Generation jobs", value: "--", tone: "mint" }
];

export default function DashboardPage() {
  return (
    <Section className="section-stack">
      <PageHeader
        eyebrow="Admin"
        title="Dashboard"
        description="Khung quản trị đã được chuẩn hóa để sẵn sàng nối số liệu thật cho người dùng, đề cương, khóa học và generation jobs."
        actions={<Button variant="ghost">Làm mới dữ liệu</Button>}
      />

      <div className="metric-grid">
        {metrics.map((metric) => (
          <Card key={metric.label} tone={metric.tone} variant="shadowed">
            <span className="ui-badge">Tổng quan</span>
            <div className="metric-card__value">{metric.value}</div>
            <p>{metric.label}</p>
          </Card>
        ))}
      </div>

      <Card variant="shadowed">
        <h2>Quick actions</h2>
        <p>Các thao tác nghiệp vụ sẽ được nối dần khi các module syllabus import và generation job được triển khai.</p>
        <div className="quick-actions">
          <Button as={Link} to="/admin/syllabuses">Import đề cương</Button>
          <Button variant="ghost">Tạo khóa học</Button>
          <Button variant="ghost">Xem khóa học</Button>
        </div>
      </Card>
    </Section>
  );
}
