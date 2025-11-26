# Supabase Integration Setup Guide

## ✅ What's Been Configured

Your FitWarehouse project is now connected to Supabase PostgreSQL with authentication!

### 1. **Supabase Project Details**
- **Organization**: Database Proj
- **Project Name**: FitWarehouse
- **Project URL**: `https://ikbpmyqftmlgyvfysucs.supabase.co`
- **Region**: us-west-2
- **Database**: PostgreSQL 17.6.1

### 2. **Database Schema**
Created `inventory` table with the following structure:

```sql
CREATE TABLE inventory (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_name TEXT NOT NULL,
    quantity INTEGER NOT NULL CHECK (quantity > 0),
    warehouse_location TEXT NOT NULL,
    submitted_by TEXT NOT NULL,
    processed_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

**Row Level Security (RLS)** is enabled with policies:
- Authenticated users can perform all operations
- Anonymous users can read data

### 3. **Frontend Changes**

#### New Files Created:
- `frontend/src/lib/supabase.ts` - Supabase client configuration

#### Updated Files:
- `frontend/src/Login.tsx` - Full authentication with email/password
  - Sign up functionality
  - Login functionality
  - Error handling
  - Redirects to home after successful login

- `frontend/src/App.tsx` - Integrated with Supabase
  - Loads inventory data from Supabase PostgreSQL
  - Saves new inventory directly to Supabase (bypassing ETL for now)
  - Shows logged-in user email
  - Logout functionality
  - User session management

#### Dependencies Added:
- `@supabase/supabase-js` (v2.84.0)

## 🚀 How to Use

### 1. **Create Your .env File**
Create `frontend/.env` with these values:

```env
VITE_BACKEND_BASE_URL=http://localhost:5018
VITE_ETL_BASE_URL=http://localhost:8000
VITE_SUPABASE_URL=https://ikbpmyqftmlgyvfysucs.supabase.co
VITE_SUPABASE_ANON_KEY=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImlrYnBteXFmdG1sZ3l2ZnlzdWNzIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjM4NzQyNjEsImV4cCI6MjA3OTQ1MDI2MX0.jvZwCyyd5UhzLyLnVAWB2tr7JG5cDAtTKnGch-14xFM
```

### 2. **Start the Frontend**
```bash
cd frontend
yarn dev
```

### 3. **Test Authentication**
1. Navigate to `http://localhost:5173`
2. Click "Login" button
3. Click "Sign Up" to create a new account
4. Check your email for confirmation (Supabase sends a confirmation email)
5. After confirming, log in with your credentials

### 4. **Test Inventory Management**
1. Once logged in, you'll see your email in the header
2. Submit a new inventory item using the form
3. Data is saved directly to Supabase PostgreSQL
4. The table updates in real-time

## 📊 Current Architecture

```
Frontend (React + Vite)
    ↓
Supabase PostgreSQL
    ↓
(Backend ASP.NET Core - for health/weather only)
```

**Note**: The ETL service is currently bypassed. Inventory data flows directly from frontend to Supabase.

## 🔄 Next Steps (Optional)

### Option 1: Keep Current Setup
- Frontend talks directly to Supabase
- Simple and efficient for your data warehousing project
- All SQL queries visible in Supabase dashboard

### Option 2: Re-integrate ETL Pipeline
Update the ETL service to:
1. Receive submissions from frontend
2. Clean/validate data
3. Insert into Supabase PostgreSQL (instead of ASP.NET backend)

### Option 3: Connect ASP.NET Backend to Supabase
Install Entity Framework Core with PostgreSQL provider:
```bash
cd backend
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

Then configure connection string to point to Supabase.

## 🔐 Security Notes

- The `anon` key is public and safe to use in frontend code
- Row Level Security (RLS) protects your data
- Users can only access data based on RLS policies
- Never expose the `service_role` key in frontend code

## 📱 Supabase Dashboard

Access your database at: https://supabase.com/dashboard/project/ikbpmyqftmlgyvfysucs

From the dashboard you can:
- View/edit data in the Table Editor
- Run SQL queries
- Monitor authentication users
- View logs and analytics
- Manage RLS policies

## 🎓 For Your Data Warehousing Project

This setup gives you:
- ✅ Real PostgreSQL database (industry standard for data warehousing)
- ✅ SQL access for complex queries and analytics
- ✅ Authentication system
- ✅ Ability to create views, stored procedures, and triggers
- ✅ Direct SQL query access via Supabase dashboard
- ✅ Can connect BI tools (Tableau, Power BI, etc.) to Supabase

You can now focus on:
- Creating analytical queries
- Building data models (fact/dimension tables)
- Implementing ETL transformations
- Creating reports and dashboards

