import unittest

from app.timeline import build_slide_timeline, parse_audio_segments_json


class TimelineTests(unittest.TestCase):
    def test_parse_audio_segments_json_supports_pascal_case(self) -> None:
        parsed = parse_audio_segments_json(
            '[{"SlideNumber":1,"Title":"Intro","DurationSeconds":3.2},{"SlideNumber":2,"Title":"Wrap","DurationSeconds":5.0}]'
        )

        self.assertEqual(parsed[0]["slide_number"], 1)
        self.assertEqual(parsed[1]["duration_seconds"], 5.0)

    def test_build_slide_timeline_uses_audio_segment_durations(self) -> None:
        timeline = build_slide_timeline(
            [
                {"slide_number": 1, "title": "Intro", "duration_seconds": 3.2},
                {"slide_number": 2, "title": "Wrap", "duration_seconds": 5.0},
            ]
        )

        self.assertEqual(timeline[0]["start_seconds"], 0.0)
        self.assertEqual(timeline[0]["duration_seconds"], 3.2)
        self.assertEqual(timeline[1]["start_seconds"], 3.2)
        self.assertEqual(timeline[1]["end_seconds"], 8.2)


if __name__ == "__main__":
    unittest.main()
