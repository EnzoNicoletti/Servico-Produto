using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace PedidosVendas.Infrastructure.HealthChecks;

/// <summary>
/// Verifica a conectividade real com o PostgreSQL executando <c>SELECT 1</c>
/// através do pool do <see cref="NpgsqlDataSource"/> (mesmo padrão do Servico-Estoque).
/// </summary>
public sealed class PostgresHealthCheck(NpgsqlDataSource dataSource) : IHealthCheck
{
    public const string Name = "postgresql";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = dataSource.CreateCommand("SELECT 1");
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("PostgreSQL acessível.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "Falha ao conectar no PostgreSQL.",
                exception,
                data: null);
        }
    }
}
