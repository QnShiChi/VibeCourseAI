import { readFileSync } from "node:fs";
import { describe, expect, it } from "vitest";

const themeCss = readFileSync("src/styles/theme.css", "utf8");

describe("theme accent surfaces", () => {
  it("uses accent ink for badges and active grouped navigation triggers", () => {
    expect(themeCss).toMatch(/\.app-nav__group-trigger--active\s*\{[^}]*background:\s*var\(--color-accent-green\);[^}]*color:\s*var\(--color-accent-ink\);/s);
    expect(themeCss).toMatch(/\.ui-badge\s*\{[^}]*background:\s*var\(--color-accent-green\);[^}]*color:\s*var\(--color-accent-ink\);/s);
  });
});
