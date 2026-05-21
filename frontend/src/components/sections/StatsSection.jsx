import { useEffect, useMemo, useState } from "react";
import Card from "../ui/Card";
import styles from "../../styles/HomePage.module.css";
import { useRevealOnScroll } from "../../hooks/useRevealOnScroll";

function StatCounter({ target, suffix = "" }) {
  const [value, setValue] = useState(0);

  useEffect(() => {
    let frame = 0;
    const duration = 900;
    const stepMs = 32;
    const totalFrames = Math.max(1, Math.round(duration / stepMs));

    const interval = window.setInterval(() => {
      frame += 1;
      const nextValue = Math.round((target * frame) / totalFrames);
      setValue(nextValue >= target ? target : nextValue);

      if (frame >= totalFrames) {
        window.clearInterval(interval);
      }
    }, stepMs);

    return () => window.clearInterval(interval);
  }, [target]);

  return <span>{value}{suffix}</span>;
}

export default function StatsSection({ items }) {
  const { ref, isVisible } = useRevealOnScroll();
  const stats = useMemo(() => items ?? [], [items]);

  return (
    <section
      ref={ref}
      className={`${styles.statsSection} ${isVisible ? styles.isVisible : ""}`.trim()}
      data-reveal="true"
    >
      <div className={styles.statsHeader}>
        <span className="ui-badge">Momentum</span>
        <div>
          <h2>Những con số cho thấy một nền tảng đang thật sự vận hành</h2>
          <p>Tập trung vào sản xuất khóa học nhanh, learner delivery rõ ràng và trải nghiệm quản trị nhất quán.</p>
        </div>
      </div>

      <div className={styles.statsGrid}>
        {stats.map((item) => (
          <Card key={item.label} className={styles.statsCard} tone={item.tone} variant="shadowed">
            <span className={styles.statsValue}>
              {isVisible ? <StatCounter suffix={item.suffix} target={item.value} /> : `0${item.suffix ?? ""}`}
            </span>
            <strong>{item.label}</strong>
            <p>{item.description}</p>
          </Card>
        ))}
      </div>
    </section>
  );
}
