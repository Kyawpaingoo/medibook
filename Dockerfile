FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY MediBook.slnx .
COPY MediBookAPI/MediBookAPI.csproj MediBookAPI/
COPY Data/Data.csproj Data/
COPY Infra/Infra.csproj Infra/
COPY UnitTesting/UnitTesting.csproj UnitTesting/

RUN dotnet restore MediBookAPI/MediBookAPI.csproj

COPY . .
RUN dotnet publish MediBookAPI/MediBookAPI.csproj -c Release -o /app/publish --no-restore \
    --self-contained false \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build --chown=app:app /app/publish .
USER app

ENTRYPOINT ["dotnet", "MediBookAPI.dll"]
