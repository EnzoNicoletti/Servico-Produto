using System.Net;
using System.Text.Json;
using PedidosVendas.Tests.Integration.Infrastructure;

namespace PedidosVendas.Tests.Integration;

/// <summary>
/// Autenticação JWT reaproveitada do Tenant/Identity: sem JWT válido → 401;
/// JWT válido sem tenant → 403 (middleware); claims completas ecoadas em /api/v1/me.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class AuthEndpointTests(IntegrationTestFixture fixture)
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid BranchId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private const string MePath = "/api/v1/me";

    [Fact]
    public async Task Requisicao_sem_token_retorna_401()
    {
        var response = await fixture.Client.GetAsync(MePath);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_valido_sem_tenant_id_retorna_403()
    {
        var token = TestJwt.CreateToken(UserId, tenantId: null);

        var response = await fixture.Client.SendAsync(TestJwt.ToAuthorizedRequest(MePath, token));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Token_com_tenant_invalido_rejeitado_com_403()
    {
        // Token assinado corretamente, mas tenant_id não é GUID — deve ser rejeitado.
        var token = TestJwt.CreateTokenWithInvalidTenant(UserId);

        var response = await fixture.Client.SendAsync(TestJwt.ToAuthorizedRequest(MePath, token));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Token_de_emissor_desconhecido_rejeitado_com_401()
    {
        var foreignToken = TestJwt.CreateForeignIssuerToken(UserId, TenantId);

        var response = await fixture.Client.SendAsync(
            TestJwt.ToAuthorizedRequest(MePath, foreignToken));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_com_claims_completas_extrai_valores_corretos_do_current_user_service()
    {
        var token = TestJwt.CreateToken(
            UserId,
            TenantId,
            BranchId,
            roles: ["PedidosRead", "PedidosWrite"]);

        var response = await fixture.Client.SendAsync(TestJwt.ToAuthorizedRequest(MePath, token));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal(UserId.ToString(), root.GetProperty("userId").GetString());
        Assert.Equal(TenantId.ToString(), root.GetProperty("tenantId").GetString());
        Assert.Equal(BranchId.ToString(), root.GetProperty("branchId").GetString());

        var roles = root.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToList();
        Assert.Equal(2, roles.Count);
        Assert.Contains("PedidosRead", roles);
        Assert.Contains("PedidosWrite", roles);
    }

    [Fact]
    public async Task Token_sem_branch_id_extrai_branch_nula_e_roles_vazias()
    {
        var token = TestJwt.CreateToken(UserId, TenantId, branchId: null, roles: null);

        var response = await fixture.Client.SendAsync(TestJwt.ToAuthorizedRequest(MePath, token));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal(JsonValueKind.Null, root.GetProperty("branchId").ValueKind);
        Assert.Empty(root.GetProperty("roles").EnumerateArray());
    }
}
