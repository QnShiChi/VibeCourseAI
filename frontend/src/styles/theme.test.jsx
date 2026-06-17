import { readFileSync } from "node:fs";
import { describe, expect, it } from "vitest";

const themeCss = readFileSync("src/styles/theme.css", "utf8");

describe("theme accent surfaces", () => {
  it("uses accent ink for badges and active grouped navigation triggers", () => {
    expect(themeCss).toMatch(/\.app-nav__group-trigger--active\s*\{[^}]*background:\s*var\(--color-accent-green\);[^}]*color:\s*var\(--color-accent-ink\);/s);
    expect(themeCss).toMatch(/\.ui-badge\s*\{[^}]*background:\s*var\(--color-accent-green\);[^}]*color:\s*var\(--color-accent-ink\);/s);
  });

  it("defines stronger shared error alert colors for light and dark themes", () => {
    expect(themeCss).toMatch(/--app-error-bg:\s*#fee2e2;/);
    expect(themeCss).toMatch(/--app-error-border:\s*rgba\(185,\s*28,\s*28,\s*0\.34\);/);
    expect(themeCss).toMatch(/--app-error-text:\s*#7f1d1d;/);
    expect(themeCss).toMatch(/--app-error-bg:\s*rgba\(127,\s*29,\s*29,\s*0\.36\);/);
    expect(themeCss).toMatch(/--app-error-border:\s*rgba\(248,\s*113,\s*113,\s*0\.62\);/);
    expect(themeCss).toMatch(/--app-error-text:\s*#fecaca;/);
    expect(themeCss).toMatch(/\.ui-alert--error\s*\{[^}]*border-color:\s*var\(--app-error-border\);[^}]*background:\s*var\(--app-error-bg\);[^}]*color:\s*var\(--app-error-text\);/s);
  });

  it("defines readable shared success and warning alert colors for light and dark themes", () => {
    expect(themeCss).toMatch(/--app-success-bg:\s*#eefbdd;/);
    expect(themeCss).toMatch(/--app-success-border:\s*rgba\(77,\s*124,\s*15,\s*0\.26\);/);
    expect(themeCss).toMatch(/--app-success-text:\s*#365314;/);
    expect(themeCss).toMatch(/--app-success-bg:\s*rgba\(54,\s*83,\s*20,\s*0\.42\);/);
    expect(themeCss).toMatch(/--app-success-border:\s*rgba\(163,\s*230,\s*53,\s*0\.38\);/);
    expect(themeCss).toMatch(/--app-success-text:\s*#f1f5c8;/);
    expect(themeCss).toMatch(/\.ui-alert--success\s*\{[^}]*border-color:\s*var\(--app-success-border\);[^}]*background:\s*var\(--app-success-bg\);[^}]*color:\s*var\(--app-success-text\);/s);

    expect(themeCss).toMatch(/--app-warning-bg:\s*#fef3c7;/);
    expect(themeCss).toMatch(/--app-warning-border:\s*rgba\(217,\s*119,\s*6,\s*0\.28\);/);
    expect(themeCss).toMatch(/--app-warning-text:\s*#92400e;/);
    expect(themeCss).toMatch(/--app-warning-bg:\s*rgba\(146,\s*64,\s*14,\s*0\.34\);/);
    expect(themeCss).toMatch(/--app-warning-border:\s*rgba\(251,\s*191,\s*36,\s*0\.42\);/);
    expect(themeCss).toMatch(/--app-warning-text:\s*#fde68a;/);
    expect(themeCss).toMatch(/\.ui-alert--warning\s*\{[^}]*border-color:\s*var\(--app-warning-border\);[^}]*background:\s*var\(--app-warning-bg\);[^}]*color:\s*var\(--app-warning-text\);/s);
  });

  it("uses shared error surfaces for non-alert inline error states", () => {
    expect(themeCss).toMatch(/\.generation-progress__error,\s*\.lesson-card__error\s*\{[^}]*border:\s*1px solid var\(--app-error-border\);[^}]*background:\s*var\(--app-error-bg\);[^}]*color:\s*var\(--app-error-text\);/s);
    expect(themeCss).toMatch(/\.lesson-voice-fab__error\s*\{[^}]*border:\s*1px solid var\(--app-error-border\);[^}]*background:\s*var\(--app-error-bg\);[^}]*color:\s*var\(--app-error-text\);/s);
  });

  it("keeps paid payment chart colors aligned with the success legend", () => {
    expect(themeCss).toMatch(/\.admin-payment-wave-chart__area--paid\s*\{[^}]*fill:\s*rgba\(158,\s*233,\s*57,\s*0\.24\);/s);
    expect(themeCss).toMatch(/\.admin-payment-wave-chart__line--paid\s*\{[^}]*stroke:\s*#9ee939;/s);
    expect(themeCss).toMatch(/\.admin-payment-legend__dot--success\s*\{[^}]*background:\s*#9ee939;/s);
  });

  it("uses the stronger homepage-style pulse for admin activity dots", () => {
    expect(themeCss).toMatch(/\.admin-activity-list__dot\s*\{[^}]*--admin-activity-pulse-core:\s*rgba\(177,\s*244,\s*73,\s*0\.72\);[^}]*--admin-activity-pulse-glow-strong:\s*rgba\(177,\s*244,\s*73,\s*0\.34\);/s);
    expect(themeCss).toMatch(/\.admin-activity-list__dot::after\s*\{[^}]*background:\s*var\(--admin-activity-pulse-core\);[^}]*animation:\s*admin-activity-dot-pulse 1\.85s ease-in-out infinite;/s);
    expect(themeCss).toMatch(/@keyframes admin-activity-dot-pulse\s*\{[\s\S]*0 0 0 7px var\(--admin-activity-pulse-ring-strong\)[\s\S]*0 0 18px var\(--admin-activity-pulse-glow-strong\);/s);
    expect(themeCss).toMatch(/\.admin-activity-list__dot--danger::after\s*\{[^}]*--admin-activity-pulse-core:\s*rgba\(248,\s*113,\s*113,\s*0\.72\);[^}]*--admin-activity-pulse-glow-strong:\s*rgba\(248,\s*113,\s*113,\s*0\.34\);/s);
    expect(themeCss).toMatch(/\.admin-activity-list__dot--pending::after\s*\{[^}]*--admin-activity-pulse-core:\s*rgba\(250,\s*204,\s*21,\s*0\.74\);[^}]*--admin-activity-pulse-glow-strong:\s*rgba\(250,\s*204,\s*21,\s*0\.34\);/s);
  });

  it("animates payment legend dots using their own status colors", () => {
    expect(themeCss).toMatch(/\.admin-payment-legend__dot--success::after\s*\{[^}]*background:\s*rgba\(177,\s*244,\s*73,\s*0\.72\);[^}]*animation:\s*admin-payment-legend-dot-pulse 1\.85s ease-in-out infinite;/s);
    expect(themeCss).toMatch(/\.admin-payment-legend__dot--pending::after\s*\{[^}]*background:\s*rgba\(250,\s*204,\s*21,\s*0\.74\);[^}]*animation:\s*admin-payment-legend-dot-pulse-warning 1\.85s ease-in-out infinite;/s);
    expect(themeCss).toMatch(/\.admin-payment-legend__dot--danger::after\s*\{[^}]*background:\s*rgba\(248,\s*113,\s*113,\s*0\.72\);[^}]*animation:\s*admin-payment-legend-dot-pulse-danger 1\.85s ease-in-out infinite;/s);
  });

  it("applies readable payment dashboard text colors in light theme", () => {
    expect(themeCss).toMatch(/\.page-shell\[data-theme="light"\]\s+\.admin-mini-stat__label,[^}]*\.admin-payment-legend__item small\s*\{[^}]*color:\s*rgba\(17,\s*24,\s*39,\s*0\.72\);/s);
    expect(themeCss).toMatch(/\.page-shell\[data-theme="light"\]\s+\.admin-payment-wave-chart__label\s*\{[^}]*fill:\s*rgba\(17,\s*24,\s*39,\s*0\.88\);[^}]*font-weight:\s*600;/s);
    expect(themeCss).toMatch(/\.page-shell\[data-theme="light"\]\s+\.admin-payment-overview__summary strong,[^}]*\.admin-payment-legend__topline span:last-child\s*\{[^}]*color:\s*#0f172a;/s);
    expect(themeCss).toMatch(/\.page-shell\[data-theme="light"\]\s+\.admin-payment-legend__item\s*\{[^}]*background:\s*rgba\(255,\s*255,\s*255,\s*0\.68\);[^}]*border-color:\s*rgba\(17,\s*24,\s*39,\s*0\.12\);/s);
  });
});
