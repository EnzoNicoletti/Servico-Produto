# PROMPT_ETAPA_07.md — Finalização da Compra (Pedido → Venda)

## Papel
Continue como o mesmo Arquiteto/Desenvolvedor Sênior especialista em e-commerce e ASP.NET Core 10
responsável por este microsserviço.

## Antes de tudo
1. Leia **todas** as memórias existentes (`docs/memoria/MEMORIA_ETAPA_00.md` até `docs/memoria/MEMORIA_ETAPA_06.md`) — esta é a
   etapa mais crítica do serviço e depende de tudo que já foi implementado (Cupom, Carrinho, Frete,
   FormaPagto).
2. Releia as seções 4.6, 4.7, 6.5 e 10 de `docs/planejamento/00_ANALISE_E_ROADMAP.md`.
3. Confirme os serviços/métodos já existentes: `ICupomEligibilityService`, `ConsumirCupom()`,
   `IFreteCalculator`, validador de parcelas.

## Objetivo desta etapa
Implementar a operação transacional de finalização da compra: criar `Venda` e `ProdutosVenda` a
partir do `Pedido`/`ProdutosPedido`, aplicando todas as regras já implementadas nas etapas
anteriores, com consistência garantida.

## Escopo desta etapa

1. Criar enum/seed final de `StatusPedido` caso a Etapa 03 tenha deixado incompleto — revisar
   transições válidas (Carrinho → AguardandoPagamento → VendaEfetuada, e Carrinho/AguardandoPagamento
   → Cancelado). Definir e documentar quais transições são permitidas e quem pode disparar cada uma.
2. Criar entidade `Venda`: `Id`, `IdPedido`, `DataVenda`, `IdFormaPagto`, `ValorBruto`,
   `ValorDesconto`, `ValorLiquidoPedido`, `ValorFinal` (líquido + frete), `isPago`, `NrPedido`
   (nullable, reservado para futura NF), `QuantidadeParcelas`, `TenantId`.
3. Criar entidade `ProdutosVenda` (snapshot): `Id`, `IdVenda`, `IdProduto`, `Quantidade`,
   `NomeProduto`, `Sku` (ou campo equivalente do Estoque), `ValorUnitario`, `ValorDesconto` do item —
   confirmar nomes exatos disponíveis no Estoque durante a implementação e documentar o snapshot
   final escolhido.
4. Implementar o caso de uso `FinalizarCompra` (endpoint de checkout), transacional, executando na
   ordem:
   1. Carregar o `Pedido` (deve estar em `Status = Carrinho` ou `AguardandoPagamento`) do Tenant/
      usuário autenticado — 404/403 caso contrário.
   2. Revalidar itens do carrinho contra o Estoque (disponibilidade atual).
   3. Se houver `IdCupom`, revalidar (Tenant, validade, quantidade, valor mínimo) e recalcular
      desconto via `ICupomEligibilityService`.
   4. Recalcular frete via `IFreteCalculator` com o CEP informado no checkout.
   5. Validar `IdFormaPagto` e `QuantidadeParcelas` via o validador da Etapa 06.
   6. Calcular `ValorBruto`, `ValorDesconto`, `ValorLiquidoPedido`, `ValorFinal`.
   7. Criar `Venda` e os registros de `ProdutosVenda` (snapshot).
   8. Se houver cupom, chamar `ConsumirCupom()` (decremento com concorrência otimista) — se falhar
      (cupom esgotado por corrida), abortar toda a transação com erro de negócio claro.
   9. Atualizar `Pedido.Status = VendaEfetuada`, `Pedido.DataFechamento = now`.
   10. Commit único. Qualquer falha em qualquer passo → rollback completo, `Pedido` permanece
       inalterado (ou volta a `Status = Carrinho`, conforme decisão a documentar).
5. Implementar idempotência no endpoint de finalização (ex.: chave de idempotência enviada pelo
   cliente ou baseada em `IdPedido` + verificação de status atual) para evitar dupla venda em caso de
   duplo clique/retry de rede.
6. Migration para `Venda` e `ProdutosVenda`, com FK de `ProdutosVenda.IdVenda` para `Venda.Id`
   (cascade) e índice `(TenantId, IdPedido)` único em `Venda` (um pedido só pode gerar uma venda).

## Testes desta etapa
- Integração: fluxo feliz completo (carrinho com itens, cupom, frete, pagamento) resultando em
  `Venda` correta e `Pedido` fechado.
- Integração: falha em qualquer passo (ex.: item ficou indisponível durante a finalização) causa
  rollback total — nenhuma `Venda` parcial é criada.
- Concorrência: duas finalizações simultâneas do mesmo `Pedido` (duplo clique) → apenas uma `Venda` é
  criada.
- Concorrência: duas finalizações simultâneas de pedidos diferentes usando o último cupom disponível
  → apenas uma consome o cupom com sucesso.
- Unitários: cálculo de `ValorBruto`/`ValorDesconto`/`ValorLiquidoPedido`/`ValorFinal`.

## Critérios de conclusão
- [ ] Entidades `Venda`/`ProdutosVenda` e migration aplicadas.
- [ ] Caso de uso `FinalizarCompra` transacional, idempotente e testado.
- [ ] Testes de concorrência e rollback passando.
- [ ] Build sem erros.

## Documentar e registrar
- Criar `docs/memoria/MEMORIA_ETAPA_07.md`, detalhando o snapshot final escolhido para `ProdutosVenda` (decisão
  pendente #6) e a estratégia de idempotência adotada.
- Declarar conclusão explícita e apontar a próxima etapa (`docs/prompts/PROMPT_ETAPA_08.md` — Integração de
  Estoque / baixa pós-venda).

## Regra fundamental
Esta operação é a mais sensível do sistema — priorize consistência sobre performance. Nenhuma venda
parcial ou pedido em estado inconsistente pode ser deixado no banco em caso de falha.
