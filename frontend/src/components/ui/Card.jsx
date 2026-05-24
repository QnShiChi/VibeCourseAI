export default function Card({ variant = "default", tone, className = "", children, ...props }) {
  const toneClass = tone ? `surface-card--${tone}` : "";
  const variantClass = variant === "shadowed"
    ? "surface-card surface-card--shadowed"
    : variant === "highlight"
      ? "surface-card surface-card--highlight"
      : "surface-card";

  return <div {...props} className={`${variantClass} ${toneClass} ${className}`.trim()}>{children}</div>;
}
