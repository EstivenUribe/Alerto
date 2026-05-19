FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Alerto.Domain/Alerto.Domain.csproj src/Alerto.Domain/
COPY src/Alerto.Application/Alerto.Application.csproj src/Alerto.Application/
COPY src/Alerto.Infrastructure/Alerto.Infrastructure.csproj src/Alerto.Infrastructure/
COPY src/Alerto.Api/Alerto.Api.csproj src/Alerto.Api/

RUN dotnet restore src/Alerto.Api/Alerto.Api.csproj

COPY src/ src/
RUN dotnet publish src/Alerto.Api/Alerto.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Alerto.Api.dll"]
