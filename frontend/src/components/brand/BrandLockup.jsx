import { Link } from "react-router-dom";
import vibecourseMark from "../../assets/icons/vibecourse-mark.png";

export default function BrandLockup({ className = "", iconClassName = "", wordmarkClassName = "", to = "/" }) {
  return (
    <Link aria-label="VibeCourseAI" className={`brand-lockup ${className}`.trim()} to={to}>
      <img
        alt="VibeCourseAI"
        className={`brand-lockup__icon ${iconClassName}`.trim()}
        src={vibecourseMark}
      />
      <span className={`brand-lockup__wordmark ${wordmarkClassName}`.trim()}>
        <span className="brand-lockup__wordmark-main">VibeCourse</span>
        <span className="brand-lockup__wordmark-accent">AI</span>
      </span>
    </Link>
  );
}
