export default function Button({
  as: Component = "button",
  variant = "primary",
  className = "",
  type,
  children,
  ...props
}) {
  const resolvedType = Component === "button" ? type ?? "button" : undefined;

  return (
    <Component
      className={`ui-button ui-button--${variant} ${className}`.trim()}
      type={resolvedType}
      {...props}
    >
      {children}
    </Component>
  );
}
