# FitWarehouse — Running Data Warehouse

This project ingests long-distance running data (weekly athlete metrics), loads it into a PostgreSQL star schema, and surfaces OLAP-style analytics via a C# API and React (Vite) frontend. Python is used only as a client-side loader; the C# backend handles ingestion, staging → warehouse promotion, and analytics endpoints.

## What data warehouse and why?
- **Warehouse**: PostgreSQL (star schema with fact `fact_run_weekly` and dimensions for athlete, age group, gender, country, date, major). Staging tables (`stg_runs_raw`, `stg_ingest_log`) capture raw loads.
- **Why**: To analyze trends and cohorts in running volume/pace across age groups, gender, country, and marathon majors; to spot differences in training load, pace distributions, and progression.

## Questions/associations (examples)i
- How does weekly distance or pace trend by age group, gender, or country over time?
- Which age groups or genders log the most distance or fastest pace?
- How does major participation (e.g., Boston, Berlin, London) relate to training volume?
- Which countries contribute the most training distance and what is their average pace?
- Per-athlete progression: how has an athlete’s pace changed over the last year?

## OLAP queries (plain English)
1) “For the last N weeks, show average weekly distance per runner, grouped by age group (or gender), week over week.”  
2) “For each major (by year), show average weekly distance per runner over the last N weeks.”  
3) “Top 50 countries by total distance in the last N weeks, and their average pace.”  
4) “For athlete X, show weekly pace (min/km) over the last 52 weeks.”

## SQL representations (samples)
- Weekly distance by age group (per-runner average):
```sql
with recent_weeks as (
  select distinct d.date_key, d.week_start_date
  from fact_run_weekly frw
  join dim_date d on d.date_key = frw.date_key
  order by d.date_key desc
  limit @weeks
)
select d.week_start_date,
       coalesce(ag.age_group_label, 'Unknown') as age_group,
       sum(coalesce(frw.distance_km, 0)) / nullif(count(distinct frw.athlete_key), 0) as avg_distance_km_per_runner
from fact_run_weekly frw
join dim_date d on d.date_key = frw.date_key
join dim_athlete a on a.athlete_key = frw.athlete_key
left join dim_age_group ag on ag.age_group_key = a.age_group_key
join recent_weeks rw on rw.date_key = d.date_key
group by d.week_start_date, ag.age_group_label
order by d.week_start_date, ag.age_group_label;
```
- Major × year average distance per runner:
```sql
with recent_weeks as (
  select distinct d.date_key, d.week_start_date
  from fact_run_weekly frw
  join dim_date d on d.date_key = frw.date_key
  order by d.date_key desc
  limit @weeks
)
select d.week_start_date,
       coalesce(dm.major_year::text, 'Unknown') as major_year,
       sum(coalesce(frw.distance_km,0)) / nullif(count(distinct frw.athlete_key), 0) as avg_distance_km_per_runner
from fact_run_weekly frw
join dim_date d on d.date_key = frw.date_key
join dim_athlete a on a.athlete_key = frw.athlete_key
join bridge_athlete_major bam on bam.athlete_key = a.athlete_key
join dim_major dm on dm.major_key = bam.major_key
join recent_weeks rw on rw.date_key = d.date_key
group by d.week_start_date, dm.major_year
order by d.week_start_date, dm.major_year;
```
- Top countries by distance + avg pace:
```sql
with recent_weeks as (
  select distinct d.date_key, d.week_start_date
  from fact_run_weekly frw
  join dim_date d on d.date_key = frw.date_key
  order by d.date_key desc
  limit @weeks
)
select coalesce(c.country_name, 'Unknown') as country,
       sum(coalesce(frw.distance_km,0)) as total_distance_km,
       case when sum(coalesce(frw.distance_km,0)) = 0 then null
            else sum(coalesce(frw.duration_min,0)) / sum(coalesce(frw.distance_km,0)) end as avg_pace_min_per_km
from fact_run_weekly frw
join dim_date d on d.date_key = frw.date_key
join dim_athlete a on a.athlete_key = frw.athlete_key
left join dim_country c on c.country_key = a.country_key
join recent_weeks rw on rw.date_key = d.date_key
group by c.country_name
order by total_distance_km desc nulls last
limit 50;
```
- Athlete progression (pace over last 52 weeks):
```sql
with recent_weeks as (
  select distinct d.date_key, d.week_start_date
  from fact_run_weekly frw
  join dim_date d on d.date_key = frw.date_key
  join dim_athlete a on a.athlete_key = frw.athlete_key
  where a.athlete_id_source = @athleteId
  order by d.date_key desc
  limit @weeks
)
select d.week_start_date,
       coalesce(frw.pace_min_per_km, case when frw.distance_km = 0 then null else frw.duration_min / nullif(frw.distance_km,0) end) as pace_min_per_km
from fact_run_weekly frw
join dim_date d on d.date_key = frw.date_key
join dim_athlete a on a.athlete_key = frw.athlete_key
join recent_weeks rw on rw.date_key = d.date_key
where a.athlete_id_source = @athleteId;
```

## OLAP schema (star)
- **Fact**: `fact_run_weekly` (PK: date_key + athlete_key; measures: distance_km, duration_min, pace_min_per_km, load fields, flags).
- **Dimensions**:  
  - `dim_date` (week grain)  
  - `dim_athlete` (links to gender, age_group, country)  
  - `dim_gender`, `dim_age_group`, `dim_country`, `dim_major`  
  - Bridge: `bridge_athlete_major` (athlete ↔ major).
- **Staging**: `stg_runs_raw`, `stg_ingest_log`.

## Data sources
- Kaggle “Long Distance Running Dataset” (weekly runs): `run_ww_2019_w.csv`, `run_ww_2020_w.csv`.

## Technology stack
- **Database**: PostgreSQL 
- **Backend**: C# ASP.NET Core Web API with Npgsql EF Core mappings for the star schema; raw SQL for OLAP endpoints.  
- **ETL**: Python CLI (psycopg/csv) to load CSVs into staging; C# promotes to dims/fact.  
- **Frontend**: React + Vite; Chart.js for visuals.

## Current analytics endpoints (high level)
- `/api/analytics/overview`: KPIs + weekly distance by age group (per-runner average).
- `/api/analytics/top-countries`: top N countries by distance + avg pace.
- `/api/analytics/major-gender-distribution`: runner counts by gender for top majors.
- `/api/analytics/major-distance-by-year`: average distance per runner by major year.
- `/api/analytics/distance-by-gender`: weekly average distance per runner by gender.
- `/api/analytics/athlete-pace`: pace progression + majors for a given athlete.

## Frontend views
- **Overview**: General quereis, weekly distance by age group, top countries table, gender distribution by major.
- **Trends**: Pace by gender+age group, distance by major year, distance by gender.
- **Athlete**: Per-athlete pace over time + majors list.
