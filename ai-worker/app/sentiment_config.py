import os
from pathlib import Path

DEFAULT_FALLBACK_MODEL_ID = "wonrax/phobert-base-vietnamese-sentiment"
DEFAULT_LOCAL_MODEL_DIR = "phobert_dataset2_model/checkpoint-5200"

_LABEL_ALIASES = {
    "NEG": "negative",
    "NEGATIVE": "negative",
    "POS": "positive",
    "POSITIVE": "positive",
    "NEU": "normal",
    "NEUTRAL": "normal",
    "NORMAL": "normal",
}


def resolve_model_reference(
    base_dir: Path,
    fallback_model_id: str = DEFAULT_FALLBACK_MODEL_ID,
    local_model_dir: str = DEFAULT_LOCAL_MODEL_DIR,
) -> str:
    local_checkpoint = base_dir / local_model_dir
    if (local_checkpoint / "config.json").exists():
        return str(local_checkpoint)

    return os.getenv("SENTIMENT_MODEL_ID", fallback_model_id)


def normalize_label(label: str) -> str:
    normalized = label.strip().upper()
    return _LABEL_ALIASES.get(normalized, label.strip().lower())
