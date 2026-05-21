import Button from "../ui/Button";
import Card from "../ui/Card";
import styles from "../../styles/HomePage.module.css";
import { useRevealOnScroll } from "../../hooks/useRevealOnScroll";

export default function FeatureSection({
  title,
  description,
  bullets = [],
  cta,
  tone = "mint",
  layout = "content-left",
  visual,
  className = ""
}) {
  const { ref, isVisible } = useRevealOnScroll();
  const layoutClass = layout === "content-right" ? styles.featureSectionReverse : layout === "centered" ? styles.featureSectionCentered : "";

  return (
    <section
      ref={ref}
      className={`${styles.featureSection} ${layoutClass} ${isVisible ? styles.isVisible : ""} ${className}`.trim()}
      data-reveal="true"
    >
      <div className={styles.featureContent}>
        <h2>{title}</h2>
        <p>{description}</p>

        {bullets.length ? (
          <ul className={styles.featureList}>
            {bullets.map((bullet) => (
              <li key={bullet}>{bullet}</li>
            ))}
          </ul>
        ) : null}

        {cta ? (
          <div className={styles.featureActions}>
            <Button as={cta.as} to={cta.to} variant={cta.variant ?? "primary"}>
              {cta.label}
            </Button>
          </div>
        ) : null}
      </div>

      <Card className={styles.featureVisual} tone={tone} variant="shadowed">
        {visual}
      </Card>
    </section>
  );
}
