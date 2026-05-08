from fastapi import FastAPI

app = FastAPI(title="Course Video AI Worker")


@app.get("/health")
def health():
    return {"status": "ok"}


@app.get("/jobs/ping")
def ping():
    return {"message": "worker ready"}
