# MEMORIA_ETAPA_02.md — Domínio de Cupom

> Terceira entrada da memória persistente. Segue a estrutura de `MEMORIA_ETAPA_00.md`.
> Etapa executada conforme `docs/prompts/PROMPT_ETAPA_02.md` (+ roadmap 4.1–4.4, 4.9).

## Resumo
- Objetivo da etapa: implementar o domínio de Cupom (`Cupom`, `CupomProduto`, validações,
  elegibilidade/prioridade) e o CRUD via API — **sem integrar com Pedido** (Etapa 04).
- Status geral: entidades + migration `AdicionarCupomECupomProduto` aplicadas, CRUD
  tenant-scoped funcionando, build sem erros, **34 testes unitários + 18 de integração
  passando**, compose validado (migrate automático em Development, tabelas criadas).

## Estado atual
- Novos arquivos — Domain: `Entities/Cupom.cs`, `Entities/CupomProduto.cs`,
  `Interfaces/ICupomRepository.cs`, `Exceptions/CupomInvalidoException.cs`.
- Application: `Cupons/CarrinhoItem.cs`, `Cupons/CupomDados.cs`,
  `Cupons/ICupomEligibilityService.cs` + `CupomEligibilityService.cs`,
  `Cupons/ICupomService.cs` + `CupomService.cs`.
- Infrastructure: `Persistence/Configurations/CupomConfiguration.cs`,
  `CupomProdutoConfiguration.cs`, `Persistence/Repositories/CupomRepository.cs`,
  `Persistence/Migrations/*_AdicionarCupomECupomProduto.cs`; DbContext com os 2 DbSets.
- API: `Contracts/Cupons/CupomRequest.cs`, `CupomResponse.cs`, `Contracts/PagedResult.cs`,
  `Controllers/CuponsController.cs` (`api/v1/cupons`); `Program.cs` com migrate automático
  só em Development (padrão do Identity).
- Não implementado (fora de escopo): aplicação do cupom a Pedido real (Etapa 04),
  seeds, endpoints de frete/pagamento/venda.

## Decisões técnicas
- **Ids de Produto/Categoria: Guid sem FK real (verificado).** O `Estoque.Domain` está vazio
  (nenhuma entidade `Produto`/`Categoria` em todo o `src` do Estoque) — não há formato a
  compatibilizar além da convenção da plataforma (todos os Ids do Identity são Guid).
  `IdCategoria`/`IdProduto` são Guids com índice, sem constraint cross-service; o próprio
  `ProdutoDto.Id` (Etapa 01) já era Guid.
- **`IdCategora` → `IdCategoria`, `isCupomProduto` → `IsCupomProduto`** (typo + convenção C#).
- **Exclusividade de desconto em 2 níveis:** `Cupom.Criar/Atualizar` exige exatamente um entre
  `ValorDesconto`/`PercDesconto` > 0 (roadmap 4.3) + `CHECK (CK_Cupom_DescontoExclusivo)` no
  banco; `CHECK (Quantidade >= 0)` (roadmap 4.9).
- **Coerência flag↔vínculos (adotada):** `isCupomProduto=true` exige ≥ 1 link em `CupomProduto`;
  `false` proíbe links. O flag sozinho não decide nada — a prioridade (4.4) lê os vínculos reais.
- **Prioridade determinística:** links (`CupomProduto`) > `IdCategoria` > global; implementada
  em `CupomEligibilityService.ObterItensElegiveis`, pura e sem Pedido.
- **Aplicabilidade (`PodeAplicar`):** tenant igual + `agoraUtc.Date <= DataValidade.Date`
  (validade é data, não instante) + `Quantidade > 0` + `subtotal >= ValorMinimoCompra`.
  Criação rejeita `DataValidade` passada; o `agoraUtc` parametrizado permite testar expiração
  sem entidades vencidas.
- **Desconto fixo limitado ao subtotal elegível** (`Math.Min`) — nunca gera valor negativo;
  percentual incide só sobre os elegíveis.
- **Concorrência otimista:** `uint RowVersion` → coluna `xmin` (`IsRowVersion()`); consumo via
  `RegistrarUso()` (lança se esgotado); teste de 2 consumos simultâneos da última unidade
  prova que só um vence (`DbUpdateConcurrencyException`).
- **Achado EF (registrado por ser load-bearing): Guid preenchido no ctor + coleção em
  entidade rastreada ⇒ UPDATE fantasma.** Diagnóstico (SQL log + `ChangeTracker`: filho novo
  com estado `Modified`): o EF lê Guid não-default descoberto na coleção como "existente".
  **Correção:** `CupomRepository.AtualizarAsync` reconcilia vínculos explicitamente
  (`Add` novos, `Remove` removidos) em vez de confiar em Clear+SaveChanges. POST (Add em
  cascata) e DELETE (Remove com cascade) não são afetados.
- **Sem MediatR/FluentValidation nesta etapa (adotada):** o Identity os usa, mas o Estoque
  (microsserviço-recurso análogo) não tem nenhum dos dois e o próprio prompt prescreve
  `ICupomEligibilityService` como serviço simples. Validação de entrada: DataAnnotations no
  request + invariantes no domínio (`CupomInvalidoException` → 400 `ValidationProblem`).
  Reavaliar na Etapa 09 se o volume de casos de uso justificar.
- **Inativar = DELETE hard (adotada):** a especificação não tem flag `Ativo` e o prompt lista
  só os campos dela + `TenantId`/`RowVersion`; delete com cascade em `CupomProduto`.
- **Índices:** `(TenantId)`, `(TenantId, DataValidade)` (exigidos), mais `UNIQUE
  (IdCupom, IdProduto)` (sem vínculo duplicado) e índice em `IdProduto` (lookup Etapa 04).
- **Resposta paginada:** envelope `PagedResult<Total, Pagina, TamanhoPagina, Itens>` (as APIs
  de referência retornam DTOs diretos; lista paginada exige envelope).
- **Tenant nunca do payload** (DTOs sequer têm o campo); cross-tenant retorna **404** (não
  403) para não vazar existência; sem JWT → 401 pelo pipeline da Etapa 01.

## Banco de dados
- Migration `20260912162155_AdicionarCupomECupomProduto` (assembly Infrastructure):
  tabelas `Cupom` (uuid, `varchar(200)`, `numeric(18,2)`/`(5,2)`, `timestamptz`, `xmin xid`
  rowversion) e `CupomProduto` (uuid), PKs, 2 CHECKs, FK com cascade, 4 índices.
- Aplicada com sucesso: via `MigrateAsync` na fixture de testes e via migrate automático
  em Development no compose (`\dt` confirma `Cupom`, `CupomProduto`, `__EFMigrationsHistory`).
- Nenhuma seed nesta etapa.

## Testes
- Unitários (24 novos; 34 no total): `CupomTests` (10 — criação válida, exclusividade ambos/
  nenhum/negativos, tenant/descrição, validade passada, quantidade negativa, coerência
  flag↔vínculos, `RegistrarUso`, `Atualizar`+revalidação) e `CupomEligibilityServiceTests`
  (14 — global/categoria/produto, prioridade produto > categoria com ambos presentes,
  carrinho sem vinculados, cupom sem vínculos = global, mínimo acima/igual/abaixo,
  tenant/expirado/esgotado, fixo com teto, percentual só-elegíveis, vazio = 0).
- Integração (7 novos; 18 no total): CRUD 201→200→200→204→404 (inclui PUT que troca vínculos
  e deleção de órfãos), isolamento Tenant (GET/PUT/DELETE cross-tenant = 404, lista não vaza),
  paginação (total + fatias), 400 para dois descontos e validade passada, CHECK real via SQL
  direto (`PostgresException` 23514 — SQL cru não passa pelo wrap do EF), concorrência da
  última unidade, POST com vínculos persistido.
- Comando: `dotnet test PedidosVendas.slnx` → 34 + 18, 0 falhas; build 0 erros.
- Compose: rebuild (`.dockerignore` da Etapa 01 segue válido), `up -d`, migrate automático
  aplicado, `/health/ready` 200, `/api/v1/cupons` sem JWT 401, `identity-*` intactos.
  (Aviso benigno `libgssapi_krb5` do Npgsql no log; `dotnet ef` em design-time exibe o
  trace `HostAborted` padrão — ambos sem efeito.)

## Problemas
- UPDATE fantasma em vínculos (detalhado acima): 2 rodadas de falha até o diagnóstico
  (SQL + tracker) apontar a causa; corrigido com reconciliação explícita; arquivo
  `DiagnosticoTemporario.cs` e hack de log `EFLOG` removidos após o diagnóstico.
- Teste do CHECK esperava `DbUpdateException`, mas SQL direto lança `PostgresException`
  pura — asserção corrigida (prova ainda mais direta da constraint).
- `Program.cs` precisou de `using Microsoft.EntityFrameworkCore;` para `MigrateAsync`.
- `MigrateAsync` no `Program` (Development) exige o banco dev no ar para futuros
  `dotnet ef migrations add` — mesmo tradeoff do Identity.

## Decisões pendentes
1. ~~Estrutura real dos projetos~~ — resolvida na Etapa 01.
2. Confirmação de que `Pedido` é a própria entidade-carrinho — **Etapa 03**.
3. Origem do limite de parcelas por Tenant — **Etapa 06**.
4. Carrinho exige usuário autenticado ou permite anônimo? — **Etapa 03**.
5. Mecanismo de mensageria para baixa de estoque — **Etapa 08**.
6. Campos exatos do snapshot `ProdutosVenda`/`ProdutosPedido` — **Etapa 03/07**.
7. O que cachear — **Etapa 09** (+ reavaliar MediatR/FluentValidation se justificado).
8. Revisar D1 da Etapa 01 (branch ativa) quando a plataforma padronizar — mantida.
9. Trocar o mock `IProdutoServiceClient` pelo client HTTP real — **Etapa 03** (quando o
   Estoque expuser Produto/Categoria; `CarrinhoItem`/`ICupomEligibilityService` já estão
   prontos para a integração da Etapa 04).

## Próximos passos
- Executar `docs/prompts/PROMPT_ETAPA_03.md` — Carrinho e Pedido (entidades
  `Pedido`/`ProdutosPedido` com `ValorUnitario` snapshot, endpoints de carrinho,
  integração de leitura com Estoque via `IProdutoServiceClient`, `IdUnidade` nullable
  conforme D1).
