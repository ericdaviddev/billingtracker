from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.ext.asyncio import AsyncSession

from app.db.session import get_db
from app.schemas.payment_ingestion import (
    PaymentIngestionRequest,
    PaymentIngestionResponse,
)
from app.services.payment_ingestion_service import PaymentIngestionService

router = APIRouter()


@router.post(
    "/payments",
    response_model=PaymentIngestionResponse,
    status_code=status.HTTP_200_OK,
    summary="Ingest payment records from external source systems",
)
async def ingest_payments(
    request: PaymentIngestionRequest,
    db: AsyncSession = Depends(get_db),
):
    service = PaymentIngestionService(db)
    try:
        result = await service.ingest_payments(request)
        return result
    except ValueError as e:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=str(e),
        )
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Ingestion failed: {str(e)}",
        )
