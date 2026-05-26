import io
import json
import os
import textwrap
import urllib.request
import urllib.parse
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont, ImageOps


CANVAS_WIDTH = 1280
CANVAS_HEIGHT = 720
BG_COLOR = "#12121a"
PANEL_COLOR = "#1e1e2f"
TEXT_COLOR = "#ffffff"
ACCENT_COLOR = "#7b61ff"
BORDER_COLOR = "#3f3f5a"
SECONDARY_TEXT = "#b0b0c0"
BULLET_COLOR = "#00e5ff"


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
                "image_keyword": str(item.get("imageKeyword") or item.get("ImageKeyword") or "").strip(),
                "bullet_points": [str(point).strip() for point in (bullet_points or []) if str(point).strip()],
                "speaker_notes": str(item.get("speakerNotes") or item.get("SpeakerNotes") or "").strip(),
            }
        )

    return slides


def render_slide_png(output_path: Path, slide_number: int, title: str, bullet_points: list[str], image_keyword: str = "") -> None:
    image = Image.new("RGB", (CANVAS_WIDTH, CANVAS_HEIGHT), BG_COLOR)
    draw = ImageDraw.Draw(image)

    panel_margin = 56
    
    # Drop shadow
    shadow_offset = 12
    draw.rounded_rectangle(
        (panel_margin + shadow_offset, panel_margin + shadow_offset, CANVAS_WIDTH - panel_margin + shadow_offset, CANVAS_HEIGHT - panel_margin + shadow_offset),
        radius=32,
        fill="#08080c",
    )
    
    # Main Panel
    draw.rounded_rectangle(
        (panel_margin, panel_margin, CANVAS_WIDTH - panel_margin, CANVAS_HEIGHT - panel_margin),
        radius=32,
        fill=PANEL_COLOR,
        outline=BORDER_COLOR,
        width=2,
    )

    title_font = _load_font(42)
    body_font = _load_font(28)
    
    illustration = _fetch_image_for_slide(image_keyword)
    has_image = illustration is not None
    
    text_width_title = 20 if has_image else 34
    text_width_bullet = 30 if has_image else 54

    title_lines = textwrap.wrap(title or "Untitled slide", width=text_width_title)
    y = 120
    for line in title_lines[:3]:
        draw.text((96, y), line, fill=TEXT_COLOR, font=title_font)
        y += 52

    y += 24
    bullet_indent = 122
    for bullet in bullet_points[:8]:
        wrapped = textwrap.wrap(bullet, width=text_width_bullet) or [bullet]
        draw.ellipse((96, y + 14, 108, y + 26), fill=BULLET_COLOR)
        for line_index, line in enumerate(wrapped):
            draw.text((bullet_indent, y + line_index * 34), line, fill=SECONDARY_TEXT, font=body_font)
        y += max(42, len(wrapped) * 34 + 14)

    if has_image:
        target_w, target_h = 480, 520
        illustration = ImageOps.fit(illustration, (target_w, target_h), Image.Resampling.LANCZOS)
        img_w, img_h = target_w, target_h
        img_x = CANVAS_WIDTH - panel_margin - img_w - 40
        img_y = (CANVAS_HEIGHT - img_h) // 2
        mask = Image.new("L", (img_w, img_h), 0)
        ImageDraw.Draw(mask).rounded_rectangle((0, 0, img_w, img_h), radius=24, fill=255)
        image.paste(illustration, (img_x, img_y), mask)

    output_path.parent.mkdir(parents=True, exist_ok=True)
    image.save(output_path, format="PNG")

def _fetch_image_for_slide(keyword: str) -> Image.Image | None:
    if not keyword:
        return None
        
    import time
    
    # 0. Try Unsplash first if API key exists (highly requested, very good for keywords)
    unsplash_key = os.getenv("UNSPLASH_API_KEY")
    if unsplash_key:
        print(f"Trying Unsplash for {keyword}")
        try:
            url = f"https://api.unsplash.com/search/photos?query={urllib.parse.quote(keyword)}&per_page=1"
            req = urllib.request.Request(url, headers={'Authorization': f'Client-ID {unsplash_key}', 'User-Agent': 'Mozilla/5.0'})
            with urllib.request.urlopen(req, timeout=10) as response:
                data = json.loads(response.read())
                if data.get("results") and len(data["results"]) > 0:
                    img_url = data["results"][0]["urls"]["regular"]
                    req_img = urllib.request.Request(img_url, headers={'User-Agent': 'Mozilla/5.0'})
                    with urllib.request.urlopen(req_img, timeout=10) as img_resp:
                        img_data = img_resp.read()
                        return Image.open(io.BytesIO(img_data)).convert("RGBA")
        except Exception as e:
            print(f"Unsplash failed for {keyword}: {e}")

    # 1. Try Pexels first if API key exists
    pexels_key = os.getenv("PEXELS_API_KEY")
    if pexels_key:
        print(f"Trying Pexels for {keyword}")
        try:
            url = f"https://api.pexels.com/v1/search?query={urllib.parse.quote(keyword)}&per_page=1"
            req = urllib.request.Request(url, headers={'Authorization': pexels_key, 'User-Agent': 'Mozilla/5.0'})
            with urllib.request.urlopen(req, timeout=10) as response:
                data = json.loads(response.read())
                if data.get("photos") and len(data["photos"]) > 0:
                    img_url = data["photos"][0]["src"]["medium"]
                    req_img = urllib.request.Request(img_url, headers={'User-Agent': 'Mozilla/5.0'})
                    with urllib.request.urlopen(req_img, timeout=10) as img_resp:
                        img_data = img_resp.read()
                        return Image.open(io.BytesIO(img_data)).convert("RGBA")
        except Exception as e:
            print(f"Pexels failed for {keyword}: {e}")

    # 2. Try Pixabay if API key exists
    pixabay_key = os.getenv("PIXABAY_API_KEY")
    if pixabay_key:
        print(f"Trying Pixabay for {keyword}")
        try:
            url = f"https://pixabay.com/api/?key={pixabay_key}&q={urllib.parse.quote(keyword)}&image_type=photo&per_page=3"
            req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
            with urllib.request.urlopen(req, timeout=10) as response:
                data = json.loads(response.read())
                if data.get("hits") and len(data["hits"]) > 0:
                    img_url = data["hits"][0]["webformatURL"]
                    req_img = urllib.request.Request(img_url, headers={'User-Agent': 'Mozilla/5.0'})
                    with urllib.request.urlopen(req_img, timeout=10) as img_resp:
                        img_data = img_resp.read()
                        return Image.open(io.BytesIO(img_data)).convert("RGBA")
        except Exception as e:
            print(f"Pixabay failed for {keyword}: {e}")

    # 3. Try Pollinations AI as a free fallback
    try:
        url = f"https://image.pollinations.ai/prompt/{urllib.parse.quote(keyword)}_abstract_education_style_flat?width=480&height=520&nologo=true"
        req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
        with urllib.request.urlopen(req, timeout=10) as response:
            data = response.read()
            # Sleep briefly to avoid aggressive rate limiting from Pollinations AI
            time.sleep(1.5)
            return Image.open(io.BytesIO(data)).convert("RGBA")
    except urllib.error.HTTPError as e:
        print(f"Pollinations AI failed for {keyword} with status {e.code}: {e.reason}")
    except Exception as e:
        print(f"Pollinations AI failed for {keyword}: {e}")

    # Fallback to LoremFlickr if Pollinations AI rate limits us (402 Payment Required or others)
    print(f"Falling back to LoremFlickr for {keyword}")
    try:
        # Get first word of keyword for better matches on LoremFlickr
        simple_keyword = keyword.split()[0] if keyword else "education"
        fallback_url = f"https://loremflickr.com/480/520/{urllib.parse.quote(simple_keyword)},abstract/all"
        req = urllib.request.Request(fallback_url, headers={'User-Agent': 'Mozilla/5.0'})
        with urllib.request.urlopen(req, timeout=10) as response:
            data = response.read()
            return Image.open(io.BytesIO(data)).convert("RGBA")
    except Exception as e:
        print(f"Fallback also failed for {keyword}: {e}")
        
    # Final Fallback to a generated text image
    print(f"Final fallback to dummy text image for {keyword}")
    try:
        dummy_url = f"https://dummyimage.com/480x520/1e1e2f/7b61ff.png&text={urllib.parse.quote(keyword)}"
        req = urllib.request.Request(dummy_url, headers={'User-Agent': 'Mozilla/5.0'})
        with urllib.request.urlopen(req, timeout=10) as response:
            data = response.read()
            return Image.open(io.BytesIO(data)).convert("RGBA")
    except Exception as e:
        print(f"Dummy fallback failed for {keyword}: {e}")
        return None


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
