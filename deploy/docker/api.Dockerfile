FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . /src/system/integration-plataform
COPY --from=archon-framework . /src/frameworks/archon-framework

WORKDIR /src/system/integration-plataform/IntegrationPlataform
RUN dotnet restore "IntegrationPlataform.Api/IntegrationPlataform.Api.csproj"
RUN dotnet publish "IntegrationPlataform.Api/IntegrationPlataform.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "IntegrationPlataform.Api.dll"]
