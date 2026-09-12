# PROMPT_ETAPA_03.md — Carrinho e Pedido

## Papel
Continue como o mesmo Arquiteto/Desenvolvedor Sênior especialista em e-commerce e ASP.NET Core 10
responsável por este microsserviço.

## Antes de tudo
1. Leia todas as memórias existentes (`docs/memoria/MEMORIA_ETAPA_00.md` até `docs/memoria/MEMORIA_ETAPA_02.md`).
2. Releia as seções 4.1, 4.8, 6.1 e 8 de `docs/planejamento/00_ANALISE_E_ROADMAP.md`.
3. Confirme no código real como se obtém `IdCliente` (usuário logado) e `IdUnidade` (Branch) a partir
   do contexto autenticado.
4. Confirme no serviço de Estoque o endpoint/contrato real para consultar um produto por Id (preço,
   nome, disponibilidade) — você vai depender dele nesta etapa.

## Objetivo desta etapa
Implementar `Pedido` e `ProdutosPedido`, cobrindo o ciclo de vida de carrinho (`Status = Carrinho`)
até o momento anterior ao checkout, incluindo o gerenciamento de itens.

## Escopo desta etapa

1. Criar enum/seed `StatusPedido` (ao menos: `Carrinho`, `AguardandoPagamento`, `VendaEfetuada`,
   `Cancelado` — ajuste/expanda conforme necessidade encontrada, documentando a decisão).
2. Criar entidade `Pedido`: `Id`, `DataAbertura`, `IdCliente` (nullable), `IdCupom` (nullable),
   `IdUnidade` (nullable), `Status`, `DataFechamento` (nullable), `ValorTotal`, `TenantId`.
3. Criar entidade `ProdutosPedido`: `Id`, `IdProduto`, `Quantidade`, `IdPedido` (renomeado de
   `IdCarrinho` conforme decisão 4.1 registrada na memória — confirme que a memória já registra isso;
   se não, registre agora), mais `ValorUnitario` (snapshot, decisão 4.8).
4. Confirmar/decidir: carrinho exige usuário autenticado ou permite anônimo (decisão pendente #4).
   Recomendação: exigir autenticação, pois `IdCliente` é usado para localizar o carrinho ativo do
   usuário — mas valide contra o restante do projeto (se existir sessão anônima em outro serviço,
   siga o mesmo padrão) e registre a decisão final na memória.
5. Endpoints de carrinho:
   - Obter (ou criar, se não existir) o carrinho aberto do usuário logado na Unidade atual.
   - Adicionar item (consulta o Estoque para validar existência/disponibilidade e obter o
     `ValorUnitario` no momento; se o item já existir no carrinho, soma quantidade).
   - Atualizar quantidade de um item.
   - Remover item.
   - Recalcular `Pedido.ValorTotal` (soma dos itens, sem desconto de cupom ainda) a cada alteração.
6. Migration para `Pedido` e `ProdutosPedido`, com:
   - Índice `(TenantId, IdCliente, Status)` para localizar carrinho aberto rapidamente.
   - FK de `ProdutosPedido.IdPedido` para `Pedido.Id` com delete cascade.
   - `CHECK (Quantidade > 0)` em `ProdutosPedido`.

## Testes desta etapa
- Unitários: recálculo de `ValorTotal` ao adicionar/remover/alterar itens.
- Integração: criação de carrinho, adição de item chamando (mock/stub) o client HTTP do Estoque.
- Integração: isolamento de Tenant e de usuário (usuário A não vê/edita carrinho do usuário B).
- Integração: item inexistente ou indisponível no Estoque é rejeitado com erro claro.

## Critérios de conclusão
- [ ] Entidades e migration aplicadas.
- [ ] Endpoints de carrinho funcionando fim a fim (criar, adicionar, atualizar, remover, obter).
- [ ] Integração de leitura com Estoque funcionando (via client HTTP real ou mock configurável para
      testes).
- [ ] Todos os testes passando, build sem erros.

## Documentar e registrar
- Criar `docs/memoria/MEMORIA_ETAPA_03.md`.
- Resolver explicitamente as decisões pendentes #2, #4 e a parte de `ProdutosPedido` da #6 do
  template de memória, registrando a decisão final tomada e por quê.
- Declarar conclusão explícita e apontar a próxima etapa (`docs/prompts/PROMPT_ETAPA_04.md` — Aplicação de Cupom
  ao Pedido).

## Regra fundamental
Ainda não implemente aplicação de cupom, frete, pagamento ou finalização — isso é escopo das
próximas etapas. Esta etapa entrega apenas o ciclo de vida do carrinho.
