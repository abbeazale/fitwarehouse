# Quick Reference Card

## 🚀 Start All Services

```bash
# Terminal 1 - Backend (C#)
cd backend
dotnet run --launch-profile http

# Terminal 2 - ETL (Python)
cd etl
source .venv/bin/activate
uvicorn app:app --reload --port 8000

# Terminal 3 - Frontend (React)
cd frontend
yarn dev
```

## 🔗 Service URLs

- **Frontend**: http://localhost:5173
- **ETL Service**: http://localhost:8000
- **Backend API**: http://localhost:5018
- **Supabase Dashboard**: https://supabase.com/dashboard/project/ikbpmyqftmlgyvfysucs

## 📊 Data Flow

```
CSV/XML File → Frontend → ETL → Backend API → Supabase PostgreSQL
```

## 🔑 Important Files

### Configuration
- `frontend/.env` - Supabase credentials
- `backend/appsettings.json` - Database connection string
- `etl/.env` - Backend URL and CORS settings

### Sample Data
- `sample_data/inventory_sample.csv` - CSV test file
- `sample_data/inventory_sample.xml` - XML test file

## 📝 API Endpoints

### ETL Service (Port 8000)
- `POST /upload` - Upload CSV/XML file
- `POST /ingest` - Single record submission
- `GET /health` - Health check

### Backend API (Port 5018)
- `GET /api/inventory` - Get all inventory
- `POST /api/inventory` - Create inventory record
- `GET /api/health` - Health check
- `GET /weatherforecast` - Demo endpoint

## 🔧 Common Commands

### Backend
```bash
# Restore packages
dotnet restore

# Build
dotnet build

# Run
dotnet run --launch-profile http
```

### ETL
```bash
# Install dependencies
pip install -r requirements.txt

# Run
uvicorn app:app --reload --port 8000
```

### Frontend
```bash
# Install dependencies
yarn install

# Run dev server
yarn dev

# Build for production
yarn build
```

## 🐛 Quick Troubleshooting

**Backend won't start?**
→ Check database password in `appsettings.json`

**ETL errors?**
→ Make sure backend is running first

**File upload fails?**
→ Check ETL service is running on port 8000

**No data showing?**
→ Check browser console, verify Supabase connection

## 📚 Documentation

- **Full Setup**: [FULL_PIPELINE_SETUP.md](./FULL_PIPELINE_SETUP.md)
- **Supabase Details**: [SUPABASE_SETUP.md](./SUPABASE_SETUP.md)
- **Quick Start**: [QUICKSTART.md](./QUICKSTART.md)
- **Main README**: [README.md](./README.md)

## 🎯 Testing Checklist

- [ ] All 3 services running
- [ ] Can log in to frontend
- [ ] Can upload CSV file
- [ ] Can upload XML file
- [ ] Can manually enter data
- [ ] Data appears in table
- [ ] Data persists after refresh

## 🔐 Database Connection

**Host**: db.ikbpmyqftmlgyvfysucs.supabase.co  
**Port**: 5432  
**Database**: postgres  
**Username**: postgres  
**Password**: [Get from Supabase Dashboard]

## 📦 Tech Stack

- **Frontend**: React 19 + Vite + TypeScript + Tailwind CSS
- **ETL**: Python 3.10+ + FastAPI + httpx
- **Backend**: .NET 9 + ASP.NET Core + Entity Framework Core
- **Database**: PostgreSQL 17 (Supabase)
- **Auth**: Supabase Auth

