FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Shared build settings and central package versions; restore needs them before any project.
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]

# Api
COPY ["src/api/Cvitlo.Api/Cvitlo.Api.csproj", "src/api/Cvitlo.Api/"]

# Aspire
COPY ["src/api/Aspire/Cvitlo.ServiceDefaults/Cvitlo.ServiceDefaults.csproj", "src/api/Aspire/Cvitlo.ServiceDefaults/"]

# Auth
COPY ["src/api/Modules/Auth/Cvitlo.Auth.Infrastructure/Cvitlo.Auth.Infrastructure.csproj", "src/api/Modules/Auth/Cvitlo.Auth.Infrastructure/"]
COPY ["src/api/Modules/Auth/Cvitlo.Auth.Application/Cvitlo.Auth.Application.csproj", "src/api/Modules/Auth/Cvitlo.Auth.Application/"]
COPY ["src/api/Modules/Auth/Cvitlo.Auth.PublicApi/Cvitlo.Auth.PublicApi.csproj", "src/api/Modules/Auth/Cvitlo.Auth.PublicApi/"]

# Common
COPY ["src/api/Common/Cvitlo.Common/Cvitlo.Common.csproj", "src/api/Common/Cvitlo.Common/"]

# Documents
COPY ["src/api/Modules/Documents/Cvitlo.Documents.Application/Cvitlo.Documents.Application.csproj", "src/api/Modules/Documents/Cvitlo.Documents.Application/"]
COPY ["src/api/Modules/Documents/Cvitlo.Documents.PublicApi/Cvitlo.Documents.PublicApi.csproj", "src/api/Modules/Documents/Cvitlo.Documents.PublicApi/"]

# Resumes
COPY ["src/api/Modules/Resumes/Cvitlo.Resumes.Domain/Cvitlo.Resumes.Domain.csproj", "src/api/Modules/Resumes/Cvitlo.Resumes.Domain/"]
COPY ["src/api/Modules/Resumes/Cvitlo.Resumes.Application/Cvitlo.Resumes.Application.csproj", "src/api/Modules/Resumes/Cvitlo.Resumes.Application/"]
COPY ["src/api/Modules/Resumes/Cvitlo.Resumes.Infrastructure/Cvitlo.Resumes.Infrastructure.csproj", "src/api/Modules/Resumes/Cvitlo.Resumes.Infrastructure/"]
COPY ["src/api/Modules/Resumes/Cvitlo.Resumes.PublicApi/Cvitlo.Resumes.PublicApi.csproj", "src/api/Modules/Resumes/Cvitlo.Resumes.PublicApi/"]

RUN dotnet restore "src/api/Cvitlo.Api/Cvitlo.Api.csproj"
COPY . .
WORKDIR "/src/src/api/Cvitlo.Api"
RUN dotnet build "Cvitlo.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Install PowerShell
RUN wget -q "https://github.com/PowerShell/PowerShell/releases/download/v7.6.6/powershell_7.6.6-1.deb_amd64.deb" && \
    dpkg -i powershell_7.6.6-1.deb_amd64.deb || true && \
    apt-get update && \
    apt-get install -f -y && \
    rm powershell_7.6.6-1.deb_amd64.deb

# Install Playwright browsers using PowerShell script
RUN pwsh /app/build/playwright.ps1 install

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Cvitlo.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
RUN apt-get update && apt-get install -y \
    curl \
    wget \
    libnss3 \
    libnspr4 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libasound2 \
    libpango-1.0-0 \
    libcairo2 \
    libatspi2.0-0 \
    fonts-liberation \
    xvfb \
    xauth \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /root/.cache/ms-playwright /root/.cache/ms-playwright

# Ensure proper permissions
RUN chmod -R 777 /app
RUN chmod -R 777 /root/.cache/ms-playwright
ENTRYPOINT ["dotnet", "Cvitlo.Api.dll"]
