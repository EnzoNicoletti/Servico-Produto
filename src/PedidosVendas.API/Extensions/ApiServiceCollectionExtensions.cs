using PedidosVendas.API.Services;
using PedidosVendas.Application.Carrinho;
using PedidosVendas.Application.Common.Interfaces;
using PedidosVendas.Application.Cupons;
using PedidosVendas.Application.Pagamento;
using PedidosVendas.Infrastructure;

namespace PedidosVendas.API.Extensions;

/// <summary>
/// Composição dos serviços específicos da camada de Api.
/// </summary>
public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        // Mesma instância scoped exposta pelas duas abstrações (fonte única das claims).
        services.AddScoped<CurrentUserProvider>();
        services.AddScoped<ICurrentUserService>(sp => sp.GetRequiredService<CurrentUserProvider>());
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<CurrentUserProvider>());

        // Etapa 02: casos de uso de Cupom (serviços simples; sem MediatR nesta etapa —
        // ver MEMORIA_ETAPA_02). Repositório registrado na Infrastructure.
        services.AddScoped<ICupomService, CupomService>();
        services.AddScoped<ICupomEligibilityService, CupomEligibilityService>();

        // Etapa 03: ciclo de vida do carrinho.
        services.AddScoped<ICarrinhoService, CarrinhoService>();

        // Etapa 04: aplicação de cupom ao carrinho (sem decrementar quantidade).
        services.AddScoped<ICupomAplicacaoService, CupomAplicacaoService>();

        // Etapa 06: validação de parcelas.
        services.AddScoped<IParcelamentoService, ParcelamentoService>();

        services.AddControllers();
        services.AddPedidosVendasHealthChecks();

        return services;
    }
}
