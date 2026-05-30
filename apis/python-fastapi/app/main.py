from contextlib import asynccontextmanager

from fastapi import FastAPI

from app.api.routes import health, ingestion
from app.core.config import settings
from app.db.session import engine


@asynccontextmanager
async def lifespan(app: FastAPI):
    yield
    await engine.dispose()


app = FastAPI(
    title=settings.app_name,
    version=settings.app_version,
    lifespan=lifespan,
)

app.include_router(health.router, tags=["health"])
app.include_router(ingestion.router, prefix="/api/ingestion", tags=["ingestion"])
