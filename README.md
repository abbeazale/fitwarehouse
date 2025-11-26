# FitWarehouse Monorepo

End-to-end data warehousing project with a Vite + React frontend, Supabase PostgreSQL database, authentication, and optional FastAPI ETL microservice. Data flows **Frontend → Supabase PostgreSQL** with authentication and real-time updates.

**🎉 Now integrated with Supabase!** See [SUPABASE_SETUP.md](./SUPABASE_SETUP.md) for details.

```
fitwarehouse/
├── backend/   # ASP.NET Core Web API (inventory endpoints + weather demo)
├── etl/       # Python FastAPI service that cleans/forwards submissions
└── frontend/  # Vite + React client with a simple inventory form + dashboards
```

## Prerequisites

- .NET SDK 9.x
- Node.js ≥ 18 (bundled with Yarn 1.x in this repo)
- Python ≥ 3.10

## Environment configuration

| Component | Sample file | What to set |
| --- | --- | --- |
| Backend | `backend/appsettings.json` | `Cors:AllowedOrigins` (React dev server, ETL service if needed) |
| ETL | `etl/env.example` → `.env` | `BACKEND_BASE_URL` and `FRONTEND_ORIGINS` |
| Frontend | `frontend/env.example` → `.env` | `VITE_BACKEND_BASE_URL` and `VITE_ETL_BASE_URL` |

Copy each example file to `.env` (or edit the JSON) and adjust ports/hosts when deploying beyond localhost.

## Local development workflow

Open three terminals and run the services in the order below:

1. **Backend**
   ```bash
   cd '/Users/abbe/Documents/Documents - MacBook Pro (2)/school/Fall 2025/Data Warehousing/fitwarehouse/backend'
   dotnet run --launch-profile http
   ```
   - Provides `/api/health`, `/api/inventory`, and `/weatherforecast`.
   - CORS is driven by `Cors:AllowedOrigins`.

2. **ETL**
   ```bash
   cd '/Users/abbe/Documents/Documents - MacBook Pro (2)/school/Fall 2025/Data Warehousing/fitwarehouse/etl'
   python3 -m venv .venv
   source .venv/bin/activate
   pip install -r requirements.txt
   cp env.example .env  # edit values once
   uvicorn app:app --reload --port 8000
   ```
   - Accepts `POST /ingest` payloads from the frontend, normalizes the data, forwards it to `/api/inventory`, and returns both the cleaned payload and backend response.

3. **Frontend**
   ```bash
   cd '/Users/abbe/Documents/Documents - MacBook Pro (2)/school/Fall 2025/Data Warehousing/fitwarehouse/frontend'
   yarn install  # first run
   cp env.example .env  # ensure base URLs match your services
   yarn dev --host
   ```
   - Provides a form that posts to the ETL service, displays the current inventory table, and pings the weather + health endpoints directly from the backend.

## Testing the pipeline quickly

After all services are running:

- Navigate to `http://localhost:5173`, submit a record, and verify it appears instantly in the "Most Recent Inventory" table.
- Use curl to hit the backend directly (bypassing ETL) when debugging:
  ```bash
  curl -X POST http://localhost:5018/api/inventory \
    -H "Content-Type: application/json" \
    -d '{"productName":"Dumbbells","quantity":10,"warehouseLocation":"Zone B","submittedBy":"System","processedAtUtc":"2024-11-22T18:00:00Z"}'
  ```
- Confirm the ETL service is reachable:
  ```bash
  curl -X POST http://localhost:8000/ingest \
    -H "Content-Type: application/json" \
    -d '{"productName":" jump rope ","quantity":"40 units","warehouseLocation":" north bay ","submittedBy":"alex"}'
  ```

## Next steps

- ✅ **Database**: Now using Supabase PostgreSQL! See [SUPABASE_SETUP.md](./SUPABASE_SETUP.md)
- ✅ **Authentication**: Supabase Auth integrated with email/password login
- **Optional**: Connect the ASP.NET backend to Supabase PostgreSQL (see setup guide)
- **Optional**: Re-integrate ETL service to clean data before inserting into Supabase
- Expand data models with fact/dimension tables for data warehousing
- Create analytical queries and reports
- Secure inter-service traffic (auth tokens, TLS) once you move beyond local development
- Containerize each service or wire them together with Docker Compose when you are ready for consistent deployments

