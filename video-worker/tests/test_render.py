import tempfile
import unittest
from pathlib import Path

from app.render import parse_slide_outline_json, render_slide_png
from app.storage import resolve_storage_path_from_url


class RenderTests(unittest.TestCase):
    def test_parse_slide_outline_json_supports_pascal_case(self) -> None:
        slides = parse_slide_outline_json(
            '[{"SlideNumber":1,"Title":"Intro","BulletPoints":["A","B"],"SpeakerNotes":"Notes"}]'
        )

        self.assertEqual(slides[0]["slide_number"], 1)
        self.assertEqual(slides[0]["title"], "Intro")
        self.assertEqual(slides[0]["bullet_points"], ["A", "B"])

    def test_render_slide_png_creates_output_file(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            output = Path(temp_dir) / "slide-1.png"
            render_slide_png(output, slide_number=1, title="Intro", bullet_points=["A", "B"])
            self.assertTrue(output.exists())
            self.assertGreater(output.stat().st_size, 0)

    def test_resolve_storage_path_from_url_maps_to_local_storage(self) -> None:
        resolved = resolve_storage_path_from_url("/storage/audio/lesson-1.wav")
        self.assertEqual(resolved.as_posix(), "/app/storage/audio/lesson-1.wav")


if __name__ == "__main__":
    unittest.main()
