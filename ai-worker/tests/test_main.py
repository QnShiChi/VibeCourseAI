import unittest
from pathlib import Path
import sys
import types

from fastapi.testclient import TestClient

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

fake_torch = types.SimpleNamespace(
    cuda=types.SimpleNamespace(is_available=lambda: False),
    device=lambda name: name,
    no_grad=lambda: types.SimpleNamespace(__enter__=lambda self: None, __exit__=lambda self, exc_type, exc, tb: False),
    softmax=lambda logits, dim=None: logits,
    argmax=lambda probs, dim=None: types.SimpleNamespace(item=lambda: 1),
)

class _FakeTokenizer:
    @staticmethod
    def from_pretrained(*args, **kwargs):
        class _Tokenizer:
            def __call__(self, *args, **kwargs):
                return {}
        return _Tokenizer()

class _FakeModel:
    config = types.SimpleNamespace(id2label={1: "NORMAL"})

    @staticmethod
    def from_pretrained(*args, **kwargs):
        class _Model:
            config = types.SimpleNamespace(id2label={1: "NORMAL"})
            def to(self, device):
                return self
            def eval(self):
                return self
            def __call__(self, **kwargs):
                return types.SimpleNamespace(logits=[[0.1, 0.8, 0.1]])
        return _Model()

sys.modules.setdefault("torch", fake_torch)
sys.modules.setdefault(
    "transformers",
    types.SimpleNamespace(
        AutoTokenizer=_FakeTokenizer,
        AutoModelForSequenceClassification=_FakeModel,
    ),
)

from app.main import create_app
from app.sentiment_runtime import SentimentModelNotReadyError


class FakeSentimentRuntime:
    def __init__(self, state: str = "loading") -> None:
        self.state = state
        self.start_called = False

    def start_background_load(self) -> None:
        self.start_called = True

    def health_payload(self) -> dict:
        return {"status": "ok", "sentiment_model": self.state}

    def analyze(self, text: str) -> dict:
        if self.state != "ready":
            raise SentimentModelNotReadyError("Sentiment model is still loading.")

        return {
            "text": text,
            "pred_label_id": 1,
            "pred_label": "normal",
            "probabilities": {
                "negative": 0.1,
                "normal": 0.8,
                "positive": 0.1,
            },
        }


class MainAppTests(unittest.TestCase):
    def test_health_endpoint_responds_while_model_is_loading(self) -> None:
        runtime = FakeSentimentRuntime(state="loading")
        app = create_app(runtime)

        with TestClient(app) as client:
            response = client.get("/health")

        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["status"], "ok")
        self.assertEqual(response.json()["sentiment_model"], "loading")
        self.assertTrue(runtime.start_called)

    def test_analyze_sentiment_returns_503_while_model_is_loading(self) -> None:
        runtime = FakeSentimentRuntime(state="loading")
        app = create_app(runtime)

        with TestClient(app) as client:
            response = client.post("/jobs/analyze-sentiment", json={"text": "Xin chao"})

        self.assertEqual(response.status_code, 503)
        self.assertIn("loading", response.json()["detail"].lower())


if __name__ == "__main__":
    unittest.main()
