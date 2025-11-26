# Full Data Pipeline Setup Guide

## 🎯 Architecture Overview

Your complete data warehousing pipeline now follows this flow:

```
Frontend (React + Vite)
    ↓ Upload CSV/XML files
Python ETL (FastAPI)
    ↓ Parse, Clean, Validate
C# Backend (ASP.NET Core Web API)
    ↓ Business Logic + Entity Framework
Supabase PostgreSQL
    ↓ Persistent Storage
```

## ✅ What's Been Implemented

### 1. Frontend Updates
- **File Upload Component** (`frontend/src/components/FileUpload.tsx`)
  - Accepts CSV and XML files
  - Shows upload progress and results
  - Displays success/error messages
  
- **Updated App.tsx**
  - Added file upload section
  - Kept manual entry form as backup
  - Auto-refreshes data after upload

### 2. Python ETL Service
- **CSV Parser** - Handles comma-separated values
- **XML Parser** - Handles XML inventory records
- **Data Cleaning** - Normalizes text, extracts quantities
- **Batch Processing** - Processes multiple records at once
- **Error Handling** - Reports which rows failed and why

### 3. C# Backend
- **Entity Framework Core** - ORM for database access
- **PostgreSQL Provider** - Npgsql for Supabase connection
- **DbContext** - Maps C# models to database tables
- **Async Operations** - Better performance

### 4. Database
- **Supabase PostgreSQL** - Already configured with inventory table
- **Row Level Security** - Data protection
- **Connection via EF Core** - Type-safe queries

## 🚀 Setup Instructions

### Step 1: Install Backend Dependencies

```bash
cd backend
dotnet restore
```

This will install:
- Entity Framework Core 9.0
- Npgsql (PostgreSQL provider)
- EF Core Design tools

### Step 2: Configure Database Connection

You need your Supabase database password. Get it from:
1. Go to https://supabase.com/dashboard/project/ikbpmyqftmlgyvfysucs
2. Click **Settings** → **Database**
3. Find your database password (or reset it if needed)

Update `backend/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "SupabaseConnection": "Host=db.ikbpmyqftmlgyvfysucs.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD_HERE"
  }
}
```

**Important**: Replace `YOUR_PASSWORD_HERE` with your actual Supabase database password.

### Step 3: Install Python Dependencies

```bash
cd etl
pip install -r requirements.txt
```

This adds `python-multipart` for file upload handling.

### Step 4: Start All Services

Open **3 terminals**:

**Terminal 1 - Backend:**
```bash
cd backend
dotnet run --launch-profile http
```
Should start on http://localhost:5018

**Terminal 2 - ETL:**
```bash
cd etl
source .venv/bin/activate  # or create venv if needed
uvicorn app:app --reload --port 8000
```
Should start on http://localhost:8000

**Terminal 3 - Frontend:**
```bash
cd frontend
yarn dev
```
Should start on http://localhost:5173

## 📊 Testing the Pipeline

### Test 1: Upload CSV File

1. Open http://localhost:5173
2. Log in (or sign up if you haven't)
3. Find the "Upload Inventory File" section
4. Click "Choose File" and select `sample_data/inventory_sample.csv`
5. Click "Upload File"
6. Watch the data appear in the inventory table below!

### Test 2: Upload XML File

1. Same steps as above
2. Select `sample_data/inventory_sample.xml` instead
3. Upload and verify

### Test 3: Manual Entry (Still Works!)

1. Scroll to "Submit Inventory (Manual Entry)"
2. Fill in the form
3. Click "Send to ETL"
4. Data goes through the same pipeline

## 🔍 How Data Flows

### CSV/XML Upload Flow:

1. **Frontend** → User selects file
2. **Frontend** → Sends file to `POST /upload` on ETL service
3. **ETL** → Parses CSV or XML into individual records
4. **ETL** → Cleans each record (normalizes text, extracts numbers)
5. **ETL** → Sends each cleaned record to `POST /api/inventory` on backend
6. **Backend** → Validates data
7. **Backend** → Uses Entity Framework to insert into Supabase
8. **Supabase** → Stores in PostgreSQL
9. **Frontend** → Refreshes and shows new data

### Manual Entry Flow:

1. **Frontend** → User fills form
2. **Frontend** → Sends to `POST /ingest` on ETL service
3. **ETL** → Cleans the single record
4. **ETL** → Forwards to backend
5. **Backend** → Saves to database via Entity Framework
6. **Frontend** → Shows success message

## 📁 Sample Data Files

Two sample files are provided in `sample_data/`:

**inventory_sample.csv:**
- 7 records
- Tests CSV parsing
- Includes quantities with "units" text

**inventory_sample.xml:**
- 5 records
- Tests XML parsing
- Same format as CSV

## 🛠️ Troubleshooting

### Backend won't start?
- Make sure you set the database password in `appsettings.json`
- Run `dotnet restore` to install packages
- Check if port 5018 is available

### ETL service errors?
- Make sure `python-multipart` is installed
- Check if backend is running (ETL needs it)
- Verify port 8000 is available

### File upload fails?
- Check ETL service is running
- Look at ETL terminal for error messages
- Verify file format matches samples

### No data showing?
- Check browser console for errors
- Verify backend can connect to Supabase
- Test database connection in Supabase dashboard

## 🎓 For Your Data Warehousing Project

This setup gives you:

### ✅ ETL Pipeline
- **Extract**: CSV/XML file parsing
- **Transform**: Data cleaning, normalization, validation
- **Load**: Insertion into PostgreSQL via Web API

### ✅ Data Warehouse Features
- **Staging Area**: ETL service acts as staging
- **Data Quality**: Validation and cleaning rules
- **Batch Processing**: Handle multiple records
- **Error Logging**: Track failed records

### ✅ Best Practices
- **Separation of Concerns**: Frontend → ETL → API → Database
- **Type Safety**: C# with Entity Framework
- **Data Validation**: Multiple layers (ETL + Backend)
- **Scalability**: Each service can scale independently

## 🔐 Security Notes

- Database password should be in environment variables (not committed)
- Use `appsettings.Development.json` for local dev
- Never commit passwords to git
- Consider using Azure Key Vault or similar for production

## 📈 Next Steps

1. **Add more data transformations** in ETL
2. **Create fact and dimension tables** in Supabase
3. **Build analytical queries** for reporting
4. **Add data validation rules** specific to your domain
5. **Create views** in PostgreSQL for common queries
6. **Connect BI tools** (Tableau, Power BI) to Supabase

## 🎉 You're All Set!

You now have a complete data warehousing pipeline with:
- File upload (CSV/XML)
- ETL processing
- Web API layer
- PostgreSQL storage
- Authentication
- Real-time updates

Perfect for your data warehousing class project!

