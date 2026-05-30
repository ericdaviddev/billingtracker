from pydantic import Field, PostgresDsn
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        case_sensitive=False,
        extra="ignore",
    )

    app_name: str = "BillingTracker API"
    app_version: str = "0.1.0"
    debug: bool = False

    database_url: PostgresDsn = Field(
        default="postgresql://postgres:postgres@localhost:5432/billingtracker",
        description="PostgreSQL connection string",
    )

    db_echo: bool = Field(
        default=False,
        description="Enable SQLAlchemy query logging",
    )

    db_pool_size: int = Field(
        default=5,
        description="Database connection pool size",
    )

    db_max_overflow: int = Field(
        default=10,
        description="Maximum overflow connections beyond pool_size",
    )


settings = Settings()
