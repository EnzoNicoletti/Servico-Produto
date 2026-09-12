using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using PedidosVendas.Application.Catalog;
using PedidosVendas.Application.Frete;
using PedidosVendas.Domain.Interfaces;
using PedidosVendas.Infrastructure.Catalog;
using PedidosVendas.Infrastructure.Frete;
using PedidosVendas.Infrastructure.HealthChecks;
using PedidosVendas.Infrastructure.Persistence;
using PedidosVendas.Infrastructure.Persistence.Repositories;

namespace PedidosVendas.Infrastructure;

/// <summary>
/// Ponto único de composição da infraestrutura (mesmo papel do
/// InfrastructureServiceCollectionExtensions do Servico-Estoque).
/// Divergência documentada (Etapa 01): o Estoque ainda não registra DbContext/EF
/// ("será registrado a partir da Etapa 3" deles); aqui o DbContext vazio já é
/// registrado porque o PROMPT_ETAPA_01 exige explicitamente. Quando o Estoque
/// adicionar o EF, os dois padrões serão realinhados.
/// </summary>
public static class DependencyInjection
{
    public const string DefaultConnectionName = "DefaultConnection";

    /// <summary>Tags usadas pelos health checks de dependências externas.</summary>
    public const string ReadyTag = "ready";

    /// <summary>Tags usadas pelos health checks de liveness (sem dependências).</summary>
    public const string LiveTag = "live";

    public static IServiceCollection AddPedidosVendasInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext vazio (sem entidades — Cupom/Pedido/Venda entram nas Etapas 02-07).
        // MigrationsAssembly = Infrastructure, como no Identity.
        // Registro LAZY (overload com IServiceProvider): a connection string é lida
        // da IConfiguration final, já com overrides de ambiente/testes — mesmo
        // motivo do NpgsqlDataSource lazy no Servico-Estoque.
        services.AddDbContext<PedidosVendasDbContext>((sp, options) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>()
                .GetConnectionString(DefaultConnectionName)
                ?? throw new InvalidOperationException(
                    $"A connection string '{DefaultConnectionName}' não está configurada.");

            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(PedidosVendasDbContext).Assembly.FullName));
        });

        // NpgsqlDataSource singleton com leitura LAZY da configuração final
        // (inclui overrides de ambiente/testes) — padrão do Servico-Estoque.
        services.AddSingleton(sp =>
            NpgsqlDataSource.Create(
                sp.GetRequiredService<IConfiguration>().GetConnectionString(DefaultConnectionName)
                ?? throw new InvalidOperationException(
                    $"A connection string '{DefaultConnectionName}' não está configurada.")));

        // Etapa 01: client mock. Etapa 03 troca por HTTP real sem mudar o contrato.
        services.AddScoped<IProdutoServiceClient, MockProdutoServiceClient>();

        // Etapa 02: persistência do agregado Cupom.
        services.AddScoped<ICupomRepository, CupomRepository>();

        // Etapa 03: persistência do agregado Pedido.
        services.AddScoped<IPedidoRepository, PedidoRepository>();

        // Etapa 05: frete plugável por configuração. "Mock" é o único provedor desta
        // etapa; um provedor real entra como nova classe + novo ramo, sem tocar chamadas.
        services.Configure<FreteOptions>(configuration.GetSection(FreteOptions.SectionName));
        var provedorFrete = configuration.GetSection(FreteOptions.SectionName).Get<FreteOptions>()?.Provedor ?? "Mock";
        if (!string.Equals(provedorFrete, "Mock", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Provedor de frete '{provedorFrete}' não implementado. Valores aceitos: Mock.");
        }

        services.AddScoped<IFreteCalculator, FreteCalculatorMock>();

        return services;
    }

    /// <summary>
    /// Registra os health checks:
    /// - "self" (tag live): liveness puro do processo;
    /// - "postgresql" (tag ready): conectividade real com o banco.
    /// </summary>
    public static IHealthChecksBuilder AddPedidosVendasHealthChecks(this IServiceCollection services)
    {
        return services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("API no ar."), tags: [LiveTag])
            .AddCheck<PostgresHealthCheck>(PostgresHealthCheck.Name, tags: [ReadyTag]);
    }
}
