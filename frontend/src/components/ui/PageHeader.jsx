export default function PageHeader({ eyebrow, title, description, actions }) {
  return (
    <div className="page-header">
      {eyebrow ? <p className="page-eyebrow">{eyebrow}</p> : null}
      <div className="page-header__row">
        <div className="section-stack" style={{ gap: "12px" }}>
          <h1>{title}</h1>
          {description ? <p>{description}</p> : null}
        </div>
        {actions ? <div>{actions}</div> : null}
      </div>
    </div>
  );
}
