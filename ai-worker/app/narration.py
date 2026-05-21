import json

from app.models import NarrationSegment


def build_narration_segments(teaching_script: str, slide_outline_json: str, voiceover_plan_json: str) -> list[NarrationSegment]:
    slide_outline = json.loads(slide_outline_json)
    if not isinstance(slide_outline, list) or not slide_outline:
        raise ValueError("Slide outline phải là một mảng slide không rỗng.")

    segments: list[NarrationSegment] = []
    fallback_script = teaching_script.strip()

    for index, slide in enumerate(slide_outline, start=1):
        notes = str(slide.get("speakerNotes") or slide.get("SpeakerNotes") or "").strip()
        bullets = slide.get("bulletPoints") or slide.get("BulletPoints") or []
        title = str(slide.get("title") or slide.get("Title") or f"Slide {index}")
        bullet_text = "; ".join(str(item).strip() for item in bullets if str(item).strip())

        narration_text = notes
        if len(narration_text) < 30:
            parts = [f"Ở slide này: {title}."]
            if bullet_text:
                parts.append(bullet_text)
            if notes:
                parts.append(notes)
            elif fallback_script:
                parts.append(fallback_script)
            narration_text = " ".join(part for part in parts if part).strip()

        if not narration_text:
            raise ValueError(f"Missing narration text for slide {index}.")

        segments.append(
            NarrationSegment(
                slide_number=int(slide.get("slideNumber") or slide.get("SlideNumber") or index),
                title=title,
                narration_text=narration_text,
            )
        )

    return segments
