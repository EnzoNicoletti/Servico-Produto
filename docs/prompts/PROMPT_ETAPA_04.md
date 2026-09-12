# PROMPT_ETAPA_04.md — Aplicação de Cupom ao Pedido

## Papel
Continue como o mesmo Arquiteto/Desenvolvedor Sênior especialista em e-commerce e ASP.NET Core 10
responsável por este microsserviço.

## Antes de tudo
1. Leia todas as memórias existentes (`docs/memoria/MEMORIA_ETAPA_00.md` até `docs/memoria/MEMORIA_ETAPA_03.md`).
2. Releia as seções 4.4, 4.9 e 6.2 de `docs/planejamento/00_ANALISE_E_ROADMAP.md`.
3. Confirme no código real os serviços já implementados: `ICupomEligibilityService` (Etapa 02) e o
   fluxo de carrinho (Etapa 03).

## Objetivo desta etapa
Ligar o domínio de Cupom (já implementado e testado isoladamente) ao carrinho/`Pedido`, incluindo o
cálculo de desconto e o tratamento de concorrência no consumo do cupom.

## Escopo desta etapa

1. Endpoint para aplicar um cupom (por Id ou código, conforme o que existir) a um carrinho aberto:
   - Validar Tenant, validade, quantidade disponível, valor mínimo de compra contra o subtotal atual
     do carrinho.
   - Gravar `Pedido.IdCupom`.
   - Não decrementar `Cupom.Quantidade` ainda — isso só ocorre na finalização (Etapa 07), pois o
     carrinho pode ser abandonado.
2. Endpoint para remover cupom aplicado ao carrinho.
3. Endpoint/serviço para calcular o desconto atual do carrinho (usado tanto para exibir ao usuário
   quanto reutilizado na finalização): aplica `ICupomEligibilityService` sobre os itens do carrinho e
   retorna valor de desconto total e por item.
4. Revalidação: se o carrinho mudar depois do cupom aplicado (item removido, por exemplo) e o
   `ValorMinimoCompra` deixar de ser atingido, o desconto deixa de ser exibido/aplicado automaticamente
   até nova validação — decidir e documentar se o cupom é removido automaticamente ou apenas marcado
   como inválido até a finalização recalcular.
5. Implementar o decremento controlado de `Cupom.Quantidade` como método de domínio reutilizável
   (`ConsumirCupom()` ou equivalente) com concorrência otimista (RowVersion) — **o método é
   implementado agora, mas só será chamado de fato na Etapa 07 (finalização)**. Cobrir com teste de
   concorrência simulando duas chamadas simultâneas ao mesmo cupom com `Quantidade = 1`.

## Testes desta etapa
- Unitários: cálculo de desconto total do carrinho com cupom de produto, de categoria e global.
- Unitários: comportamento quando o carrinho muda e o valor mínimo deixa de ser atingido.
- Concorrência: duas "finalizações simuladas" chamando `ConsumirCupom()` ao mesmo tempo em um cupom
  com `Quantidade = 1` — apenas uma deve suceder, a outra deve falhar de forma controlada (sem
  exceção não tratada, com erro de negócio claro).
- Integração: aplicar/remover cupom via API, respeitando Tenant.

## Critérios de conclusão
- [ ] Endpoints de aplicar/remover/calcular desconto funcionando.
- [ ] Método `ConsumirCupom()` implementado com concorrência otimista e testado.
- [ ] Todos os testes passando, build sem erros.

## Documentar e registrar
- Criar `docs/memoria/MEMORIA_ETAPA_04.md`.
- Registrar a decisão tomada no item 4 (comportamento quando o carrinho muda após cupom aplicado).
- Declarar conclusão explícita e apontar a próxima etapa (`docs/prompts/PROMPT_ETAPA_05.md` — Frete).

## Regra fundamental
`Cupom.Quantidade` só é efetivamente decrementada na finalização real da compra (Etapa 07). Nesta
etapa o método existe e está testado, mas não é chamado em produção ainda pelo fluxo de carrinho.
