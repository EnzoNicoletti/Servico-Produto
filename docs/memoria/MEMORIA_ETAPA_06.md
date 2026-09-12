# MEMORIA_ETAPA_06.md — Formas de Pagamento e Parcelas

> Sétima entrada da memória persistente. Segue a estrutura de `MEMORIA_ETAPA_00.md`.
> Etapa executada conforme `docs/prompts/PROMPT_ETAPA_06.md` (+ roadmap 4.5, 6.4).

## Resumo
- Objetivo da etapa: `FormaPagto` + seed, regra de parcelas (à vista × crédito com limite
  por Tenant) e listagem com limites resolvidos.
- Status geral: entidades + migration `AdicionarFormaPagtoEConfig` + seed idempotente,
  build sem erros, **75 testes unitários + 36 de integração passando**, compose validado
  (seed presente). **Decisão pendente #3 resolvida.**

## Estado atual
- Novos arquivos — Domain: `Entities/FormaPagto.cs`, `Entities/TenantFormaPagtoConfig.cs`,
  `Interfaces/IFormaPagtoRepository.cs`, `Exceptions/FormaPagtoInvalidaException.cs`.
- Application/Pagamento: `IParcelamentoService.cs` + `ParcelamentoService.cs`,
  `FormaPagamentoItem.cs` (+`ParcelamentoValidado`), 2 exceptions.
- Infrastructure: configs das 2 tabelas, `Repositories/FormaPagtoRepository.cs`,
  `Persistence/Seed/FormaPagtoSeeder.cs`; pacotes Options/Configuration/Binder explícitos.
- API: `Contracts/Pagamento/*`, `Controllers/FormasPagamentoController.cs`
  (`GET /api/v1/formas-pagamento`, `POST /validar-parcelas`); seed no bloco Development.
- Não implementado (fora de escopo): uso no checkout (Etapa 07), gateway de pagamento.

## Decisões técnicas
- **#3 resolvida — fallback local `TenantFormaPagtoConfig`.** Grep em todo o Identity por
  parcela/pagamento/limite: **zero ocorrências** — não existe campo de parcelas por Tenant
  (roadmap 4.5 previa exatamente este desfecho). Tabela local `(TenantId, IdFormaPagto,
  QtdMaximaParcelasOverride)` UNIQUE, usada só com linha existente; sem linha vale o catálogo.
- **À vista × parcelável deriva de `QtdMaximaParcelas`** (1 ⇒ força 1 parcela, rejeitando
  qualquer outro valor no backend; > 1 ⇒ valida 1..limite). Sem hardcode de "crédito" nas
  regras; `PermiteParcelar` expõe a leitura.
- **Seed espelha o `RoleSeeder` do Identity** (classe estática idempotente por descrição, no
  bloco Development após migrate; Estoque não tem seeds). Catálogo: PIX/Transferência/
  Depósito/Débito = 1, Crédito = 12. Ids não fixos (consumo sempre via listagem).
- **CHECKs** `QtdMaximaParcelas >= 1` (ambas as tabelas); `Descricao` UNIQUE; índice TenantId.
- **Erros:** forma inexistente → 404; parcela inválida → 400 com motivo e limite.

## Banco de dados
- Migration `20260912171711_AdicionarFormaPagtoEConfig`: `FormaPagto`, `TenantFormaPagtoConfig`
  (FK cascade, UNIQUE tenant+forma), CHECKs, índices. Aplicada na fixture e no compose.

## Testes
- Unitários (11 novos; 75 no total, stub em memória): burla do limite 1 em à-vista (4 formas),
  crédito 1/6/12 ok e 0/13 rejeitados, override 6 prevalece (7 rejeitado, outro tenant no 12),
  forma desconhecida, listagem com override.
- Integração (5 novos; 36 no total): seed com 5 linhas + rerun idempotente, listagem com
  valores padrão, validar-parcelas (PIX 1 ok/3 400, crédito 12 ok/13 400, inexistente 404),
  override inserido via DbContext altera listagem e validação (com cleanup), 401 sem JWT.
- Comando: `dotnet test PedidosVendas.slnx` → 75 + 36, 0 falhas; build 0 erros.
- Compose: rebuild, `up`, `/health/ready` 200, seed verificada via psql (5 linhas),
  `identity-*` intactos.

## Problemas
- Nenhum bloqueador. Observação PowerShell: `docker compose exec ... -c "SQL"` corrompe
  aspas — usar pipe via stdin (`echo '...' | ... exec -T ... psql`).

## Decisões pendentes
1. ~~Estrutura~~ — 01. 2. ~~Pedido=carrinho~~ — 03. 3. ~~Parcelas por Tenant~~ — **resolvida**.
4. ~~Auth~~ — 03. 5. Mensageria — **Etapa 08**. 6. `ProdutosVenda` — **Etapa 07**.
7. Cache (+ MediatR) — **Etapa 09**. 8. Branch ativa → **Etapa 07**. 9. Mock produto →
   HTTP real (exigindo `IdCategoria`). 10. Provedor real de frete — futuro.

## Próximos passos
- Executar `docs/prompts/PROMPT_ETAPA_07.md` — Finalização da Compra (Venda):
  `Venda`/`ProdutosVenda`, transação de checkout (revalidar tudo + `ConsumirAsync` +
  `ValidarAsync` + frete recalculado + `IdUnidade` explícito com 403), `Pedido → VendaEfetuada`.
