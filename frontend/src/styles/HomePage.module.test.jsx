import { readFileSync } from "node:fs";
import { describe, expect, it } from "vitest";

const homeCss = readFileSync("src/styles/HomePage.module.css", "utf8");

describe("HomePage module styles", () => {
  it("keeps the theme toggle on the original single neon dot treatment", () => {
    expect(homeCss).toMatch(/\.themeToggleDot\s*\{[^}]*width:\s*12px;[^}]*height:\s*12px;[^}]*animation:\s*neonDotPulse 1\.5s ease-in-out infinite -0\.35s;/s);
  });

  it("keeps active carousel dots circular while only animating on slide changes", () => {
    expect(homeCss).toMatch(/\.carouselDotActive\s*\{[^}]*background:\s*var\(--glass-bg\);/s);
    expect(homeCss).not.toMatch(/\.carouselDotActive\s*\{[^}]*width:\s*42px;/s);
    expect(homeCss).toMatch(/\.carouselDotActive::before\s*\{[^}]*animation:\s*none;/s);
    expect(homeCss).toMatch(/\.carouselDotActive\[data-transfer-state="arriving"\]::before\s*\{[^}]*animation:\s*carouselDotArrive 820ms cubic-bezier\(0\.24,\s*0\.84,\s*0\.2,\s*1\) 1 both;/s);
    expect(homeCss).toMatch(/@keyframes carouselDotArrive/s);
    expect(homeCss).not.toMatch(/@keyframes carouselDotSwallow/s);
  });
});
