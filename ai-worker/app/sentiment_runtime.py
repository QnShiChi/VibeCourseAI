from __future__ import annotations

import threading
from pathlib import Path
from typing import Any

from app.sentiment_config import normalize_label, resolve_model_reference


class SentimentModelNotReadyError(RuntimeError):
    pass


class SentimentRuntime:
    def __init__(self, base_dir: Path) -> None:
        self._base_dir = base_dir
        self._lock = threading.Lock()
        self._load_thread: threading.Thread | None = None
        self._state = "not_started"
        self._error: str | None = None
        self._tokenizer: Any | None = None
        self._model: Any | None = None
        self._device: Any | None = None
        self._label2text = {
            0: "negative",
            1: "normal",
            2: "positive",
        }

    @property
    def state(self) -> str:
        return self._state

    def start_background_load(self) -> None:
        with self._lock:
            if self._state in {"loading", "ready"}:
                return

            self._state = "loading"
            self._error = None
            self._load_thread = threading.Thread(target=self._load_model, daemon=True)
            self._load_thread.start()

    def health_payload(self) -> dict[str, Any]:
        payload: dict[str, Any] = {
            "status": "ok",
            "sentiment_model": self._state,
        }
        if self._error:
            payload["sentiment_model_error"] = self._error

        return payload

    def analyze(self, text: str) -> dict[str, Any]:
        if self._state == "not_started":
            self.start_background_load()
            raise SentimentModelNotReadyError("Sentiment model is still loading.")

        if self._state == "loading":
            raise SentimentModelNotReadyError("Sentiment model is still loading.")

        if self._state != "ready" or self._tokenizer is None or self._model is None or self._device is None:
            detail = self._error or "Sentiment model is unavailable."
            raise SentimentModelNotReadyError(detail)

        if not text.strip():
            raise ValueError("Text cannot be empty.")

        inputs = self._tokenizer(
            text,
            return_tensors="pt",
            truncation=True,
            padding=True,
            max_length=256,
        )

        inputs = {key: value.to(self._device) for key, value in inputs.items()}

        import torch

        with torch.no_grad():
            outputs = self._model(**inputs)
            probabilities = torch.softmax(outputs.logits, dim=-1)
            predicted_label_id = torch.argmax(probabilities, dim=-1).item()

        return {
            "text": text,
            "pred_label_id": predicted_label_id,
            "pred_label": self._label2text[predicted_label_id],
            "probabilities": {
                self._label2text[index]: float(probabilities[0][index])
                for index in range(len(self._label2text))
            },
        }

    def _load_model(self) -> None:
        try:
            import torch
            from transformers import AutoModelForSequenceClassification, AutoTokenizer

            model_reference = resolve_model_reference(base_dir=self._base_dir)
            tokenizer_reference = model_reference if not Path(model_reference).is_dir() else "vinai/phobert-base"

            tokenizer = AutoTokenizer.from_pretrained(tokenizer_reference)
            model = AutoModelForSequenceClassification.from_pretrained(model_reference)

            device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
            model.to(device)
            model.eval()

            model_labels = getattr(model.config, "id2label", {}) or {}
            label2text = self._label2text
            if model_labels:
                label2text = {
                    int(label_id): normalize_label(label)
                    for label_id, label in model_labels.items()
                }

            with self._lock:
                self._tokenizer = tokenizer
                self._model = model
                self._device = device
                self._label2text = label2text
                self._state = "ready"
                self._error = None
        except Exception as ex:
            with self._lock:
                self._state = "failed"
                self._error = f"Sentiment model failed to load: {ex}"
