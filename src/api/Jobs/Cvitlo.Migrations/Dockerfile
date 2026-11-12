FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Shared build settings and central package versions; restore needs them before any project.
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]

COPY ["src/api/Jobs/Cvitlo.Migrations/Cvitlo.Migrations.csproj", "src/api/Jobs/Cvitlo.Migrations/"]
COPY ["src/api/Aspire/Faber.ServiceDefaults/Faber.ServiceDefaults.csproj", "src/api/Aspire/Faber.ServiceDefaults/"]
COPY ["src/api/Modules/Resumes/Cvitlo.Resumes.Infrastructure/Cvitlo.Resumes.Infrastructure.csproj", "src/api/Modules/Resumes/Cvitlo.Resumes.Infrastructure/"]
COPY ["src/api/Modules/Resumes/Cvitlo.Resumes.Domain/Cvitlo.Resumes.Domain.csproj", "src/api/Modules/Resumes/Cvitlo.Resumes.Domain/"]
COPY ["src/api/Modules/Auth/Cvitlo.Auth.Infrastructure/Cvitlo.Auth.Infrastructure.csproj", "src/api/Modules/Auth/Cvitlo.Auth.Infrastructure/"]

RUN dotnet restore "src/api/Jobs/Cvitlo.Migrations/Cvitlo.Migrations.csproj"

COPY . .

WORKDIR "/src/src/api/Jobs/Cvitlo.Migrations"
RUN dotnet publish "Cvitlo.Migrations.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Cvitlo.Migrations.dll"]
