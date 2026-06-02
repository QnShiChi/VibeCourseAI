import asyncio
from pathlib import Path
from fastapi import FastAPI, HTTPException

# --- Sentiment Analysis Setup ---
import torch
from transformers import AutoTokenizer, AutoModelForSequenceClassification
from pydantic import BaseModel
from app.sentiment_config import normalize_label, resolve_model_reference

base_dir = Path(__file__).resolve().parent
model_reference = resolve_model_reference(base_dir=base_dir)
max_length = 256
label2text = {
    0: "negative",
    1: "normal",
    2: "positive",
}

try:
    tokenizer_reference = model_reference if not Path(model_reference).is_dir() else "vinai/phobert-base"
    tokenizer = AutoTokenizer.from_pretrained(tokenizer_reference)
    sentiment_model = AutoModelForSequenceClassification.from_pretrained(model_reference)

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    sentiment_model.to(device)
    sentiment_model.eval()

    model_labels = getattr(sentiment_model.config, "id2label", {}) or {}
    if model_labels:
        label2text = {
            int(label_id): normalize_label(label)
            for label_id, label in model_labels.items()
        }
    print("PhoBERT Sentiment Model loaded successfully on", device)
except Exception as e:
    print(f"Error loading sentiment model: {e}")
    sentiment_model = None
    tokenizer = None

class SentimentRequest(BaseModel):
    text: str

class SentimentResponse(BaseModel):
    text: str
    pred_label_id: int
    pred_label: str
    probabilities: dict
# --------------------------------


app = FastAPI(title="Course AI Worker")


@app.get("/health")
def health():
    return {"status": "ok"}


@app.get("/jobs/ping")
def ping():
    return {"message": "worker ready"}


@app.post("/jobs/analyze-sentiment", response_model=SentimentResponse)
async def analyze_sentiment(request: SentimentRequest):
    if sentiment_model is None or tokenizer is None:
        raise HTTPException(status_code=500, detail="Sentiment model is not loaded.")
        
    text = request.text
    if not text.strip():
        raise HTTPException(status_code=400, detail="Text cannot be empty.")
        
    inputs = tokenizer(
        text,
        return_tensors="pt",
        truncation=True,
        padding=True,
        max_length=max_length,
    )
    
    inputs = {k: v.to(device) for k, v in inputs.items()}
    
    with torch.no_grad():
        outputs = sentiment_model(**inputs)
        probs = torch.softmax(outputs.logits, dim=-1)
        pred_id = torch.argmax(probs, dim=-1).item()
        
    probabilities = {
        label2text[i]: float(probs[0][i]) for i in range(len(label2text))
    }
    
    return SentimentResponse(
        text=text,
        pred_label_id=pred_id,
        pred_label=label2text[pred_id],
        probabilities=probabilities
    )
