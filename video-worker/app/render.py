import json
import textwrap
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


CANVAS_WIDTH = 1280
CANVAS_HEIGHT = 720
BG_COLOR = "#f8f2d8"
PANEL_COLOR = "#fffdf5"
TEXT_COLOR = "#101010"
ACCENT_COLOR = "#b7f14a"
BORDER_COLOR = "#171717"


def parse_slide_outline_json(value: str) -> list[dict]:
    parsed = json.loads(value or "[]")
    if not isinstance(parsed, list):
        raise ValueError("Slide outline phải là một mảng.")

    slides: list[dict] = []
    for index, item in enumerate(parsed, start=1):
        if not isinstance(item, dict):
            raise ValueError("Mỗi slide phải là object.")

        bullet_points = item.get("bulletPoints")
        if bullet_points is None:
            bullet_points = item.get("BulletPoints")

        slides.append(
            {
                "slide_number": int(item.get("slideNumber") or item.get("SlideNumber") or index),
                "title": str(item.get("title") or item.get("Title") or "").strip(),
                "bullet_points": [str(point).strip() for point in (bullet_points or []) if str(point).strip()],
                "speaker_notes": str(item.get("speakerNotes") or item.get("SpeakerNotes") or "").strip(),
            }
        )

    return slides


def render_slide_png(output_path: Path, slide_number: int, title: str, bullet_points: list[str]) -> None:
    image = Image.new("RGB", (CANVAS_WIDTH, CANVAS_HEIGHT), BG_COLOR)
    draw = ImageDraw.Draw(image)

    panel_margin = 56
    draw.rounded_rectangle(
        (panel_margin, panel_margin, CANVAS_WIDTH - panel_margin, CANVAS_HEIGHT - panel_margin),
        radius=28,
        fill=PANEL_COLOR,
        outline=BORDER_COLOR,
        width=3,
    )

    title_font = _load_font(42)
    body_font = _load_font(28)
    badge_font = _load_font(24)

    badge_bounds = (96, 88, 250, 138)
    draw.rounded_rectangle(badge_bounds, radius=22, fill=ACCENT_COLOR, outline=BORDER_COLOR, width=2)
    draw.text((badge_bounds[0] + 22, badge_bounds[1] + 12), f"Slide {slide_number}", fill=TEXT_COLOR, font=badge_font)

    title_lines = textwrap.wrap(title or "Untitled slide", width=34)
    y = 176
    for line in title_lines[:3]:
        draw.text((96, y), line, fill=TEXT_COLOR, font=title_font)
        y += 52

    y += 24
    bullet_indent = 122
    for bullet in bullet_points[:8]:
        wrapped = textwrap.wrap(bullet, width=52) or [bullet]
        draw.ellipse((96, y + 12, 110, y + 26), fill=TEXT_COLOR)
        for line_index, line in enumerate(wrapped):
            draw.text((bullet_indent, y + line_index * 34), line, fill=TEXT_COLOR, font=body_font)
        y += max(42, len(wrapped) * 34 + 14)

    output_path.parent.mkdir(parents=True, exist_ok=True)
    image.save(output_path, format="PNG")


def _load_font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    font_candidates = [
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
    ]

    for candidate in font_candidates:
        path = Path(candidate)
        if path.exists():
            return ImageFont.truetype(str(path), size=size)

    return ImageFont.load_default()
