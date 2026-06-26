from datetime import datetime
from decimal import Decimal
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field
from pydantic.alias_generators import to_camel


class CamelCaseModel(BaseModel):
    model_config = ConfigDict(
        alias_generator=to_camel,
        populate_by_name=True,
        from_attributes=True,
    )


class PaymentIngestionItem(CamelCaseModel):
    model_config = ConfigDict(
        alias_generator=to_camel,
        populate_by_name=True,
        str_strip_whitespace=True,
        validate_assignment=True,
    )

    external_payment_id: str = Field(
        ...,
        description="External payment identifier from source system",
        min_length=1,
    )

    external_guarantor_id: str = Field(
        ...,
        description="External guarantor identifier from source system",
        min_length=1,
    )

    external_dependent_id: str = Field(
        ...,
        description="External dependent identifier from source system",
        min_length=1,
    )

    external_location_id: str = Field(
        ...,
        description="External location identifier from source system",
        min_length=1,
    )

    amount: Decimal = Field(
        ...,
        description="Payment amount",
        ge=0,
        decimal_places=2,
    )

    payment_date: datetime = Field(
        ...,
        description="Payment date from source system",
    )

    payment_status: str = Field(
        ...,
        description="Payment status (e.g., posted, pending, failed, reversed)",
        min_length=1,
    )

    source_updated_at: datetime = Field(
        ...,
        description="Last update timestamp from source system",
    )


class PaymentIngestionRequest(CamelCaseModel):
    model_config = ConfigDict(
        alias_generator=to_camel,
        populate_by_name=True,
        str_strip_whitespace=True,
        validate_assignment=True,
    )

    client_external_id: str = Field(
        ...,
        description="External client identifier from source system",
        min_length=1,
    )

    source_system: str = Field(
        ...,
        description="Source system name (e.g., Dentrix, Epic, AthenaHealth)",
        min_length=1,
    )

    payments: list[PaymentIngestionItem] = Field(
        ...,
        description="List of payment records to ingest",
        min_length=1,
    )


class PaymentIngestionResultItem(CamelCaseModel):
    model_config = ConfigDict(
        alias_generator=to_camel,
        populate_by_name=True,
        from_attributes=True,
    )

    external_payment_id: str = Field(
        ...,
        description="External payment identifier",
    )

    status: str = Field(
        ...,
        description="Result status: inserted, updated, skipped_stale, failed",
    )

    payment_id: UUID | None = Field(
        default=None,
        description="Internal payment UUID if successful",
    )

    error_message: str | None = Field(
        default=None,
        description="Error message if status is failed",
    )


class PaymentIngestionResponse(CamelCaseModel):
    model_config = ConfigDict(
        alias_generator=to_camel,
        populate_by_name=True,
        from_attributes=True,
    )

    ingestion_run_id: UUID = Field(
        ...,
        description="Ingestion run identifier",
    )

    total_received: int = Field(
        ...,
        description="Total number of payment records received",
    )

    total_inserted: int = Field(
        ...,
        description="Number of new payment records inserted",
    )

    total_updated: int = Field(
        ...,
        description="Number of existing payment records updated",
    )

    total_skipped_stale: int = Field(
        ...,
        description="Number of records skipped due to stale source_updated_at",
    )

    total_failed: int = Field(
        ...,
        description="Number of records that failed to process",
    )

    results: list[PaymentIngestionResultItem] = Field(
        ...,
        description="Per-record ingestion results",
    )
