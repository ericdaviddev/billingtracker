# BillingTracker Python/FastAPI API

Python/FastAPI implementation of the BillingTracker multi-tenant billing ingestion platform.

## Tech Stack

- **Python**: 3.13+
- **FastAPI**: Modern async web framework
- **Pydantic v2**: Data validation and settings management
- **SQLAlchemy 2.x**: Async ORM
- **Alembic**: Database migrations
- **PostgreSQL**: Primary database
- **pytest**: Testing framework

## Project Structure

```
apis/python-fastapi/
├── app/
│   ├── main.py                    # FastAPI application entry point
│   ├── api/
│   │   └── routes/
│   │       ├── health.py          # Health check endpoint
│   │       └── ingestion.py       # Payment ingestion endpoints
│   ├── core/
│   │   └── config.py              # Application configuration
│   ├── db/
│   │   └── session.py             # Database session management
│   ├── schemas/
│   │   └── payment_ingestion.py   # Pydantic models for ingestion
│   ├── services/
│   │   └── payment_ingestion_service.py  # Business logic
│   └── repositories/
│       └── payment_ingestion_repository.py  # Data access layer
├── tests/
│   └── test_health.py             # Health endpoint tests
├── pyproject.toml                 # Project dependencies and config
├── .env.example                   # Environment variables template
└── README.md                      # This file
```

## Architecture

This API follows a clean layered architecture:

- **Routes**: HTTP request/response handling
- **Services**: Business logic and orchestration
- **Repositories**: Data access and database operations
- **Schemas**: Pydantic models for validation and serialization

The API shares the PostgreSQL schema defined in `../../sql/` with the .NET API.

## Getting Started

### Prerequisites

- Python 3.13 or higher
- PostgreSQL 14+ with the BillingTracker schema
- pip or uv for package management

### Installation

1. **Create and activate a virtual environment:**

```powershell
# Windows PowerShell
python -m venv venv
.\venv\Scripts\Activate.ps1
```

2. **Install dependencies:**

```powershell
pip install -e ".[dev]"
```

3. **Configure environment variables:**

```powershell
cp .env.example .env
# Edit .env with your database credentials
```

4. **Ensure the database schema exists:**

The API expects the PostgreSQL schema to be created using the SQL scripts in `../../sql/`:

```sql
-- Run these in order:
-- ../../sql/00_create_schema.sql
-- ../../sql/billing_create_tables.sql
-- ../../sql/seed_data_inserts.sql (optional, for testing)
```

### Running the API

**Development server with auto-reload:**

```powershell
uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
```

The API will be available at:
- API: http://localhost:8000
- Interactive docs: http://localhost:8000/docs
- ReDoc: http://localhost:8000/redoc

### Running Tests

```powershell
pytest
```

**With coverage:**

```powershell
pytest --cov=app --cov-report=html
```

## API Endpoints

### Health Check

**GET** `/health`

Returns API health status.

**Response:**
```json
{
  "status": "ok"
}
```

### Payment Ingestion (Stub)

**POST** `/api/ingestion/payments`

Ingests payment records from external source systems.

**Request:**
```json
{
  "clientExternalId": "dentrix-client-100",
  "sourceSystem": "Dentrix",
  "payments": [
    {
      "externalPaymentId": "DX-PAY-INGEST-0001",
      "externalGuarantorId": "G-DX-1001",
      "externalDependentId": "D-DX-1001",
      "externalLocationId": "DX-LOC-MIDTOWN",
      "amount": 155.75,
      "paymentDate": "2026-05-24T12:30:00Z",
      "paymentStatus": "posted",
      "sourceUpdatedAt": "2026-05-24T12:45:00Z"
    }
  ]
}
```

**Response:**
```json
{
  "ingestionRunId": "uuid",
  "totalReceived": 1,
  "totalInserted": 0,
  "totalUpdated": 0,
  "totalSkippedStale": 0,
  "totalFailed": 1,
  "results": [
    {
      "externalPaymentId": "DX-PAY-INGEST-0001",
      "status": "failed",
      "paymentId": null,
      "errorMessage": "Ingestion not yet implemented"
    }
  ]
}
```

**Note:** The ingestion endpoint is currently a stub. Full implementation will include:
- Source system resolution
- Client resolution via `billing.client_source_mappings`
- Guarantor, dependent, and location resolution
- Ingestion run tracking
- Idempotent upsert using `ON CONFLICT DO UPDATE`
- Stale update protection using `source_updated_at`
- Error tracking in `billing.ingestion_errors`

## Configuration

Configuration is managed via environment variables and Pydantic Settings.

See `.env.example` for available options.

## Database Schema Reference

The API uses the shared PostgreSQL schema in `../../sql/`:

**Core Tables:**
- `billing.clients` - Tenant organizations
- `billing.source_systems` - External source systems
- `billing.client_source_mappings` - Client-to-source mappings
- `billing.guarantors` - Financially responsible parties
- `billing.dependents` - Dependents associated with guarantors
- `billing.locations` - Physical/operational locations
- `billing.location_source_mappings` - Location-to-source mappings
- `billing.payments` - Canonical payment records
- `billing.ingestion_runs` - Ingestion execution history
- `billing.ingestion_errors` - Failed ingestion records

## Development

### Code Quality

This project uses **ruff** for linting and formatting:

```powershell
# Format code
ruff format .

# Lint code
ruff check .

# Fix auto-fixable issues
ruff check --fix .
```

### Adding Dependencies

Edit `pyproject.toml` and reinstall:

```powershell
pip install -e ".[dev]"
```

## Next Steps

1. Implement full payment ingestion logic in `PaymentIngestionService`
2. Add SQLAlchemy models for database tables
3. Implement repository methods for entity resolution and upsert
4. Add comprehensive tests for ingestion workflows
5. Add Alembic migrations
6. Add Docker Compose for local development
7. Add authentication and authorization
8. Add observability (logging, metrics, tracing)

## License

Internal use only.
