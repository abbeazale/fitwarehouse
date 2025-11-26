"""FitWarehouse ETL microservice.

This FastAPI application accepts raw submissions from the React frontend,
performs lightweight cleaning/validation, and forwards the sanitized payloads
to the ASP.NET Core backend for persistence.
"""

from __future__ import annotations

import csv
import io
import os
import re
import xml.etree.ElementTree as ET
from datetime import datetime, timezone
from typing import Annotated

import httpx
from dotenv import load_dotenv
from fastapi import Depends, FastAPI, File, HTTPException, UploadFile
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field


load_dotenv()

app = FastAPI(
    title="FitWarehouse ETL",
    description="Cleans data submitted from the frontend before forwarding it to the backend API.",
)


def _parse_origins() -> list[str]:
    raw_origins = os.getenv("FRONTEND_ORIGINS", "http://localhost:5173")
    return [origin.strip() for origin in raw_origins.split(",") if origin.strip()]


app.add_middleware(
    CORSMiddleware,
    allow_origins=_parse_origins(),
    allow_methods=["*"],
    allow_headers=["*"],
)


class RawSubmission(BaseModel):
    productName: str = Field(..., min_length=1, max_length=128)
    quantity: str | int = Field(..., description="Raw quantity value e.g. '15 units'")
    warehouseLocation: str = Field(..., min_length=1, max_length=64)
    submittedBy: str = Field(..., min_length=1, max_length=64)


class CleanedSubmission(BaseModel):
    productName: str
    quantity: int
    warehouseLocation: str
    submittedBy: str
    processedAtUtc: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))


def clean_submission(payload: RawSubmission) -> CleanedSubmission:
    """Normalize strings, coerce quantity into an integer, and stamp processed time."""

    def sanitize_text(value: str) -> str:
        value = value.strip()
        if not value:
            raise ValueError("All text fields must contain at least one non-space character.")
        return re.sub(r"\s+", " ", value).title()

    product = sanitize_text(payload.productName)
    location = sanitize_text(payload.warehouseLocation)
    submitted_by = sanitize_text(payload.submittedBy)

    quantity_text = str(payload.quantity).strip()
    match = re.search(r"-?\d+", quantity_text)
    if not match:
        raise ValueError("Quantity must include at least one numeric value.")

    quantity = int(match.group())
    if quantity <= 0:
        raise ValueError("Quantity must be greater than zero after cleaning.")

    return CleanedSubmission(
        productName=product,
        warehouseLocation=location,
        submittedBy=submitted_by,
        quantity=quantity,
    )


async def get_backend_client():
    base_url = os.getenv("BACKEND_BASE_URL", "http://localhost:5018").rstrip("/")
    async with httpx.AsyncClient(base_url=base_url, timeout=10) as client:
        yield client


BackendClient = Annotated[httpx.AsyncClient, Depends(get_backend_client)]


@app.get("/health", tags=["Health"])
async def healthcheck():
    return {"status": "ok", "timestampUtc": datetime.utcnow().isoformat()}


@app.post("/ingest", tags=["Inventory"])
async def ingest_submission(payload: RawSubmission, client: BackendClient):
    try:
        cleaned = clean_submission(payload)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc

    target_endpoint = "/api/inventory"

    try:
        response = await client.post(target_endpoint, json=cleaned.model_dump(mode="json"))
    except httpx.RequestError as exc:
        raise HTTPException(
            status_code=502,
            detail=f"Failed to reach backend API: {exc}",
        ) from exc

    if response.is_error:
        raise HTTPException(
            status_code=response.status_code,
            detail=response.text,
        )

    return {
        "cleanedSubmission": cleaned,
        "backendResponse": response.json(),
    }


def parse_csv_file(content: str) -> list[RawSubmission]:
    """Parse CSV file content and return list of raw submissions."""
    submissions = []
    reader = csv.DictReader(io.StringIO(content))
    
    for row in reader:
        try:
            submission = RawSubmission(
                productName=row.get('productName', row.get('product_name', '')),
                quantity=row.get('quantity', '0'),
                warehouseLocation=row.get('warehouseLocation', row.get('warehouse_location', '')),
                submittedBy=row.get('submittedBy', row.get('submitted_by', ''))
            )
            submissions.append(submission)
        except Exception as e:
            print(f"Error parsing CSV row: {row}, error: {e}")
            continue
    
    return submissions


def parse_xml_file(content: str) -> list[RawSubmission]:
    """Parse XML file content and return list of raw submissions."""
    submissions = []
    
    try:
        root = ET.fromstring(content)
        
        # Support multiple XML structures
        items = root.findall('.//item') or root.findall('.//record') or root.findall('.//inventory')
        
        for item in items:
            try:
                product_name = (
                    item.find('productName') or 
                    item.find('product_name') or 
                    item.find('name')
                )
                quantity = item.find('quantity')
                warehouse = (
                    item.find('warehouseLocation') or 
                    item.find('warehouse_location') or 
                    item.find('location')
                )
                submitted_by = (
                    item.find('submittedBy') or 
                    item.find('submitted_by') or 
                    item.find('user')
                )
                
                if all([product_name is not None, quantity is not None, 
                       warehouse is not None, submitted_by is not None]):
                    submission = RawSubmission(
                        productName=product_name.text or '',
                        quantity=quantity.text or '0',
                        warehouseLocation=warehouse.text or '',
                        submittedBy=submitted_by.text or ''
                    )
                    submissions.append(submission)
            except Exception as e:
                print(f"Error parsing XML item: {ET.tostring(item)}, error: {e}")
                continue
                
    except ET.ParseError as e:
        raise ValueError(f"Invalid XML format: {e}")
    
    return submissions


@app.post("/upload", tags=["Inventory"])
async def upload_file(file: UploadFile = File(...), client: BackendClient = Depends(get_backend_client)):
    """
    Upload CSV or XML file containing inventory records.
    The file will be parsed, cleaned, and sent to the backend.
    """
    if not file.filename:
        raise HTTPException(status_code=400, detail="No file provided")
    
    file_extension = file.filename.split('.')[-1].lower()
    
    if file_extension not in ['csv', 'xml']:
        raise HTTPException(
            status_code=400, 
            detail="Only CSV and XML files are supported"
        )
    
    try:
        # Read file content
        content = await file.read()
        content_str = content.decode('utf-8')
        
        # Parse based on file type
        if file_extension == 'csv':
            raw_submissions = parse_csv_file(content_str)
        else:  # xml
            raw_submissions = parse_xml_file(content_str)
        
        if not raw_submissions:
            raise HTTPException(
                status_code=400,
                detail="No valid records found in file"
            )
        
        # Clean and send each submission to backend
        results = []
        errors = []
        
        for idx, raw_submission in enumerate(raw_submissions):
            try:
                # Clean the submission
                cleaned = clean_submission(raw_submission)
                
                # Send to backend
                response = await client.post(
                    "/api/inventory",
                    json=cleaned.model_dump(mode="json")
                )
                
                if response.is_error:
                    errors.append({
                        "row": idx + 1,
                        "error": f"Backend error: {response.text}"
                    })
                else:
                    results.append(response.json())
                    
            except ValueError as e:
                errors.append({
                    "row": idx + 1,
                    "error": f"Validation error: {str(e)}"
                })
            except httpx.RequestError as e:
                errors.append({
                    "row": idx + 1,
                    "error": f"Network error: {str(e)}"
                })
        
        return {
            "recordsProcessed": len(results),
            "recordsFailed": len(errors),
            "totalRecords": len(raw_submissions),
            "successfulRecords": results,
            "errors": errors if errors else None
        }
        
    except UnicodeDecodeError:
        raise HTTPException(
            status_code=400,
            detail="File encoding error. Please ensure file is UTF-8 encoded."
        )
    except Exception as e:
        raise HTTPException(
            status_code=500,
            detail=f"Error processing file: {str(e)}"
        )


if __name__ == "__main__":
    import uvicorn

    uvicorn.run("app:app", host="0.0.0.0", port=int(os.getenv("PORT", "8000")), reload=True)

