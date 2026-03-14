# =========================
# Runtime
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080

# =========================
# Build
# =========================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Api.Public/Api.Public.csproj", "Api.Public/"]
COPY ["Api.Admin/Api.Admin.csproj", "Api.Admin/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]

RUN dotnet restore "Api.Public/Api.Public.csproj"
RUN dotnet restore "Api.Admin/Api.Admin.csproj"

COPY . .

# =========================
# Publish Api.Public
# =========================
FROM build AS publish-public
WORKDIR /src/Api.Public
RUN dotnet publish -c Release -o /app/publish

# =========================
# Publish Api.Admin
# =========================
FROM build AS publish-admin
WORKDIR /src/Api.Admin
RUN dotnet publish -c Release -o /app/publish

# =========================
# Final Api.Public
# =========================
FROM runtime AS api-public
WORKDIR /app
COPY --from=publish-public /app/publish .
ENTRYPOINT ["dotnet", "Api.Public.dll"]

# =========================
# Final Api.Admin
# =========================
FROM runtime AS api-admin
WORKDIR /app
COPY --from=publish-admin /app/publish .
ENTRYPOINT ["dotnet", "Api.Admin.dll"]
