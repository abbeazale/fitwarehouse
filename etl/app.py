"""
Utility script to clean the runs CSVs and POST batches to the C# ingest API.
Paths are static; intended to be run once.
"""

import csv
import os
from datetime import datetime
from pathlib import Path
from typing import Iterable, List, Dict, Any

import httpx
from dotenv import load_dotenv


load_dotenv()

CSV_FILES = [
    "run_ww_2019_w.csv",
    "run_ww_2020_w.csv",
]


def parse_rows(csv_path: Path) -> List[Dict[str, Any]]:
    """Read the runs CSV, drop the source id, normalize columns, split majors."""
    rows: List[Dict[str, Any]] = []
    with csv_path.open(newline="", encoding="utf-8") as f:
        reader = csv.DictReader(f)
        for line_no, row in enumerate(reader, start=2):  # header is line 1
            try:
                run_date_raw = row.get("datetime") or row.get("date")
                if not run_date_raw:
                    raise ValueError("missing datetime")
                run_date = datetime.fromisoformat(run_date_raw).date().isoformat()

                athlete_id = row.get("athlete")
                if athlete_id is None or athlete_id == "":
                    raise ValueError("missing athlete id")

                distance = row.get("distance")
                duration = row.get("duration")

                majors_raw = row.get("major", "") or ""
                majors = [m.strip() for m in majors_raw.split(",") if m.strip()]

                payload = {
                    "runDate": run_date,
                    "athleteIdSource": int(athlete_id),
                    "distanceKm": float(distance) if distance not in (None, "", "0") else 0.0,
                    "durationMin": float(duration) if duration not in (None, "") else 0.0,
                    "gender": (row.get("gender") or "").strip() or None,
                    "ageGroup": (row.get("age_group") or "").strip() or None,
                    "country": (row.get("country") or "").strip() or None,
                    "majors": majors if majors else None,
                }
                rows.append(payload)
            except Exception as exc:
                print(f"Skipping line {line_no} in {csv_path.name} due to parse error: {exc}")
                continue
    return rows


def chunked(items: List[Dict[str, Any]], size: int) -> Iterable[List[Dict[str, Any]]]:
    for i in range(0, len(items), size):
        yield items[i : i + size]


def send_file(client: httpx.Client, endpoint: str, csv_path: Path, batch_size: int) -> int:
    rows = parse_rows(csv_path)
    print(f"Prepared {len(rows)} rows from {csv_path.name}")

    sent = 0
    for chunk in chunked(rows, batch_size):
        resp = client.post(endpoint, json=chunk)
        if resp.is_error:
            raise RuntimeError(f"Error posting batch at offset {sent} for {csv_path.name}: {resp.status_code} {resp.text}")
        data = resp.json()
        sent += data.get("receivedCount", len(chunk))
        print(f"{csv_path.name}: Batch sent. BatchId={data.get('batchId')} Received={data.get('receivedCount')}")
    return sent


def main() -> None:
    base_dir = Path(__file__).parent
    backend_base = os.getenv("BACKEND_BASE_URL", "http://localhost:5018").rstrip("/")
    endpoint = f"{backend_base}/api/ingest/runs"
    batch_size = int(os.getenv("INGEST_BATCH_SIZE", "5000"))

    with httpx.Client(timeout=60) as client:
        total_sent = 0
        for name in CSV_FILES:
            csv_path = base_dir / name
            if not csv_path.exists():
                raise FileNotFoundError(f"CSV file not found: {csv_path}")
            total_sent += send_file(client, endpoint, csv_path, batch_size)

    print(f"Done. Sent {total_sent} rows total across {len(CSV_FILES)} files.")


if __name__ == "__main__":
    main()
