from uuid import uuid4

from sqlalchemy.ext.asyncio import AsyncSession

from app.repositories.payment_ingestion_repository import PaymentIngestionRepository
from app.schemas.payment_ingestion import (
    PaymentIngestionRequest,
    PaymentIngestionResponse,
    PaymentIngestionResultItem,
)


class PaymentIngestionService:
    def __init__(self, db: AsyncSession):
        self.db = db
        self.repository = PaymentIngestionRepository(db)

    async def ingest_payments(
        self, request: PaymentIngestionRequest
    ) -> PaymentIngestionResponse:
        results: list[PaymentIngestionResultItem] = []
        total_inserted = 0
        total_updated = 0
        total_skipped_stale = 0
        total_failed = 0

        for payment in request.payments:
            results.append(
                PaymentIngestionResultItem(
                    external_payment_id=payment.external_payment_id,
                    status="failed",
                    payment_id=None,
                    error_message="Ingestion not yet implemented",
                )
            )
            total_failed += 1

        return PaymentIngestionResponse(
            ingestion_run_id=uuid4(),
            total_received=len(request.payments),
            total_inserted=total_inserted,
            total_updated=total_updated,
            total_skipped_stale=total_skipped_stale,
            total_failed=total_failed,
            results=results,
        )
