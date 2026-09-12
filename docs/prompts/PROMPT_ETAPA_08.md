# PROMPT_ETAPA_08.md — Integração de Estoque (Baixa Pós-Venda)

## Papel
Continue como o mesmo Arquiteto/Desenvolvedor Sênior especialista em e-commerce e ASP.NET Core 10
responsável por este microsserviço.

## Antes de tudo
1. Leia todas as memórias existentes (`docs/memoria/MEMORIA_ETAPA_00.md` até `docs/memoria/MEMORIA_ETAPA_07.md`).
2. Releia a seção 7 de `docs/planejamento/00_ANALISE_E_ROADMAP.md`.
3. **Inspecione o microsserviço de Estoque** para descobrir: (a) se já existe endpoint de baixa de
   estoque/movimentação, (b) se já existe infraestrutura de mensageria (RabbitMQ, Azure Service Bus,
   Kafka, etc.) em uso em qualquer parte do projeto. Isso resolve a decisão pendente #5.

## Objetivo desta etapa
Efetivar a baixa de estoque após uma venda ser confirmada, com consistência eventual e mecanismo de
compensação/retry em caso de falha, sem acoplar a transação de venda à disponibilidade momentânea do
serviço de Estoque.

## Escopo desta etapa

1. Com base na inspeção, decidir e documentar a abordagem:
   - **Se existir mensageria no projeto:** publicar um evento de domínio (ex.: `VendaConfirmada` ou
     `EstoqueDeveSerBaixado`) após o commit da Etapa 07, consumido por um handler que chama o
     Estoque (ou o próprio Estoque assina o evento, se essa for a convenção do projeto).
   - **Se não existir mensageria:** implementar um mecanismo de fila local/tabela de outbox
     (`OutboxMessage` com `IdVenda`, `Status`, `Tentativas`) processada por um background job
     (`IHostedService`/worker) que chama o endpoint de baixa do Estoque com retry e backoff,
     evitando acoplar a transação de venda à disponibilidade do serviço de Estoque em tempo real.
2. Implementar o consumo/chamada real: para cada item de `ProdutosVenda`, solicitar ao Estoque a
   baixa de `IdProduto`/`Quantidade`, referenciando `IdVenda` para idempotência do lado do Estoque
   (evitar baixa duplicada em caso de retry).
3. Tratamento de falha: se o Estoque não confirmar a baixa após as tentativas configuradas, marcar a
   venda/outbox como `FalhaBaixaEstoque` e registrar para intervenção manual/alerta — **a venda já
   confirmada não deve ser desfeita automaticamente** (decisão de negócio a confirmar e documentar,
   já que a cobrança ao cliente já ocorreu).
4. Endpoint/consulta administrativa para listar vendas com baixa de estoque pendente ou com falha
   (suporte operacional).

## Testes desta etapa
- Integração: venda confirmada dispara a baixa de estoque com sucesso (mock do Estoque respondendo
  200).
- Integração: falha temporária do Estoque é reprocessada (retry) até sucesso.
- Integração: falha persistente marca o registro como `FalhaBaixaEstoque` sem desfazer a venda.
- Integração: chamada duplicada (retry) não causa baixa duplicada de estoque (idempotência).

## Critérios de conclusão
- [ ] Mecanismo de baixa pós-venda implementado (evento ou outbox, conforme decisão documentada).
- [ ] Retry e tratamento de falha implementados e testados.
- [ ] Endpoint de consulta de pendências/falhas disponível.
- [ ] Build sem erros, todos os testes passando.

## Documentar e registrar
- Criar `docs/memoria/MEMORIA_ETAPA_08.md`, registrando a decisão pendente #5 resolvida (mensageria existente vs.
  outbox local) e a justificativa.
- Declarar conclusão explícita e apontar a próxima etapa (`docs/prompts/PROMPT_ETAPA_09.md` — Escalabilidade,
  Cache, Documentação e Fechamento).

## Regra fundamental
A venda já confirmada ao cliente é fato consumado — problemas de baixa de estoque geram pendência
operacional a ser resolvida separadamente, nunca revertem automaticamente a venda ou o pagamento.
