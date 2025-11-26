# ETL Service

This folder hosts a small FastAPI application that receives raw submissions from the React frontend, cleans and validates them, and forwards the sanitized payloads to the ASP.NET Core backend.

## Quick start

```bash
cd /Users/abbe/Documents/Documents - MacBook Pro (2)/school/Fall 2025/Data Warehousing/fitwarehouse/etl
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
cp env.example .env  # edit values as needed
uvicorn app:app --reload --port 8000
```

### Environment variables

| Variable | Description | Default |
| --- | --- | --- |
| `BACKEND_BASE_URL` | Base URL of the ASP.NET Core API. | `http://localhost:5018` |
| `FRONTEND_ORIGINS` | Comma-separated list of origins allowed to call the ETL service. | `http://localhost:5173` |

## API

- `POST /ingest` — accepts raw submission payloads, runs `clean_submission`, forwards to `/api/inventory` on the backend, and returns both the cleaned payload and backend response.
- `GET /health` — lightweight health check endpoint.

Update `clean_submission` if additional business rules are needed before persisting data. Keep it side-effect free and covered by tests if the logic grows more complex.

