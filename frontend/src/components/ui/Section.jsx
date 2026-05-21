export default function Section({ className = "", children }) {
  return <section className={`section ${className}`.trim()}>{children}</section>;
}
