import { readFileSync } from "node:fs";
import { describe, expect, it } from "vitest";

const authCss = readFileSync("src/styles/AuthPage.module.css", "utf8");

describe("AuthPage autofill styling", () => {
  it("keeps autofilled auth inputs on theme colors instead of browser white fill", () => {
    expect(authCss).toMatch(/\.inputControl:-webkit-autofill/s);
    expect(authCss).toMatch(/-webkit-text-fill-color:\s*var\(--auth-strong\);/);
    expect(authCss).toMatch(/box-shadow:\s*0 0 0 1000px var\(--auth-input-bg\) inset;/);
  });

  it("forces auth error alerts to use readable error contrast instead of inheriting dark theme text", () => {
    expect(authCss).toMatch(/\.authAlert\s*\{/);
    expect(authCss).toMatch(/--auth-error-bg:\s*#fff0f0;/);
    expect(authCss).toMatch(/--auth-error-bg:\s*rgba\(248,\s*113,\s*113,\s*0\.14\);/);
    expect(authCss).toMatch(/--auth-error-text:\s*#fecaca;/);
    expect(authCss).toMatch(/background:\s*var\(--auth-error-bg\);/);
    expect(authCss).toMatch(/\.authShell\[data-theme="dark"\]\s+:global\(\.ui-alert--error\)\s*\{/);
    expect(authCss).toMatch(/\.authErrorAlertText\s*\{/);
  });
});
