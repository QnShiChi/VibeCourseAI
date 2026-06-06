from pathlib import Path
from fastapi import FastAPI, HTTPException

from pydantic import BaseModel
from app.sentiment_runtime import SentimentModelNotReadyError, SentimentRuntime

class SentimentRequest(BaseModel):
    text: str

class SentimentResponse(BaseModel):
    text: str
    pred_label_id: int
    pred_label: str
    probabilities: dict
# --------------------------------


def create_app(runtime: SentimentRuntime | None = None) -> FastAPI:
    sentiment_runtime = runtime or SentimentRuntime(Path(__file__).resolve().parent)
    app = FastAPI(title="Course AI Worker")

    @app.on_event("startup")
    def startup() -> None:
        sentiment_runtime.start_background_load()

    @app.get("/health")
    def health():
        return sentiment_runtime.health_payload()

    @app.get("/jobs/ping")
    def ping():
        return {"message": "worker ready", "sentiment_model": sentiment_runtime.state}

    @app.post("/jobs/analyze-sentiment", response_model=SentimentResponse)
    async def analyze_sentiment(request: SentimentRequest):
        try:
            result = sentiment_runtime.analyze(request.text)
        except ValueError as ex:
            raise HTTPException(status_code=400, detail=str(ex)) from ex
        except SentimentModelNotReadyError as ex:
            raise HTTPException(status_code=503, detail=str(ex)) from ex

        return SentimentResponse(**result)

    return app


app = create_app()
