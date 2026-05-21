import { Link } from "react-router-dom";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import Section from "../components/ui/Section";

const highlights = [
  {
    title: "Học tập tập trung",
    description: "Theo dõi các khóa học đã phát hành, giữ trải nghiệm học rõ ràng và dễ tiếp cận.",
    tone: "saffron"
  },
  {
    title: "Quản trị có cấu trúc",
    description: "Dashboard và các màn quản trị được chuẩn hóa để mở rộng sang import đề cương và generation job.",
    tone: "mint"
  },
  {
    title: "Thiết kế đồng bộ",
    description: "Toàn bộ hệ thống đang được đưa về cùng một design language theo `DESIGN.md`.",
    tone: "lavender"
  }
];

export default function HomePage() {
  return (
    <Section className="section-stack">
      <div className="hero">
        <div className="hero__content">
          <span className="ui-badge">Course video platform</span>
          <h1>Tạo, quản lý và học khóa học trong một giao diện sáng, rõ và đồng bộ.</h1>
          <p>
            VibeCourseAI tập trung vào trải nghiệm học tập và vận hành hệ thống tạo video khóa học,
            với giao diện được chuẩn hóa để mở rộng cho các flow syllabus, generation và lesson.
          </p>
          <div className="hero__actions">
            <Button as={Link} to="/register">Bắt đầu ngay</Button>
            <Button as={Link} to="/courses" variant="ghost">Xem khóa học</Button>
          </div>
        </div>

        <div className="hero__panel">
          <Card tone="mint" variant="shadowed">
            <span className="ui-badge">Admin</span>
            <h2>Shell quản trị sẵn sàng</h2>
            <p>Dashboard đã có khung giao diện để bổ sung thống kê, quick actions và điều phối các tác vụ AI tiếp theo.</p>
          </Card>
          <Card tone="saffron">
            <span className="ui-badge">Learner</span>
            <p>Luồng đăng ký, đăng nhập, xem khóa học và quản lý hồ sơ đang được chuẩn hóa theo cùng một visual system.</p>
          </Card>
        </div>
      </div>

      <div className="card-grid">
        {highlights.map((item) => (
          <Card key={item.title} tone={item.tone} variant="shadowed">
            <h2>{item.title}</h2>
            <p>{item.description}</p>
          </Card>
        ))}
      </div>
    </Section>
  );
}
