# 🚀 Quick Start Guide

## Get Started in 3 Steps

### 1. Create Environment File

Create `frontend/.env` with your Supabase credentials:

```bash
cd frontend
cat > .env << 'EOF'
VITE_BACKEND_BASE_URL=http://localhost:5018
VITE_ETL_BASE_URL=http://localhost:8000
VITE_SUPABASE_URL=https://ikbpmyqftmlgyvfysucs.supabase.co
VITE_SUPABASE_ANON_KEY=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImlrYnBteXFmdG1sZ3l2ZnlzdWNzIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjM4NzQyNjEsImV4cCI6MjA3OTQ1MDI2MX0.jvZwCyyd5UhzLyLnVAWB2tr7JG5cDAtTKnGch-14xFM
EOF
```

### 2. Start the Frontend

```bash
yarn dev
```

### 3. Open and Test

1. Open http://localhost:5173
2. Click "Login" → "Sign Up" to create an account
3. Check your email to confirm (Supabase sends confirmation)
4. Log in and start adding inventory!

## 🎯 What You Get

- ✅ Full authentication system (login/signup/logout)
- ✅ PostgreSQL database (Supabase)
- ✅ Real-time inventory management
- ✅ Data persists across sessions
- ✅ Professional UI with Tailwind CSS

## 📊 View Your Data

Access Supabase Dashboard:
https://supabase.com/dashboard/project/ikbpmyqftmlgyvfysucs

From there you can:
- Run SQL queries
- View all data in tables
- Manage users
- Monitor API usage

## 🔧 Optional: Run Backend Services

The backend and ETL services are optional now since the frontend connects directly to Supabase.

If you want to run them for the health check and weather forecast:

**Backend (Terminal 2):**
```bash
cd backend
dotnet run --launch-profile http
```

**ETL (Terminal 3):**
```bash
cd etl
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
uvicorn app:app --reload --port 8000
```

## 📚 More Information

- Full setup details: [SUPABASE_SETUP.md](./SUPABASE_SETUP.md)
- Original README: [README.md](./README.md)

