# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY PedidosVendas.slnx ./
COPY src/PedidosVendas.Domain/PedidosVendas.Domain.csproj src/PedidosVendas.Domain/
COPY src/PedidosVendas.Application/PedidosVendas.Application.csproj src/PedidosVendas.Application/
COPY src/PedidosVendas.Infrastructure/PedidosVendas.Infrastructure.csproj src/PedidosVendas.Infrastructure/
COPY src/PedidosVendas.API/PedidosVendas.API.csproj src/PedidosVendas.API/
COPY tests/PedidosVendas.Tests.Unit/PedidosVendas.Tests.Unit.csproj tests/PedidosVendas.Tests.Unit/
COPY tests/PedidosVendas.Tests.Integration/PedidosVendas.Tests.Integration.csproj tests/PedidosVendas.Tests.Integration/
RUN dotnet restore PedidosVendas.slnx

COPY src/ src/
RUN dotnet publish src/PedidosVendas.API/PedidosVendas.API.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PedidosVendas.API.dll"]
