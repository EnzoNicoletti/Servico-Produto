# PROMPT_ETAPA_09.md — Escalabilidade, Cache, Documentação e Fechamento

## Papel
Continue como o mesmo Arquiteto/Desenvolvedor Sênior especialista em e-commerce e ASP.NET Core 10
responsável por este microsserviço.

## Antes de tudo
1. Leia **todas** as memórias existentes (`docs/memoria/MEMORIA_ETAPA_00.md` até `docs/memoria/MEMORIA_ETAPA_08.md`).
2. Releia as seções 10, 11 e 14 (Docker/Documentação) de `docs/planejamento/00_ANALISE_E_ROADMAP.md`.
3. Revise o serviço completo com uma visão de "está pronto para produção com milhares de usuários
   simultâneos?".

## Objetivo desta etapa
Fechar o microsserviço: revisar performance/escalabilidade, adicionar cache onde justificado,
consolidar Docker/observabilidade e produzir a documentação final exigida.

## Escopo desta etapa

1. **Cache:** decidir e implementar cache (in-memory ou distribuído, conforme padrão já usado no
   projeto — se o Estoque já usa Redis, reaproveitar) para dados quase-estáticos: catálogo
   `FormaPagto`, `StatusPedido`. Resolver a decisão pendente #7. Não cachear dados voláteis (estoque,
   carrinho, cupom disponível) sem uma estratégia de invalidação clara.
2. **Paginação:** revisar todos os endpoints de listagem (Cupons, Pedidos, Vendas) e garantir
   paginação consistente (parâmetros padrão do projeto, se já existirem em Estoque/Tenant).
3. **Índices:** revisar todos os índices criados ao longo das etapas anteriores contra os padrões de
   acesso reais dos endpoints implementados; adicionar índices faltantes via nova migration,
   documentando o motivo de cada um.
4. **Idempotência e concorrência:** revisão final dos pontos críticos (finalização de compra,
   consumo de cupom) — garantir que testes de carga leve (múltiplas requisições concorrentes, mesmo
   que simuladas em teste de integração) não geram inconsistência.
5. **Docker:** revisar `Dockerfile` e `docker-compose.yml` do serviço: variáveis de ambiente,
   healthcheck, dependências de inicialização (`depends_on` com condição de healthy), consistência
   com os demais serviços.
6. **Logging e tratamento de erros:** confirmar que o serviço usa o mesmo padrão de logging
   estruturado e o mesmo middleware de tratamento de exceções dos demais microsserviços (não criar
   um padrão paralelo).
7. **Documentação final** (arquivo `DOCUMENTACAO.md` ou equivalente ao padrão do projeto), cobrindo:
   arquitetura, entidades e relacionamentos, todas as regras de negócio (cupom, carrinho, pedido,
   venda, pagamento, frete), integração com Estoque, endpoints (pode referenciar o Swagger/OpenAPI
   gerado), configuração de ambiente, Docker, e resumo da estratégia de testes.
8. Checklist final de produção: revisar cada item da seção 10 (concorrência/escalabilidade) e da
   seção 11 (testes) de `docs/planejamento/00_ANALISE_E_ROADMAP.md`, confirmando o que foi implementado e o que ficou
   como débito técnico documentado (se algo ficou de fora, registrar explicitamente por quê).

## Testes desta etapa
- Teste de carga leve/concorrência nos endpoints de checkout e aplicação de cupom (múltiplas
  requisições paralelas em teste de integração).
- Suite completa de testes (unitários + integração) de todas as etapas anteriores executada e
  aprovada em conjunto (regressão).
- Teste de subida completa via `docker-compose up` de todos os serviços juntos.

## Critérios de conclusão
- [ ] Cache implementado onde justificado e documentado.
- [ ] Índices revisados e migration final aplicada, se necessário.
- [ ] Docker Compose consistente e funcional com todos os serviços.
- [ ] `DOCUMENTACAO.md` completo entregue.
- [ ] Suite completa de testes passando (regressão de todas as etapas).
- [ ] Checklist de produção revisado e qualquer débito técnico explicitamente registrado.

## Documentar e registrar
- Criar `docs/memoria/MEMORIA_ETAPA_09.md`, resolvendo a decisão pendente #7 e consolidando o estado final do
  projeto.
- Declarar conclusão explícita do microsserviço `PedidosVendas`: funcionalidades implementadas,
  testes executados/aprovados, débitos técnicos remanescentes (se houver) e recomendação de próximos
  passos fora do escopo deste roadmap (ex.: integração fiscal do `NrPedido`, provedor real de frete,
  cache distribuído se ainda local).

## Regra fundamental
Esta é a etapa de fechamento — não introduzir novas regras de negócio não previstas nas etapas
anteriores. O foco é robustez, consistência e documentação do que já foi construído.
