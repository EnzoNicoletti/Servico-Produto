# Microsserviço PedidosVendas — Análise e Roadmap de Desenvolvimento

> Documento de planejamento (Etapa 1 — Análise). Nenhum código foi escrito nesta etapa.
> Este documento é a base de contexto que o OpenCode deve ler antes do PROMPT_ETAPA_01.

---

## 1. Análise dos Requisitos do Professor

A especificação define 6 entidades (`Cupom`, `CupomProduto`, `Pedido`, `ProdutosPedido`, `Venda`,
`FormaPagto`) e um conjunto de regras de negócio para cupons, checkout e conversão pedido→venda.
Os requisitos funcionais abaixo foram **preservados integralmente**, conforme obrigatório:

- Cupom global, por categoria ou por produto, com desconto em valor ou percentual, valor mínimo de
  compra, validade e quantidade.
- Prioridade determinística: produto específico (`CupomProduto`) > categoria (`IdCategoria`) > global.
- Pedido com cliente opcional, cupom opcional, unidade (Branch) opcional, status inteiro e valor total.
- `ProdutosPedido` como itens do carrinho/pedido.
- Checkout: CEP → cálculo de frete → forma de pagamento → parcelas (1 para tudo exceto cartão de
  crédito, que respeita limite do Tenant/Loja ou, na ausência, o limite da `FormaPagto`).
- `Venda` como cópia/consolidação do pedido fechado, com `ProdutosVenda` como snapshot de
  `ProdutosPedido`.
- Seeds de `FormaPagto` e de status do carrinho/pedido.

Nenhum campo ou entidade da especificação original foi removido. Onde há ambiguidade ou nome que
conflita com a arquitetura existente, isso é tratado como **decisão de implementação documentada**,
nunca como alteração silenciosa (ver seção 4).

## 2. Análise dos Microsserviços Existentes (a ser confirmada em runtime)

Este documento assume uma estrutura típica de Clean Architecture/DDD multi-tenant (Domain /
Application / Infrastructure / API, PostgreSQL, ASP.NET Identity, JWT com Claims de `TenantId`,
`BranchId`/`UnidadeId`, `UserId`, `Roles`). **O OpenCode não deve confiar nesta suposição** — a
Etapa 01 exige inspeção real dos projetos `Tenant/Identity` e `Estoque` para confirmar:

- Nome exato da solution e dos projetos (namespaces).
- Como o `TenantId` chega ao contexto (middleware, `ITenantContext`, Claims).
- Como `BranchId`/`UnidadeId` é resolvido.
- Formato de resposta de API (Result pattern, ProblemDetails, envelope padrão).
- Padrão de DbContext, migrations, connection string, multi-schema ou `TenantId` em toda tabela.
- Padrão de autenticação entre serviços (JWT propagado, client HTTP, API Gateway, Service Discovery).
- Convenções de nomenclatura de projeto (ex.: `Empresa.Estoque.Domain`, etc.) para replicar em
  `Empresa.PedidosVendas.*`.
- Estrutura de Docker Compose existente (rede, nomes de serviço, variáveis de ambiente, healthchecks).

Isso é o objetivo explícito do PROMPT_ETAPA_01.

## 3. Dependências entre Microsserviços

| Dependência | Direção | Tipo | Observação |
|---|---|---|---|
| PedidosVendas → Tenant/Identity | síncrona | JWT/Claims | Tenant, Branch e Usuário logado vêm do token, nunca do body |
| PedidosVendas → Estoque | síncrona (checkout) | REST API | Validar existência/preço/disponibilidade do produto |
| PedidosVendas → Estoque | assíncrona (pós-venda) | evento/mensageria (proposto) | Baixa de estoque após venda confirmada |
| Estoque → PedidosVendas | nenhuma | — | Estoque não deve depender deste serviço |

## 4. Ambiguidades Identificadas e Decisões Recomendadas

### 4.1 Carrinho vs Pedido vs Venda
A especificação usa `IdCarrinho` como FK em `ProdutosPedido`, mas não existe uma tabela `Carrinho`
separada — apenas `Pedido`. **Conflito**: campo referencia uma entidade que não existe no modelo.

- **Alternativas:**
  1. Criar uma entidade `Carrinho` separada, com `Pedido` só nascendo no checkout.
  2. Tratar `Pedido` como carrinho desde a criação (status inicial = "Carrinho/Aberto"), e interpretar
     `IdCarrinho` em `ProdutosPedido` como um erro de nomenclatura para `IdPedido`.
- **Recomendação:** opção 2. O comportamento exigido pelo professor (usuário monta o carrinho,
  depois finaliza e ele vira pedido/venda) é preservado; só a nomenclatura do campo é corrigida
  (`IdCarrinho` → `IdPedido`) porque `Pedido` já cumpre o papel de carrinho enquanto `Status =
  Carrinho`. Isso evita duplicar entidade e estado sem necessidade.
- **Registrado como decisão técnica.** Confirmação final ocorre na Etapa 03.

### 4.2 `Cupom.IdCategora`
Erro de digitação evidente para `IdCategoria`. Será corrigido para `IdCategoria` no código,
preservando o significado original (FK para Categoria do serviço de Estoque).

### 4.3 `Cupom.ValorDesconto` e `PercDesconto` simultâneos
A especificação não define exclusividade. **Decisão recomendada:** os dois campos coexistem no
schema (conforme pedido), mas uma regra de validação de domínio exige que **exatamente um dos dois**
esteja preenchido (> 0) por cupom, evitando ambiguidade no cálculo do desconto. Isso preserva o
schema exigido e só adiciona uma constraint de negócio.

### 4.4 Prioridade de cupom (produto x categoria x global)
Regra determinística adotada, na ordem:
1. Se existir registro em `CupomProduto` para o produto do carrinho → desconto aplicado apenas
   àquele produto.
2. Senão, se `Cupom.IdCategoria` estiver preenchido → desconto aplicado a todos os produtos do
   carrinho daquela categoria.
3. Senão (cupom sem categoria e sem produtos vinculados) → desconto aplicado a todos os itens do
   carrinho.

Um único cupom é aplicado por pedido (o campo `Pedido.IdCupom` é singular), então não há conflito de
múltiplos cupons — a prioridade acima resolve apenas **a quais itens** o desconto de um único cupom
se aplica.

### 4.5 `FormaPagto` e limite de parcelas por Tenant
A especificação menciona "limite máximo definido no cadastro da Loja do Tenant" como override do
limite padrão de `FormaPagto.QtdMaximaParcelas`. **Ambiguidade:** não sabemos se o serviço de
Tenant/Identity já possui esse campo hoje. **Decisão pendente até Etapa 01** (inspeção real):
- Se existir campo equivalente no Tenant → consumir via API do serviço de Tenant.
- Se não existir → propor a criação (fora do escopo deste serviço, é dado de configuração do Tenant)
  ou, como alternativa de curto prazo, criar uma tabela local `TenantFormaPagtoConfig` neste
  microsserviço só com o override, mantendo `FormaPagto.QtdMaximaParcelas` como fallback padrão —
  exatamente como o professor descreveu ("caso não tenha, considerar a quantidade máxima cadastrada
  na tabela FormaPagto").
- **Recomendação:** iniciar com o fallback local (`TenantFormaPagtoConfig` opcional) e documentar
  como item de integração futura com o Tenant service, sem bloquear o desenvolvimento.

### 4.6 `Venda.NrPedido`
Descrito como "futuro número da nota fiscal". Será mantido como campo nullable (`string?`) sem
lógica de geração nesta fase — reservado para integração fiscal futura, conforme o professor pediu
explicitamente para não removê-lo.

### 4.7 Snapshot de produto em `ProdutosVenda`
A especificação não lista os campos do snapshot. **Decisão recomendada:** persistir, além do
`IdProduto` e quantidade, os dados imutáveis no momento da venda: nome do produto, SKU/código,
preço unitário praticado, e percentual/valor de desconto do item — necessários para reconstrução
histórica sem depender do serviço de Estoque. Justificativa: requisito explícito do professor
("não depender exclusivamente dos dados atuais do Produto").

### 4.8 Preço do produto no pedido
A tabela `ProdutosPedido` do professor não tem campo de preço unitário. Para calcular
`Pedido.ValorTotal` é necessário conhecer o preço no momento da adição ao carrinho.
**Recomendação (melhoria justificada):** adicionar `ValorUnitario` (snapshot no momento da adição
ao carrinho, obtido via API do Estoque) em `ProdutosPedido`, evitando recomputar preços voláteis e
protegendo contra alteração de preço do produto entre a montagem do carrinho e o checkout. Registrado
como melhoria técnica, não descaracteriza nenhum campo pedido pelo professor — apenas adiciona um
campo necessário para o `ValorTotal` funcionar.

### 4.9 Concorrência no consumo de cupom
`Cupom.Quantidade` pode ser decrementada concorrentemente por múltiplos checkouts simultâneos.
**Recomendação:** controle otimista (`xmin`/`RowVersion` do PostgreSQL) + constraint
`CHECK (Quantidade >= 0)` + retry curto em caso de conflito, evitando lock pessimista de longa
duração sob alta concorrência.

## 5. Modelo de Domínio (visão lógica)

```
Tenant (existente) 1---N Pedido
Branch/Unidade (existente) 1---N Pedido (opcional)
ApplicationUser (existente) 1---N Pedido (Cliente, opcional)
Cupom N---1 Tenant
Cupom 1---N CupomProduto N---1 Produto (Estoque, referência)
Cupom 0..1---N Pedido
Pedido 1---N ProdutosPedido N---1 Produto (Estoque, referência)
Pedido 0..1---1 Venda
Venda 1---N ProdutosVenda (snapshot)
Venda N---1 FormaPagto
FormaPagto 1---N Venda
StatusPedido (enum/seed) 1---N Pedido
```

Todas as entidades de escrita deste serviço (`Cupom`, `CupomProduto`, `Pedido`, `ProdutosPedido`,
`Venda`, `ProdutosVenda`) recebem `TenantId` (obtido do Claim, nunca do payload) e índice composto
`(TenantId, Id)` / índices únicos por Tenant onde aplicável. `FormaPagto` é tratada como catálogo
compartilhado (seed global), com possível override por Tenant conforme 4.5.

## 6. Fluxos de Negócio

### 6.1 Fluxo do Carrinho
1. Usuário autenticado (ou anônimo, se permitido — **decisão pendente**, ver seção 9) inicia um
   `Pedido` com `Status = Carrinho`, `IdUnidade` da Branch atual.
2. Adiciona/remove/altera quantidade em `ProdutosPedido`, validando disponibilidade via Estoque e
   gravando `ValorUnitario` snapshot.
3. `Pedido.ValorTotal` é recalculado a cada alteração (soma dos itens, sem desconto ainda).

### 6.2 Fluxo de Aplicação de Cupom
1. Usuário informa código do cupom (ou seleção) → validar Tenant, validade, quantidade > 0, valor
   mínimo de compra atingido pelo subtotal.
2. Aplicar regra de prioridade (4.4) para determinar itens elegíveis.
3. Calcular desconto (valor fixo ou percentual) só sobre os itens elegíveis.
4. Gravar `Pedido.IdCupom`; desconto não é persistido em campo próprio de `Pedido` — recalculado no
   checkout/venda (ver 6.4) para evitar inconsistência caso o carrinho mude depois.

### 6.3 Fluxo de Frete
1. No checkout, usuário informa CEP.
2. `IFreteCalculator.Calcular(cep, itens)` retorna valor — implementação inicial mockada/local,
   preparada para plugar provedor externo depois.

### 6.4 Fluxo de Pagamento e Parcelas
1. Usuário escolhe `FormaPagto`.
2. Se PIX/transferência/depósito/débito → parcelas fixas em 1 (backend ignora/rejeita valor
   diferente vindo do cliente).
3. Se crédito → validar parcelas ≤ limite (Tenant override, senão `FormaPagto.QtdMaximaParcelas`).

### 6.5 Fluxo de Finalização (Pedido → Venda)
Operação transacional única:
1. Revalidar pedido (itens, estoque, preços não mudaram de forma crítica — ou re-snapshot).
2. Revalidar cupom (ainda válido/disponível) e recalcular desconto.
3. Recalcular frete.
4. Validar forma de pagamento e parcelas.
5. Criar `Venda` (ValorBruto, ValorDesconto, ValorLiquidoPedido, ValorFinal = Líquido + Frete).
6. Copiar `ProdutosPedido` → `ProdutosVenda` (snapshot, ver 4.7).
7. Decrementar `Cupom.Quantidade` (se houver cupom) com controle de concorrência.
8. Atualizar `Pedido.Status` → `VendaEfetuada`, gravar `DataFechamento`.
9. Disparar evento/chamada para o serviço de Estoque efetivar a baixa (ver seção 7).
10. Commit único (`TransactionScope`/`DbContext` transaction) — se qualquer passo falhar, rollback
    completo, pedido permanece em `Status = Carrinho` ou `AguardandoPagamento`.

## 7. Estratégia de Integração com Estoque

- **Referências (não duplicar):** `Produto`, `Categoria`, `Fornecedor` — sempre por `Id` (Guid),
  nunca replicados como entidades próprias neste serviço.
- **Snapshots (duplicar intencionalmente):** nome, preço unitário e SKU do produto no momento da
  adição ao carrinho (`ProdutosPedido`) e no momento da venda (`ProdutosVenda`).
- **Síncrono (REST):** validação de existência/preço/disponibilidade ao adicionar item e no checkout.
- **Assíncrono (evento, proposto):** baixa efetiva de estoque após venda confirmada, para não
  acoplar a transação de venda à disponibilidade momentânea do serviço de Estoque. Modelo
  eventual-consistency com compensação (se a baixa falhar, gerar alerta/retry — fila).
- Confirmar em runtime (Etapa 01) se já existe infraestrutura de mensageria no projeto (RabbitMQ,
  Azure Service Bus, etc.) antes de propor uma nova.

## 8. Estratégia Multi-Tenant / Multi-Branch

- `TenantId` sempre obtido de Claims/contexto autenticado (nunca do body/query).
- Todas as queries filtram por `TenantId` — reutilizar `IQueryFilter`/Global Query Filter do EF Core
  se já existir padrão equivalente no projeto de Estoque/Tenant.
- `IdUnidade` (Branch) segue o mesmo mecanismo já usado nos projetos existentes; não criar nova
  representação de Branch.
- Índices únicos que dependem de Tenant (ex.: código de cupom, se adicionado) devem ser compostos
  `(TenantId, Codigo)`.

## 9. Decisões Pendentes (lista consolidada)

| # | Decisão pendente | Resolvida em |
|---|---|---|
| 1 | Estrutura real dos projetos Tenant/Identity e Estoque (namespaces, padrões) | Etapa 01 |
| 2 | Pedido é a própria entidade-carrinho (confirmar) | Etapa 03 |
| 3 | Origem do limite de parcelas por Tenant (API Tenant vs tabela local) | Etapa 06 |
| 4 | Carrinho permite usuário anônimo ou exige login? | Etapa 03 |
| 5 | Mecanismo de mensageria disponível no projeto (se houver) para baixa de estoque assíncrona | Etapa 08 |
| 6 | Campos exatos do snapshot em ProdutosVenda/ProdutosPedido (proposta na seção 4.7/4.8, a confirmar) | Etapa 03/07 |
| 7 | Cache (o que cachear: catálogo de FormaPagto? Cupons ativos?) | Etapa 09 |

## 10. Estratégia de Concorrência e Escalabilidade

- Consumo de cupom: concorrência otimista (RowVersion) + constraint de banco.
- Finalização de pedido: idempotência via chave de idempotência opcional no endpoint de checkout
  (evita dupla venda em duplo clique/retry de rede).
- Paginação obrigatória em listagens (pedidos, vendas, cupons).
- Cache leve (in-memory ou distribuído, conforme padrão do projeto) para catálogo `FormaPagto` e
  `StatusPedido` (dados quase estáticos).
- Endpoints de checkout devem ser stateless e prontos para múltiplas réplicas atrás de load balancer.

## 11. Estratégia de Testes

- **Unitários:** cálculo de desconto (prioridade de cupom), cálculo de parcelas, cálculo de
  ValorTotal/ValorFinal, transições de status.
- **Integração:** persistência com PostgreSQL (Testcontainers ou equivalente já usado no projeto),
  isolamento de Tenant (Tenant A não vê dados de Tenant B).
- **API:** endpoints de carrinho, cupom, checkout — incluindo autorização (401/403).
- **Concorrência:** dois checkouts simultâneos consumindo o último cupom disponível → só um deve
  suceder.
- Cada etapa do roadmap implementa e executa seus próprios testes antes de ser marcada concluída.

## 12. Estrutura Proposta do Novo Microsserviço

Seguir exatamente o padrão encontrado nos projetos Estoque/Tenant (a confirmar na Etapa 01).
Estrutura de referência assumida até confirmação:

```
PedidosVendas.Domain          (Entidades, Enums, Interfaces de domínio, regras de negócio puras)
PedidosVendas.Application     (Casos de uso, DTOs, Validators, Interfaces de Application)
PedidosVendas.Infrastructure  (DbContext, Repositories, Migrations, Clients HTTP p/ Estoque/Tenant)
PedidosVendas.API             (Controllers, Middlewares, Program.cs, Swagger)
PedidosVendas.Tests.Unit
PedidosVendas.Tests.Integration
```

## 13. Roadmap de Desenvolvimento (visão geral)

| Etapa | Nome | Entrega principal |
|---|---|---|
| 01 | Contexto e Esqueleto do Projeto | Inspeção do repo real, criação da solution/projetos seguindo padrão existente, DbContext, Docker Compose, healthcheck |
| 02 | Domínio de Cupom | Entidades Cupom/CupomProduto, migrations, regras de validação e prioridade, testes unitários |
| 03 | Carrinho e Pedido | Entidade Pedido/ProdutosPedido, endpoints de carrinho, integração de leitura com Estoque, snapshot de preço |
| 04 | Aplicação de Cupom ao Pedido | Endpoint de aplicar/remover cupom no carrinho, cálculo de desconto, testes de concorrência de quantidade |
| 05 | Frete | Abstração `IFreteCalculator`, implementação mock, endpoint de cálculo no checkout |
| 06 | Formas de Pagamento e Parcelas | Entidade FormaPagto + seed, regra de parcelas, resolução de limite (Tenant/fallback) |
| 07 | Finalização da Compra (Venda) | Entidade Venda/ProdutosVenda, transação de checkout completa, seed de Status, testes de transação |
| 08 | Integração de Estoque (baixa) | Chamada/evento de baixa de estoque pós-venda, compensação em falha, testes de consistência |
| 09 | Escalabilidade, Cache, Documentação e Fechamento | Cache de catálogos, idempotência, revisão de índices, documentação final, checklist de produção |

Cada etapa gera `docs/prompts/PROMPT_ETAPA_XX.md` (entregue nesta conversa) e, ao final, uma
`docs/memoria/MEMORIA_ETAPA_XX.md` (gerada pelo OpenCode durante a execução, a partir do template em
`docs/memoria/MEMORIA_ETAPA_00.md`).
