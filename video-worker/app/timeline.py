import json


def parse_audio_segments_json(value: str) -> list[dict]:
    parsed = json.loads(value or "[]")
    if not isinstance(parsed, list):
        raise ValueError("Audio segments phải là một mảng.")

    segments: list[dict] = []
    for index, item in enumerate(parsed, start=1):
        if not isinstance(item, dict):
            raise ValueError("Mỗi audio segment phải là object.")

        slide_number = int(item.get("slideNumber") or item.get("SlideNumber") or index)
        duration_seconds = float(item.get("durationSeconds") or item.get("DurationSeconds") or 0)
        if duration_seconds <= 0:
            raise ValueError("Audio segment phải có durationSeconds > 0.")

        segments.append(
            {
                "slide_number": slide_number,
                "title": str(item.get("title") or item.get("Title") or ""),
                "duration_seconds": duration_seconds,
            }
        )

    return segments


def build_slide_timeline(audio_segments: list[dict]) -> list[dict]:
    start_seconds = 0.0
    timeline: list[dict] = []

    for segment in audio_segments:
        duration_seconds = float(segment["duration_seconds"])
        item = {
            "slide_number": int(segment["slide_number"]),
            "title": str(segment.get("title") or ""),
            "start_seconds": start_seconds,
            "duration_seconds": duration_seconds,
            "end_seconds": start_seconds + duration_seconds,
        }
        timeline.append(item)
        start_seconds = item["end_seconds"]

    return timeline
