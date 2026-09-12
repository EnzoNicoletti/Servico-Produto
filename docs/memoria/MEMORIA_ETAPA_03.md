# MEMORIA_ETAPA_03.md — Carrinho e Pedido

> Quarta entrada da memória persistente. Segue a estrutura de `MEMORIA_ETAPA_00.md`.
> Etapa executada conforme `docs/prompts/PROMPT_ETAPA_03.md` (+ roadmap 4.1, 4.8, 6.1, 8).

## Resumo
- Objetivo da etapa: implementar `Pedido` e `ProdutosPedido` cobrindo o ciclo de vida de
  carrinho (`Status = Carrinho`) até o momento anterior ao checkout, com gerenciamento de itens.
- Status geral: entidades + migration `AdicionarPedidoEProdutosPedido` aplicadas, endpoints
  de carrinho fim a fim, build sem erros, **45 testes unitários + 24 de integração passando**,
  compose validado. Resolvidas as pendências **#2, #4 e a parte de `ProdutosPedido` da #6**.

## Estado atual
- Novos arquivos — Domain: `Enums/StatusPedido.cs`, `Entities/Pedido.cs`,
  `Entities/ProdutosPedido.cs`, `Interfaces/IPedidoRepository.cs`,
  `Exceptions/PedidoInvalidoException.cs`.
- Application: `Carrinho/ICarrinhoService.cs` + `CarrinhoService.cs`,
  `Carrinho/ProdutoNaoEncontradoException.cs`, `ProdutoIndisponivelException.cs`.
- Infrastructure: `Persistence/Configurations/PedidoConfiguration.cs`,
  `ProdutosPedidoConfiguration.cs`, `Persistence/Repositories/PedidoRepository.cs`,
  `Persistence/Migrations/*_AdicionarPedidoEProdutosPedido.cs`; mock estendido
  (produto indisponível).
- API: `Contracts/Carrinho/AdicionarItemRequest.cs`, `AtualizarQuantidadeRequest.cs`,
  `CarrinhoResponse.cs`, `Controllers/CarrinhoController.cs` (`api/v1/carrinho`).
- Não implementado (fora de escopo): aplicação de cupom (Etapa 04), frete/pagamento/venda.

## Decisões técnicas
- **#2 resolvida — `Pedido` é a própria entidade-carrinho (confirmada).** Nenhuma entidade
  `Carrinho` criada: `Pedido` nasce com `Status = Carrinho` e `ProdutosPedido.IdPedido`
  substitui o `IdCarrinho` da especificação (decisão 4.1). `CarrinhoItem` segue como DTO de
  entrada do eligibility (Etapa 02); na Etapa 04 será alimentado via mapeamento
  `ProdutosPedido → CarrinhoItem` (incluindo o snapshot).
- **#4 resolvida — carrinho exige autenticação.** `IdCliente`/`TenantId` vêm do JWT
  (`ICurrentUserService.UserId` = claim `sub`, obrigatório; `ITenantContext`); controller
  com `[Authorize]` (padrão da plataforma: Identity e Estoque só expõem endpoints
  autenticados). Sem JWT → 401; sem carrinho/item no escopo → 404.
- **#6 (parte `ProdutosPedido`) resolvida — snapshot = `ValorUnitario`.** `ProdutosPedido`
  tem `Id, IdPedido, IdProduto, Quantidade, ValorUnitario` (decisão 4.8); preço gravado no
  momento da adição via `IProdutoServiceClient` (mock nesta etapa, D2 da Etapa 01).
  `UNIQUE (IdPedido, IdProduto)`: adicionar produto repetido soma quantidade, nunca duplica
  linha. Parte `ProdutosVenda` fica para a Etapa 07.
- **IdCliente/IdUnidade do contexto (verificado no código real):**
  `CurrentUserProvider.cs:65-69` — `UserId` (Guid, `sub`) e `BranchId` (`branch_id`, opcional).
  Carrinho criado com `IdUnidade = BranchId` do contexto (null quando ausente, cf. D1/Etapa 01);
  localizado por (TenantId, IdCliente, IdUnidade, Status) com índice dedicado.
- **Estoque sem endpoint real (reconfirmado):** só `GET /api/v1/me`; carrinho depende do mock
  (agora com produto disponível AAAA + indisponível BBBB + nulo p/ demais). Troca por HTTP
  real segue pendente (Etapa 03→ pendência 9 atualizada para "quando o Estoque expuser").
- **`StatusPedido` como enum int, sem tabela:** `Carrinho=1, AguardandoPagamento=2,
  VendaEfetuada=3, Cancelado=4` (0 inválido força valor explícito). A especificação não tem
  tabela de status; "seed" = valores do enum. Transições (cancelar/fechar) entram nas Etapas
  04-07; mutações fora de `Carrinho` já são bloqueadas no domínio.
- **`ValorTotal` recalculado no domínio** a cada mutação (soma dos subtotais, sem desconto —
  cupom na Etapa 04). `RowVersion→xmin` também em `Pedido`.
- **Reconciliação explícita reutilizada:** `PedidoRepository.SalvarAsync` faz Add/Remove
  explícitos de itens (mesma causa da Etapa 02: Guid preenchido + coleção rastreada ⇒
  UPDATE fantasma). POST usa Add em cascata; DELETE de pedido não existe nesta etapa.
- **Erros claros:** produto inexistente → 404 (`ProdutoNaoEncontradoException`), indisponível
  ou invariante violada → 400 (`ValidationProblem`); quantidade via DataAnnotations ≥ 1.
- **Resposta:** `CarrinhoResponse` com `Status` em string, itens com subtotal calculado.

## Banco de dados
- Migration `20260912165047_AdicionarPedidoEProdutosPedido`: tabelas `Pedido` (uuid,
  `timestamptz`, `Status int`, `numeric(18,2)`, `xmin`) e `ProdutosPedido` (uuid, `numeric`),
  FK com cascade, `CHECK (Quantidade > 0)`, índice `(TenantId, IdCliente, Status)`,
  `UNIQUE (IdPedido, IdProduto)`, índice `IdProduto`.
- Aplicada via fixture nos testes e via migrate automático (Development) no compose
  (`\dt` confirma as 5 tabelas + `__EFMigrationsHistory`).

## Testes
- Unitários (11 novos; 45 no total): `PedidoTests` (10 — criação, soma de total, merge de
  quantidade sem duplicar linha, recálculo em definir/remover, guards) + mock indisponível (1).
- Integração (6 novos; 24 no total): fluxo completo (GET cria vazio → POST 199.80 → POST
  soma 299.70/qtd 3 → PUT 99.90 → DELETE zera), 404 p/ produto inexistente e 400 p/
  indisponível, 404 em item ausente, isolamento usuário (B ganha carrinho próprio vazio,
  não altera item de A) e tenant, 401 sem JWT.
- Comando: `dotnet test PedidosVendas.slnx` → 45 + 24, 0 falhas; build 0 erros.
- Compose: rebuild, `up -d`, migrate automático, `/health/ready` 200, carrinho sem JWT 401,
  `identity-*` intactos.

## Problemas
- Nenhum bloqueador. Único ajuste em relação ao rascunho: `IdCliente`/`IdUnidade` do
  `ObterCarrinhoAbertoAsync` são `Guid?` (spec nullable) — a comparação com null no EF usa
  semântica de nulidade correta (`IS NULL`), sem tratamento especial.

## Decisões pendentes
1. ~~Estrutura real dos projetos~~ — resolvida na Etapa 01.
2. ~~`Pedido` é a própria entidade-carrinho~~ — **resolvida nesta etapa**.
3. Origem do limite de parcelas por Tenant — **Etapa 06**.
4. ~~Carrinho exige autenticado ou anônimo?~~ — **resolvida: exige autenticação**.
5. Mecanismo de mensageria para baixa de estoque — **Etapa 08**.
6. Parcial: `ProdutosPedido` resolvido (snapshot = `ValorUnitario`); **`ProdutosVenda`
   confirmar na Etapa 07**.
7. O que cachear — **Etapa 09** (+ reavaliar MediatR/FluentValidation se justificado).
8. Revisar D1 da Etapa 01 (branch ativa) quando a plataforma padronizar — mantida, com
   implementação marcada para a **Etapa 07 (Finalização da Compra)**: é ali que o checkout
   de fato acontece, portanto é ali que o endpoint de checkout passa a receber `IdUnidade`
   explícito e o backend valida contra `branch_access` do token (mesma lógica do
   `BranchAccessHandler` do Identity), rejeitando com **403** se a branch não estiver na
   lista. Até lá, o carrinho (Etapa 03) carrega `IdUnidade` do contexto sem validação.
9. Trocar o mock `IProdutoServiceClient` pelo client HTTP real — **quando o Estoque expuser
   Produto/Categoria** (Etapa 04 consome o contrato atual sem mudanças).

## Próximos passos
- Executar `docs/prompts/PROMPT_ETAPA_04.md` — Aplicação de Cupom ao Pedido (endpoint
  aplicar/remover cupom no carrinho via `ICupomEligibilityService` + `ProdutosPedido →
  CarrinhoItem`, `Pedido.IdCupom`, decremento com concorrência).
