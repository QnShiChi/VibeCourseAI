from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import torch
from transformers import AutoTokenizer, AutoModelForSequenceClassification
import uvicorn

app = FastAPI(title="PhoBERT Sentiment Analysis API")

model_dir = "./phobert_dataset2_model/checkpoint-5200"
tokenizer_name = "vinai/phobert-base"
max_length = 256

label2text = {
    0: "negative",
    1: "normal",
    2: "positive",
}

try:
    tokenizer = AutoTokenizer.from_pretrained(tokenizer_name)
    model = AutoModelForSequenceClassification.from_pretrained(model_dir)
    
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    model.to(device)
    model.eval()
except Exception as e:
    print(f"Error loading model: {e}")
    model = None
    tokenizer = None

class SentimentRequest(BaseModel):
    text: str

@app.post("/predict")
def predict_sentiment(request: SentimentRequest):
    if model is None or tokenizer is None:
        raise HTTPException(status_code=500, detail="Model is not loaded.")
        
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
        outputs = model(**inputs)
        probs = torch.softmax(outputs.logits, dim=-1)
        pred_id = torch.argmax(probs, dim=-1).item()
        
    probabilities = {
        label2text[i]: float(probs[0][i]) for i in range(len(label2text))
    }
    
    return {
        "text": text,
        "pred_label_id": pred_id,
        "pred_label": label2text[pred_id],
        "probabilities": probabilities
    }

if __name__ == "__main__":
    uvicorn.run("app:app", host="0.0.0.0", port=8080, reload=True)
