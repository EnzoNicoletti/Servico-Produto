# PROMPT_ETAPA_01.md — Contexto e Esqueleto do Microsserviço PedidosVendas

## Papel
Você é um Arquiteto de Software e Desenvolvedor Backend Sênior, especialista em ASP.NET Core 10,
DDD, Clean Architecture, PostgreSQL, Docker e sistemas multi-tenant de e-commerce. Você está
trabalhando dentro do repositório real deste projeto, que já contém microsserviços de
**Tenant/Identity** e **Estoque** funcionando e validados.

## Antes de tudo
1. Leia `docs/memoria/MEMORIA_ETAPA_00.md` (e qualquer `docs/memoria/MEMORIA_ETAPA_XX.md` que já exista) para entender o
   histórico e as decisões já tomadas.
2. Leia `docs/planejamento/00_ANALISE_E_ROADMAP.md` para entender o modelo de domínio completo, os fluxos de negócio e
   as decisões arquiteturais recomendadas.
3. **Não presuma nada sobre a estrutura do projeto** — inspecione o repositório real.

## Objetivo desta etapa
Entender profundamente os microsserviços existentes e criar o esqueleto do novo microsserviço
`PedidosVendas`, seguindo exatamente os mesmos padrões — sem implementar ainda as regras de negócio
de Cupom/Pedido/Venda (isso vem nas próximas etapas).

## Escopo desta etapa (e somente este escopo)

1. Inspecionar a solution: liste todos os projetos, suas dependências e responsabilidades.
2. Inspecionar em detalhe o microsserviço de **Tenant/Identity**:
   - Como o `TenantId` é resolvido no contexto de uma requisição (middleware, Claims, serviço).
   - Como o `BranchId`/`UnidadeId` é resolvido.
   - Como funciona a autenticação (JWT: emissão, validação, claims usados).
   - Como outros microsserviços validam o token emitido por este serviço.
3. Inspecionar em detalhe o microsserviço de **Estoque**:
   - Estrutura de camadas (Domain/Application/Infrastructure/API ou equivalente real).
   - Padrão de DbContext, configuração de PostgreSQL, connection string, convenções de migration.
   - Padrão de Repository/Service, injeção de dependência, validação (FluentValidation ou outro).
   - Padrão de resposta de API (envelope de sucesso/erro, ProblemDetails, status codes).
   - Middlewares de tratamento de exceção e logging.
   - Como um serviço externo consulta produtos/categorias deste serviço hoje (se já existir algum
     client HTTP entre serviços, reaproveitar o mesmo padrão).
   - Padrão de testes (framework, estrutura de pastas, uso de Testcontainers ou banco em memória).
4. Inspecionar `docker-compose.yml` (ou arquivos equivalentes) e Dockerfiles existentes: rede,
   variáveis de ambiente, nomes de serviço, healthchecks, portas.
5. Com base no que for encontrado (não no que está assumido em `docs/planejamento/00_ANALISE_E_ROADMAP.md`), criar o
   esqueleto do microsserviço `PedidosVendas`:
   - Mesma convenção de nomes de projeto/namespace usada em Estoque (ex.: se Estoque é
     `Empresa.Estoque.Domain`, criar `Empresa.PedidosVendas.Domain`, etc.).
   - Projeto de Domain, Application, Infrastructure, API (ou os nomes/camadas equivalentes reais).
   - `DbContext` vazio (sem entidades ainda) configurado para PostgreSQL, seguindo o mesmo padrão de
     configuração/opções do Estoque.
   - Projeto(s) de teste seguindo o mesmo padrão do Estoque.
   - Middleware/pipeline de autenticação JWT reaproveitando exatamente o mecanismo do Tenant/Identity
     (não reimplementar autenticação).
   - Health check endpoint básico.
   - Entrada no `docker-compose.yml` compatível com os serviços existentes (nova imagem, novo banco
     ou schema, seguindo o padrão já usado — não inventar um padrão novo).
6. **Não criar** ainda `Cupom`, `Pedido`, `Venda`, `FormaPagto` ou qualquer entidade de negócio.
   Isso é escopo das próximas etapas.

## Testes desta etapa
- Teste de smoke: a API sobe e o endpoint de health check responde 200.
- Teste de integração básico confirmando que o `DbContext` conecta ao PostgreSQL do
  `docker-compose`.
- Teste confirmando que um endpoint protegido rejeita requisição sem JWT válido (401), reaproveitando
  o pipeline de autenticação do Tenant/Identity.

## Critérios de conclusão
- [ ] Solution/projetos criados seguindo exatamente os padrões encontrados no Estoque.
- [ ] `docker-compose.yml` atualizado e o serviço sobe junto com os demais sem quebrar nada existente.
- [ ] DbContext conecta ao PostgreSQL.
- [ ] Autenticação JWT reaproveitada (não reimplementada).
- [ ] Testes de smoke, conexão e autenticação passando.
- [ ] Build completo da solution sem erros.

## Documentar e registrar
- Crie `docs/memoria/MEMORIA_ETAPA_01.md` seguindo exatamente a estrutura de `docs/memoria/MEMORIA_ETAPA_00.md`
  (Resumo / Estado atual / Decisões técnicas / Banco de dados / Testes / Problemas / Decisões
  pendentes / Próximos passos).
- Registre em "Decisões técnicas" **tudo que foi descoberto** sobre os padrões reais do projeto
  (namespaces exatos, forma real de resolver Tenant/Branch, padrão de resposta de API etc.), porque
  as próximas etapas dependem dessas informações.
- Resolva/documente a "Decisão pendente #1" do `docs/memoria/MEMORIA_ETAPA_00.md` (estrutura real dos projetos).
- Ao final, declare explicitamente: Etapa concluída, funcionalidades implementadas, testes
  executados/aprovados, pendências, memória atualizada, e qual é a próxima etapa
  (`docs/prompts/PROMPT_ETAPA_02.md` — Domínio de Cupom).

## Regra fundamental
Não implemente nada fora deste escopo. Se encontrar um padrão no projeto real que diverge do que foi
assumido em `docs/planejamento/00_ANALISE_E_ROADMAP.md`, siga o padrão real do projeto e registre a divergência na
memória — não pergunte, decida e documente.
