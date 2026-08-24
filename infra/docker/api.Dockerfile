# Portfolio API — multi-stage build.
# Build context is the repository root, because the image needs both the solution and content/.
#
#   docker build -f infra/docker/api.Dockerfile -t portfolio-api .

# --- build ---------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

# The repository layout is reproduced inside the image rather than flattened. The API project links
# content/*.json by a relative path; flattening would make that path resolve somewhere else and the
# breakage would only show at runtime, as an empty portfolio.
WORKDIR /repo
ARG API=src/services/portfolio-api

# The root .editorconfig is part of the build, not an editor convenience: it carries the analyzer
# severities and the rule that exempts EF's generated migrations. Without it the image build fails
# on warnings-as-errors in code nobody wrote by hand.
COPY .editorconfig .

# Project files next: restore is the slowest step and only has to re-run when a dependency changes,
# not when a line of C# does.
COPY ${API}/Directory.Build.props ${API}/
COPY ${API}/Portfolio.Domain/*.csproj ${API}/Portfolio.Domain/
COPY ${API}/Portfolio.Application/*.csproj ${API}/Portfolio.Application/
COPY ${API}/Portfolio.Infrastructure/*.csproj ${API}/Portfolio.Infrastructure/
COPY ${API}/Portfolio.Api/*.csproj ${API}/Portfolio.Api/
RUN dotnet restore ${API}/Portfolio.Api/Portfolio.Api.csproj

COPY ${API}/ ${API}/
COPY content/ content/

RUN dotnet publish ${API}/Portfolio.Api/Portfolio.Api.csproj \
        -c Release \
        -o /app \
        --no-restore \
        /p:UseAppHost=false

# --- runtime -------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# A non-root user with no shell and no home directory. The service is stateless and never writes, so
# the container can also run with a read-only filesystem — see docker-compose.yml.
RUN addgroup -S portfolio && adduser -S -G portfolio -H -s /sbin/nologin portfolio

COPY --from=build --chown=root:root /app ./

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080
USER portfolio

# Liveness only. It must not touch the database: a content or connectivity problem should not make
# an orchestrator restart a process that is running perfectly well.
HEALTHCHECK --interval=15s --timeout=3s --start-period=25s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://127.0.0.1:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "Portfolio.Api.dll"]
