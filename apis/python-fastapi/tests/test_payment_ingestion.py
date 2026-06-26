import pytest
from fastapi.testclient import TestClient

from app.main import app

client = TestClient(app)


def test_payment_ingestion_accepts_camelcase_request():
    request_payload = {
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
                "sourceUpdatedAt": "2026-05-24T12:45:00Z",
            }
        ],
    }

    response = client.post("/api/ingestion/payments", json=request_payload)

    assert response.status_code == 200
    data = response.json()

    assert "ingestionRunId" in data
    assert "totalReceived" in data
    assert "totalInserted" in data
    assert "totalUpdated" in data
    assert "totalSkippedStale" in data
    assert "totalFailed" in data
    assert "results" in data

    assert data["totalReceived"] == 1
    assert len(data["results"]) == 1

    result_item = data["results"][0]
    assert "externalPaymentId" in result_item
    assert "status" in result_item
    assert "paymentId" in result_item
    assert "errorMessage" in result_item

    assert result_item["externalPaymentId"] == "DX-PAY-INGEST-0001"


def test_payment_ingestion_response_uses_camelcase():
    request_payload = {
        "clientExternalId": "dentrix-client-200",
        "sourceSystem": "Dentrix",
        "payments": [
            {
                "externalPaymentId": "DX-PAY-TEST-0002",
                "externalGuarantorId": "G-DX-2001",
                "externalDependentId": "D-DX-2001",
                "externalLocationId": "DX-LOC-DOWNTOWN",
                "amount": 250.00,
                "paymentDate": "2026-05-25T10:00:00Z",
                "paymentStatus": "posted",
                "sourceUpdatedAt": "2026-05-25T10:15:00Z",
            }
        ],
    }

    response = client.post("/api/ingestion/payments", json=request_payload)

    assert response.status_code == 200
    data = response.json()

    assert "ingestion_run_id" not in data
    assert "total_received" not in data
    assert "total_inserted" not in data
    assert "total_updated" not in data
    assert "total_skipped_stale" not in data
    assert "total_failed" not in data

    assert "ingestionRunId" in data
    assert "totalReceived" in data
    assert "totalInserted" in data
    assert "totalUpdated" in data
    assert "totalSkippedStale" in data
    assert "totalFailed" in data

    result_item = data["results"][0]
    assert "external_payment_id" not in result_item
    assert "payment_id" not in result_item
    assert "error_message" not in result_item

    assert "externalPaymentId" in result_item
    assert "paymentId" in result_item
    assert "errorMessage" in result_item


def test_payment_ingestion_nested_fields_use_camelcase():
    request_payload = {
        "clientExternalId": "epic-client-300",
        "sourceSystem": "Epic",
        "payments": [
            {
                "externalPaymentId": "EPIC-PAY-0003",
                "externalGuarantorId": "G-EPIC-3001",
                "externalDependentId": "D-EPIC-3001",
                "externalLocationId": "EPIC-LOC-NORTH",
                "amount": 99.99,
                "paymentDate": "2026-05-26T14:20:00Z",
                "paymentStatus": "pending",
                "sourceUpdatedAt": "2026-05-26T14:25:00Z",
            },
            {
                "externalPaymentId": "EPIC-PAY-0004",
                "externalGuarantorId": "G-EPIC-3002",
                "externalDependentId": "D-EPIC-3002",
                "externalLocationId": "EPIC-LOC-SOUTH",
                "amount": 175.50,
                "paymentDate": "2026-05-26T15:00:00Z",
                "paymentStatus": "posted",
                "sourceUpdatedAt": "2026-05-26T15:10:00Z",
            },
        ],
    }

    response = client.post("/api/ingestion/payments", json=request_payload)

    assert response.status_code == 200
    data = response.json()

    assert data["totalReceived"] == 2
    assert len(data["results"]) == 2

    for result_item in data["results"]:
        assert "externalPaymentId" in result_item
        assert result_item["externalPaymentId"] in ["EPIC-PAY-0003", "EPIC-PAY-0004"]


def test_payment_ingestion_accepts_snakecase_internally():
    request_payload = {
        "client_external_id": "athena-client-400",
        "source_system": "AthenaHealth",
        "payments": [
            {
                "external_payment_id": "ATHENA-PAY-0005",
                "external_guarantor_id": "G-ATHENA-4001",
                "external_dependent_id": "D-ATHENA-4001",
                "external_location_id": "ATHENA-LOC-WEST",
                "amount": 300.00,
                "payment_date": "2026-05-27T09:00:00Z",
                "payment_status": "posted",
                "source_updated_at": "2026-05-27T09:15:00Z",
            }
        ],
    }

    response = client.post("/api/ingestion/payments", json=request_payload)

    assert response.status_code == 200
    data = response.json()

    assert data["totalReceived"] == 1
    assert data["results"][0]["externalPaymentId"] == "ATHENA-PAY-0005"


def test_payment_ingestion_validation_errors():
    request_payload = {
        "clientExternalId": "",
        "sourceSystem": "Dentrix",
        "payments": [],
    }

    response = client.post("/api/ingestion/payments", json=request_payload)

    assert response.status_code == 422
