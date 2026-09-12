# MEMORIA_ETAPA_01.md — Contexto e Esqueleto do Microsserviço PedidosVendas

> Segunda entrada da memória persistente. Segue a estrutura de `MEMORIA_ETAPA_00.md`.
> Etapa executada conforme `docs/prompts/PROMPT_ETAPA_01.md`.

## Resumo
- Objetivo da etapa: inspecionar os microsserviços existentes e criar o esqueleto do
  microsserviço `PedidosVendas` seguindo exatamente os mesmos padrões — sem implementar
  ainda as regras de negócio de Cupom/Pedido/Venda.
- Status geral: esqueleto criado, build da solution sem erros, testes de unidade (10) e
  de integração (11) passando; validação do `docker-compose` retomada após pausa.

## Estado atual
- Estrutura: `PedidosVendas.slnx` com `src/PedidosVendas.Domain`, `src/PedidosVendas.Application`,
  `src/PedidosVendas.Infrastructure`, `src/PedidosVendas.API` (namespaces `PedidosVendas.*`) e
  `tests/PedidosVendas.Tests.Unit` + `tests/PedidosVendas.Tests.Integration`.
- Projetos reutilizados como referência (somente leitura, nada alterado):
  `Desktop/identity-service` (Tenant/Identity) e `Desktop/Servico-Estoque` (Estoque).
- Funcionalidades implementadas: fundação only — `PedidosVendasDbContext` vazio (sem entidades),
  pipeline JWT de validação, `TenantResolutionMiddleware`, `CurrentUserProvider`
  (`ICurrentUserService` + `ITenantContext`), `GET /api/v1/me`, health checks
  (`/health/live`, `/health/ready`, `/health`), contrato `IProdutoServiceClient` + mock,
  `Dockerfile` + `docker-compose.yml` isolado.
- Pendências: ver "Decisões pendentes" (inclui 2 itens novos desta etapa).

## Decisões técnicas
- **(Etapa 01 — resolvida) Decisão pendente #1 do `MEMORIA_ETAPA_00.md` (estrutura real dos projetos):**
  o repositório `Servico-Produto` continha só `docs/`; os padrões reais foram inspecionados nas
  pastas irmãs `identity-service` (`Identity.Domain/Application/Infrastructure/API`, namespaces
  `Identity.*`) e `Servico-Estoque` (`Estoque.Domain/Application/Infrastructure/Api/Workers`,
  namespaces `Estoque.*`). O esqueleto `PedidosVendas` replica esses padrões.
- **Padrões reais descobertos (as próximas etapas dependem destas informações):**
  - Namespaces: `PedidosVendas.Application.Common.Identity` (`IdentityClaims`, `CurrentUser`,
    `CurrentUserFactory`, `InvalidIdentityClaimsException`), `...Common.Interfaces`
    (`ITenantContext`, `ICurrentUserService`), `PedidosVendas.API.Authentication`,
    `...Middleware`, `...Services`, `...Extensions`, `...Contracts`, `...Controllers`.
  - Tenant resolvido **exclusivamente** das claims do JWT: `sub` e `tenant_id` obrigatórios
    (GUID), `branch_id` opcional, papéis em `role`/`roles` sem duplicidade
    (`CurrentUserFactory`; `MapInboundClaims = false`, `NameClaimType = sub`,
    `RoleClaimType = role`). Requisição autenticada sem `tenant_id` válido → **403**
    (`TenantResolutionMiddleware`, corpo `application/problem+json`); sem token → **401**.
  - Sem JWT válido o pipeline nem chega ao middleware (401 do JwtBearer).
  - Resposta de API: controllers finos + `MeResponse(UserId, TenantId, BranchId, Roles)`
    em camelCase; erros de tenant em `application/problem+json` (RFC 9110 §6.5.3);
    logging via Serilog (`UseSerilogRequestLogging`, `Enrich.FromLogContext`) +
    `ForwardedHeaders` (X-Forwarded-For/Proto). Ordem do pipeline: autenticação →
    resolução de tenant → autorização.
  - Banco: connection string `DefaultConnection` lida de forma **lazy** da
    `IConfiguration` final (overrides de ambiente/testes já aplicados);
    `NpgsqlDataSource` singleton + `PostgresHealthCheck` (`SELECT 1`) com tags
    `live`/`ready`; `DbContext` com `MigrationsAssembly = Infrastructure`.
  - Testes no padrão Estoque: fixture compartilhada (`IAsyncLifetime` + 1 container
    `postgres:16-alpine` + 1 `WebApplicationFactory`), `TestJwt` (chave simétrica,
    `JsonWebTokenHandler`), coleção xUnit `Integration`; unitários puros para a factory.
- **D1 — Branch ativa: não existe mecanismo de branch única em requisições de API (decisão adotada).**
  Evidência: o JWT do Identity carrega só `branch_access` (lista; `AuthClaimTypes.cs:9`,
  `JwtTokenGenerator.cs:46-49`) — não há claim `branch_id`/`UnidadeId` emitida, nem header
  (`X-Branch`), nem query param em nenhum dos dois serviços; o Identity autoriza pela branch
  da rota (`RouteValues["id"]`, `BranchAccessHandler.cs:35-56`) e o Estoque lê `branch_id`
  opcional só do JWT (sempre nulo com tokens reais). **Decisão:** `Pedido.IdUnidade` fica
  **nullable**; o frontend envia a branch explicitamente no checkout, e o backend valida que
  ela consta em `branch_access` do token (mesma lógica do `BranchAccessHandler`). Nenhum filtro
  global por branch é imposto nesta etapa.
- **D2 — Estoque sem endpoints de Produto/Categoria (decisão adotada).** Evidência: o único
  controller é `GET /api/v1/me` (`MeController.cs`), sem `DbContext`/repositories
  (`InfrastructureServiceCollectionExtensions.cs:11`: "será registrado a partir da Etapa 3").
  **Decisão:** criado o contrato `IProdutoServiceClient` com implementação **mock** na
  Infrastructure (`Infrastructure/Catalog/MockProdutoServiceClient.cs`), para não bloquear o
  desenvolvimento. A implementação HTTP real entra na Etapa 03 reaproveitando o mesmo contrato
  (regra fundamental: nenhum padrão novo).
- **D3 — Validação JWT em modo dual (decisão adotada).** O Identity valida HS256 direto
  (`Jwt__Secret/Issuer/Audience`, `Program.cs:174-192`); o Estoque, como resource server, usa
  seção `Authentication:Jwt` com dois modos mutuamente exclusivos (`Authority` OIDC/JWKS em
  produção, `SigningKey`+`ValidIssuer` HS256 em dev/testes). **Decisão:** `PedidosVendas` segue
  o padrão Estoque (é o padrão de quem **consome** token, não de quem emite). Nenhuma emissão
  de token foi implementada (não reimplementar autenticação).
- **Docker isolado (sem rede compartilhada).** Ambos os serviços reais usam a rede default do
  próprio compose (sem `networks:` explícita e sem gateway compartilhado); `PedidosVendas`
  faz o mesmo (projeto `pedidosvendas`, rede default). Portas sem colisão: host `5433`
  (identity usa `5432`, estoque usa `15432`) e API em `8081:8080`.
- **Nenhuma entidade de negócio criada** (`Cupom`, `Pedido`, `Venda`, `FormaPagto` ficam para
  as Etapas 02-07), conforme o escopo.

## Banco de dados
- Nenhuma migration criada (DbContext vazio, sem `DbSet` — por exigência do prompt desta etapa).
- `PedidosVendasDbContext` registrado com Npgsql, `MigrationsAssembly = Infrastructure`
  (como no Identity). Divergência consciente: o Estoque ainda não tem EF ("Etapa 3" deles);
  quando o Estoque adicionar o EF, os padrões serão realinhados.
- `PostgresHealthCheck` (`SELECT 1` via `NpgsqlDataSource`) cobre a conectividade; teste de
  integração prova `CanConnectAsync()` + `SELECT 1` contra `postgres:16-alpine`
  (mesma imagem do compose).

## Testes
- Unitários (10, passando): `CurrentUserFactoryTests` (8 — claims completas, branch nula,
  roles `role`+`roles` sem duplicar, não-autenticado, `sub`/`tenant_id` ausentes, tenant e
  branch malformados) + `MockProdutoServiceClientTests` (2 — id conhecido/nulo).
- Integração (11, passando, Testcontainers `postgres:16-alpine`): `HealthEndpointTests` (3 —
  `/health`, `/health/ready`, `/health/live` = 200), `AuthEndpointTests` (6 — sem token 401,
  sem tenant 403, tenant inválido 403, emissor desconhecido 401, claims completas 200,
  sem branch 200 com `branchId` nulo), `DatabaseConnectionTests` (2 — `CanConnectAsync`,
  `SELECT 1` via `NpgsqlDataSource`).
- Comando: `dotnet build PedidosVendas.slnx` (0 erros; só aviso NU1903 do pacote de template
  `Microsoft.OpenApi`) + `dotnet test` nos dois projetos.
- Validação do compose (build da imagem + `up` + `/health` + serviços existentes intactos):
  **concluída após a retomada.** `docker compose build` exigiu 2 correções fiéis ao Estoque:
  copiar os `*.csproj` de testes para o `restore` do `.slnx` e adicionar `.dockerignore`
  (`**/bin/`, `**/obj/` — sem ele, os `project.assets.json` do Windows invalidam o publish
  no Linux; o `.dockerignore` do Estoque documenta a mesma falha). `docker compose up -d`:
  `pedidosvendas-db` healthy (`5433:5432`), `pedidosvendas-api` (`8081:8080`);
  `GET /health/live`, `/health/ready` e `/health` → **200 Healthy**;
  `GET /api/v1/me` sem JWT → **401**; `identity-api/db/mailhog` intactos (up, sem restart).

## Problemas
- O repositório `Servico-Produto` continha só `docs/` (nenhum código de Tenant/Estoque);
  os padrões reais estavam nas pastas irmãs — resolvido por inspeção somente-leitura.
- Docker Desktop estava parado (Testcontainers falhava com `DockerUnavailableException`);
  daemon iniciado, containers existentes (`identity-api`, `identity-db`) preservados.
- Codificação pausada 2 vezes por solicitação (primeiro para resumir achados, depois para
  aprovar o texto da memória); build da imagem Docker ficou interrompido e será retomado.

## Decisões pendentes
1. ~~Estrutura real dos projetos~~ — **resolvida nesta etapa** (ver Decisões técnicas).
2. Confirmação de que `Pedido` é a própria entidade-carrinho — **Etapa 03** (inalterada).
3. Origem do limite de parcelas por Tenant — **Etapa 06** (inalterada).
4. Carrinho exige usuário autenticado ou permite anônimo? — **Etapa 03** (inalterada).
5. Mecanismo de mensageria para baixa de estoque — **Etapa 08** (inalterada).
6. Campos exatos do snapshot `ProdutosVenda`/`ProdutosPedido` — **Etapa 03/07** (inalterada).
7. O que cachear — **Etapa 09** (inalterada).
8. **(novo) Revisar D1 quando a plataforma padronizar branch ativa.** Se Identity/Estoque
   adotarem claim/header padrão de branch única (ex.: `branch_id` emitido ou `X-Branch-Id`),
   `Pedido.IdUnidade` e o `CurrentUser` (que hoje não carrega a lista `branch_access`) devem
   ser realinhados; a validação do checkout passa a usar o mecanismo oficial.
9. **(novo) Trocar o mock `IProdutoServiceClient` pelo client HTTP real — Etapa 03**, quando o
   Estoque expuser endpoints de Produto/Categoria; inclui definir timeout/retry e fallback
   quando o Estoque estiver fora do ar.

## Próximos passos
- Concluir a validação Docker desta etapa (build + `compose up` + `/health/live|ready` 200 +
  confirmar `identity-*` intactos) e registrar o resultado em "Testes" (adendo).
- Executar `docs/prompts/PROMPT_ETAPA_02.md` — Domínio de Cupom (entidades `Cupom`/`CupomProduto`,
  migrations, validação "exatamente um desconto", prioridade produto > categoria > global,
  testes unitários).
