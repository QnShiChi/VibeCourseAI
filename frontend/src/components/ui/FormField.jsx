export default function FormField({ id, label, children }) {
  return (
    <label className="form-field" htmlFor={id}>
      <span className="form-field__label">{label}</span>
      {children}
    </label>
  );
}
