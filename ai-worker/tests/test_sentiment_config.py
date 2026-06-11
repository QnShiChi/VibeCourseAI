import tempfile
import unittest
from pathlib import Path

from app.sentiment_config import (
    DEFAULT_FALLBACK_MODEL_ID,
    normalize_label,
    resolve_model_reference,
)


class SentimentConfigTests(unittest.TestCase):
    def test_resolve_model_reference_prefers_local_checkpoint_when_present(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            checkpoint_dir = Path(temp_dir) / "phobert_dataset2_model" / "checkpoint-5200"
            checkpoint_dir.mkdir(parents=True)
            (checkpoint_dir / "config.json").write_text("{}", encoding="utf-8")

            model_reference = resolve_model_reference(
                base_dir=Path(temp_dir),
                fallback_model_id="wonrax/phobert-base-vietnamese-sentiment",
            )

        self.assertEqual(model_reference, str(checkpoint_dir))

    def test_resolve_model_reference_falls_back_to_default_model_id_when_checkpoint_missing(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            model_reference = resolve_model_reference(base_dir=Path(temp_dir))

        self.assertEqual(model_reference, DEFAULT_FALLBACK_MODEL_ID)

    def test_normalize_label_maps_common_hugging_face_labels(self) -> None:
        self.assertEqual(normalize_label("NEG"), "negative")
        self.assertEqual(normalize_label("POS"), "positive")
        self.assertEqual(normalize_label("NEU"), "normal")


if __name__ == "__main__":
    unittest.main()
