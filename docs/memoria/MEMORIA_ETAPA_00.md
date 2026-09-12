# MEMORIA_ETAPA_00.md — Estado Inicial do Projeto PedidosVendas

> Este arquivo é o ponto de partida da memória persistente. A partir da Etapa 01, o OpenCode deve
> criar `docs/memoria/MEMORIA_ETAPA_01.md`, `docs/memoria/MEMORIA_ETAPA_02.md`, etc., cada um seguindo esta mesma estrutura de
> seções e **lendo todos os arquivos de memória anteriores antes de começar uma nova etapa**.

## Resumo
- Objetivo do microsserviço: Carrinho, Pedidos, Cupons, Formas de Pagamento, Venda e integração com
  Estoque, dentro de uma plataforma multi-tenant/multi-branch de e-commerce.
- Status geral: planejamento concluído (Etapa 1 de análise). Nenhum código implementado ainda.

## Estado atual
- Estrutura: a confirmar na Etapa 01 (projeto ainda não criado).
- Projetos existentes reutilizados como referência: microsserviço de Tenant/Identity, microsserviço
  de Estoque.
- Funcionalidades implementadas: nenhuma.
- Pendências: todas — ver roadmap em `docs/planejamento/00_ANALISE_E_ROADMAP.md`.

## Decisões técnicas
- `IdCarrinho` da especificação original será tratado como `IdPedido` — `Pedido` cumpre o papel de
  carrinho enquanto `Status = Carrinho` (ver seção 4.1 do documento de análise). **Preserva o
  comportamento exigido pelo professor**, corrige apenas nomenclatura de campo.
- `Cupom.IdCategora` corrigido para `IdCategoria` (typo).
- `Cupom.ValorDesconto` e `Cupom.PercDesconto` coexistem no schema; regra de domínio exige que
  exatamente um dos dois esteja preenchido por cupom.
- Prioridade de cupom: produto específico > categoria > global (determinística).
- `ProdutosPedido` recebe `ValorUnitario` (snapshot de preço) — melhoria necessária para calcular
  `Pedido.ValorTotal`, não presente explicitamente na tabela do professor mas indispensável.
- `ProdutosVenda` é um snapshot completo (não referencia dados voláteis do Produto atual).
- Concorrência no consumo de `Cupom.Quantidade`: controle otimista (RowVersion) + constraint
  `CHECK (Quantidade >= 0)`.
- TenantId sempre obtido via Claims/contexto autenticado, nunca do payload do cliente.

## Banco de dados
- Nenhuma migration criada ainda.
- Entidades planejadas: `Cupom`, `CupomProduto`, `Pedido`, `ProdutosPedido`, `Venda`,
  `ProdutosVenda`, `FormaPagto`, enum/seed `StatusPedido`.

## Testes
- Nenhum teste criado ainda. Estratégia definida em `docs/planejamento/00_ANALISE_E_ROADMAP.md`, seção 11.

## Problemas
- Nenhum até o momento.

## Decisões pendentes
1. Estrutura real dos projetos Tenant/Identity e Estoque (namespaces, padrões, DbContext) —
   **resolver na Etapa 01**.
2. Confirmação de que `Pedido` é a própria entidade-carrinho — **Etapa 03**.
3. Origem do limite de parcelas de cartão de crédito por Tenant (API do Tenant vs. tabela local de
   override) — **Etapa 06**.
4. Carrinho exige usuário autenticado ou permite anônimo? — **Etapa 03**.
5. Mecanismo de mensageria disponível no projeto (se houver) para baixa de estoque assíncrona —
   **Etapa 08**.
6. Campos exatos do snapshot em `ProdutosVenda`/`ProdutosPedido` — proposta feita, confirmar durante
   implementação — **Etapa 03/07**.
7. O que efetivamente cachear (catálogo de FormaPagto? Status? Cupons ativos?) — **Etapa 09**.

## Próximos passos
- Executar `docs/prompts/PROMPT_ETAPA_01.md`: inspecionar o repositório real, confirmar padrões arquiteturais dos
  microsserviços existentes e criar o esqueleto do novo microsserviço `PedidosVendas` seguindo esses
  padrões.
