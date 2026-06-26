from sqlalchemy.ext.asyncio import AsyncSession


class PaymentIngestionRepository:
    def __init__(self, db: AsyncSession):
        self.db = db
