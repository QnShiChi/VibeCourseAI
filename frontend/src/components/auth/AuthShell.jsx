import { Link } from "react-router-dom";
import vibecourseLogo from "../../assets/icons/vibecourse-logo.png";
import { useTheme } from "../../theme/ThemeContext";
import styles from "../../styles/AuthPage.module.css";

const AUTH_AMBIENT_IMAGE = "https://lh3.googleusercontent.com/aida-public/AB6AXuAYPOZVwCAf5b5EozQb5JWjUNvm6bVWnV-6O0buWpzmEKq8v1EJgSlpM-_ZjfYQlAyTDNTS2ayXXQRIJHQ25Gk-D1thv7ICBZf4Ox2MIw31gm0soIeIpEVO2UVL9njocBy0Z6mUAB1L2aJL6YvRc9OwARRo9QZd-uf7lGIO7Doda9d_ZBK5e1JHCA3MnR-4DV-eTjWFmbl15FsfdgWZMQkuTWqePJUcN_aZQ_52hDNT4bP8ce12GgC9kyrrcGYRaTOaL_1OGmmdVw";

function BrandLockup({ className, logoClassName }) {
  return (
    <Link aria-label="VibeCourseAI" className={[styles.brandLink, className].filter(Boolean).join(" ")} to="/">
      <img
        alt="VibeCourseAI"
        className={[styles.brandLogo, logoClassName].filter(Boolean).join(" ")}
        src={vibecourseLogo}
      />
    </Link>
  );
}

export function AuthField({
  autoComplete,
  helper,
  icon,
  id,
  label,
  onChange,
  placeholder,
  trailingAction,
  type,
  value
}) {
  return (
    <div className={styles.field}>
      <div className={styles.fieldHeader}>
        <label className={styles.fieldLabel} htmlFor={id}>
          {label}
        </label>
        {trailingAction}
      </div>
      <div className={styles.inputShell}>
        <span aria-hidden="true" className={styles.inputIcon}>
          {icon}
        </span>
        <input
          autoComplete={autoComplete}
          autoCapitalize="none"
          autoCorrect="off"
          className={styles.inputControl}
          id={id}
          onChange={onChange}
          placeholder={placeholder}
          spellCheck={false}
          type={type}
          value={value}
        />
      </div>
      {helper ? <p className={styles.fieldHelper}>{helper}</p> : null}
    </div>
  );
}

export function MailIcon() {
  return (
    <svg aria-hidden="true" fill="none" height="22" viewBox="0 0 24 24" width="22">
      <path d="M4 6.75h16v10.5H4V6.75Z" rx="2" stroke="currentColor" strokeWidth="1.8" />
      <path d="m5.5 8 6.5 5 6.5-5" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.8" />
    </svg>
  );
}

export function LockIcon() {
  return (
    <svg aria-hidden="true" fill="none" height="22" viewBox="0 0 24 24" width="22">
      <path d="M7.5 10V7.75a4.5 4.5 0 1 1 9 0V10" stroke="currentColor" strokeLinecap="round" strokeWidth="1.8" />
      <rect height="9" rx="2" stroke="currentColor" strokeWidth="1.8" width="12" x="6" y="10" />
      <path d="M12 13.5v2.5" stroke="currentColor" strokeLinecap="round" strokeWidth="1.8" />
    </svg>
  );
}

export function PersonIcon() {
  return (
    <svg aria-hidden="true" fill="none" height="22" viewBox="0 0 24 24" width="22">
      <path d="M12 11a3.25 3.25 0 1 0 0-6.5A3.25 3.25 0 0 0 12 11Z" stroke="currentColor" strokeWidth="1.8" />
      <path d="M5.5 18.25c1.25-2.667 3.417-4 6.5-4 3.083 0 5.25 1.333 6.5 4" stroke="currentColor" strokeLinecap="round" strokeWidth="1.8" />
    </svg>
  );
}

function GoogleIcon() {
  return (
    <svg aria-hidden="true" height="18" viewBox="0 0 24 24" width="18">
      <path d="M21.8 12.23c0-.72-.06-1.25-.19-1.8H12v3.4h5.65c-.11.84-.68 2.1-1.95 2.95l-.02.11 2.83 2.2.2.02c1.8-1.67 3.09-4.12 3.09-6.88Z" fill="#4285F4" />
      <path d="M12 22c2.76 0 5.08-.91 6.77-2.47l-3.01-2.33c-.81.56-1.89.96-3.76.96-2.7 0-5-1.78-5.82-4.25l-.1.01-2.95 2.29-.03.09A10.23 10.23 0 0 0 12 22Z" fill="#34A853" />
      <path d="M6.18 13.91A6.12 6.12 0 0 1 5.84 12c0-.66.12-1.3.32-1.91l-.01-.13-2.98-2.33-.1.05A10.07 10.07 0 0 0 2 12c0 1.6.39 3.1 1.07 4.32l3.11-2.41Z" fill="#FBBC05" />
      <path d="M12 5.84c2.36 0 3.96 1.02 4.87 1.87l3.55-3.47C18.68 2.64 15.51 2 12 2a10.23 10.23 0 0 0-8.93 5.68l3.09 2.41c.84-2.47 3.14-4.25 5.84-4.25Z" fill="#EA4335" />
    </svg>
  );
}

function FacebookIcon() {
  return (
    <svg aria-hidden="true" height="18" viewBox="0 0 24 24" width="18">
      <path d="M24 12.07C24 5.4 18.63 0 12 0S0 5.4 0 12.07c0 6.03 4.39 11.03 10.13 11.93v-8.44H7.08v-3.49h3.05V9.41c0-3.04 1.79-4.72 4.53-4.72 1.31 0 2.68.24 2.68.24v2.99h-1.51c-1.49 0-1.96.94-1.96 1.9v2.28h3.33l-.53 3.49h-2.8V24C19.61 23.1 24 18.1 24 12.07Z" fill="#1877F2" />
      <path d="m16.67 15.56.53-3.49h-3.33V9.79c0-.96.47-1.9 1.96-1.9h1.51V4.9s-1.37-.24-2.68-.24c-2.74 0-4.53 1.69-4.53 4.72v2.69H7.08v3.49h3.05V24a12.2 12.2 0 0 0 3.74 0v-8.44h2.8Z" fill="#fff" />
    </svg>
  );
}

function SocialButton({ href, icon, label }) {
  if (href) {
    return (
      <a className={styles.socialButton} href={href}>
        <span aria-hidden="true" className={styles.socialIcon}>
          {icon}
        </span>
        <span>{label}</span>
      </a>
    );
  }

  return (
    <button className={styles.socialButton} type="button">
      <span aria-hidden="true" className={styles.socialIcon}>
        {icon}
      </span>
      <span>{label}</span>
    </button>
  );
}

export default function AuthShell({
  alternateCta,
  alternateTo,
  children,
  description,
  footerLabel,
  footerLinkLabel,
  footerLinkTo,
  googleAuthUrl,
  heading,
  showcaseAudience,
  showcaseDescription,
  showcaseEyebrow,
  showcaseMeta,
  showcaseTitle
}) {
  const { theme, toggleTheme } = useTheme();
  const nextThemeLabel = theme === "light" ? "dark" : "light";
  const themeIcon = theme === "light" ? "☾" : "☀";

  return (
    <section className={styles.authShell} data-testid="auth-shell" data-theme={theme}>
      <header className={styles.authHeader}>
        <div className={styles.authHeaderInner}>
          <div className={styles.authHeaderActions}>
            <Link className={styles.authHeaderLink} to="/">
              Trang chủ
            </Link>
            <button
              aria-label={`Chuyển sang ${nextThemeLabel} mode`}
              className={styles.authThemeToggle}
              onClick={toggleTheme}
              title={`Chuyển sang ${nextThemeLabel} mode`}
              type="button"
            >
              <span aria-hidden="true" className={styles.authThemeToggleIcon}>
                {themeIcon}
              </span>
            </button>
            <Link className={styles.authHeaderCta} to={alternateTo}>
              {alternateCta}
            </Link>
          </div>
        </div>
      </header>

      <div className={styles.authStage}>
        <aside className={styles.authShowcase} style={{ backgroundImage: `url(${AUTH_AMBIENT_IMAGE})` }}>
          <div className={styles.showcaseOverlay} />
          <BrandLockup className={styles.showcaseBrand} logoClassName={styles.showcaseBrandLogo} />
          <div className={styles.showcaseCard}>
            <div className={styles.showcaseBadgeRow}>
              <span className={styles.showcaseBadge}>{showcaseEyebrow}</span>
              <span aria-hidden="true" className={styles.showcaseBadgeDot} />
            </div>
            <h2>{showcaseTitle}</h2>
            <p>{showcaseDescription}</p>
            <div className={styles.showcaseMetaRow}>
              <div className={styles.showcaseCluster}>
                <span className={styles.clusterOrbPrimary} />
                <span className={styles.clusterOrbSecondary} />
                <span className={styles.clusterOrbAccent} />
              </div>
              <div className={styles.showcaseMetaCopy}>
                <strong>{showcaseAudience}</strong>
                <span>{showcaseMeta}</span>
              </div>
            </div>
          </div>
        </aside>

        <div className={styles.authPanel}>
          <div className={styles.authPanelInner}>
            <div className={styles.mobileBrandWrap}>
              <BrandLockup />
            </div>

            <div className={styles.authCard}>
              <div className={styles.authIntro}>
                <span className={styles.authEyebrow}>{showcaseEyebrow}</span>
                <h1>{heading}</h1>
                <p>{description}</p>
              </div>

              {children}

              <div className={styles.authDivider}>
                <span />
                <p>Hoặc tiếp tục với</p>
                <span />
              </div>

              <div className={styles.socialGrid}>
                <SocialButton href={googleAuthUrl} icon={<GoogleIcon />} label="Google" />
                <SocialButton icon={<FacebookIcon />} label="Facebook" />
              </div>

              <p className={styles.authSwitch}>
                {footerLabel}{" "}
                <Link className={styles.authSwitchLink} to={footerLinkTo}>
                  {footerLinkLabel}
                </Link>
              </p>
            </div>

            <footer className={styles.authFooter}>
              <span>Điều khoản</span>
              <span>Bảo mật</span>
              <span>Trợ giúp</span>
              <p>© 2026 VibeCourseAI. All rights reserved.</p>
            </footer>
          </div>
        </div>
      </div>
    </section>
  );
}
