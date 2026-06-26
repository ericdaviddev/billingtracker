FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0-bookworm

# The base devcontainer image currently includes a Yarn apt source
# whose signing key can break apt-get update during feature installation.
RUN rm -f /etc/apt/sources.list.d/yarn.list \
    /etc/apt/sources.list.d/yarn*.list \
    /etc/apt/trusted.gpg.d/yarn*.gpg \
    && apt-get update \
    && apt-get install -y --no-install-recommends postgresql-client \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*