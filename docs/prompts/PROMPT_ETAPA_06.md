# PROMPT_ETAPA_06.md — Formas de Pagamento e Regras de Parcelamento

## Papel
Continue como o mesmo Arquiteto/Desenvolvedor Sênior especialista em e-commerce e ASP.NET Core 10
responsável por este microsserviço.

## Antes de tudo
1. Leia todas as memórias existentes (`docs/memoria/MEMORIA_ETAPA_00.md` até `docs/memoria/MEMORIA_ETAPA_05.md`).
2. Releia as seções 4.5 e 6.4 de `docs/planejamento/00_ANALISE_E_ROADMAP.md`.
3. **Inspecione o microsserviço de Tenant/Identity** para verificar se já existe um campo/tabela de
   configuração de limite máximo de parcelas por Tenant/Loja. Isso resolve a decisão pendente #3.

## Objetivo desta etapa
Implementar `FormaPagto`, seu seed, e a regra de validação de parcelas conforme a forma de pagamento
escolhida, incluindo a resolução do limite de parcelas do cartão de crédito.

## Escopo desta etapa

1. Criar entidade `FormaPagto`: `Id`, `Descricao`, `QtdMaximaParcelas`. Tratar como catálogo global
   (compartilhado entre Tenants), a menos que a inspeção do Tenant/Identity indique o contrário —
   documentar a decisão final.
2. Seed de `FormaPagto`: PIX, Transferência, Depósito, Cartão de Débito (todos com
   `QtdMaximaParcelas = 1`), Cartão de Crédito (`QtdMaximaParcelas` = valor padrão razoável, ex. 12 —
   ajustar conforme decisão de negócio e documentar).
3. Resolver a decisão pendente #3 (origem do limite de parcelas por Tenant):
   - Se o Tenant/Identity já expõe essa configuração → consumir via API/serviço existente.
   - Se não → criar tabela local `TenantFormaPagtoConfig` (`TenantId`, `IdFormaPagto`,
     `QtdMaximaParcelasOverride`) como fallback, usada apenas quando existir override; na ausência,
     usar `FormaPagto.QtdMaximaParcelas`.
   - Documentar a decisão tomada e a justificativa na memória.
4. Implementar serviço/validador de parcelas: dado `IdFormaPagto` e `QuantidadeParcelas` informados
   no checkout,
   - Se a forma de pagamento for PIX, transferência, depósito ou débito → forçar
     `QuantidadeParcelas = 1`, rejeitando qualquer outro valor enviado pelo cliente.
   - Se for cartão de crédito → validar `QuantidadeParcelas` entre 1 e o limite resolvido (Tenant
     override, senão `FormaPagto.QtdMaximaParcelas`).
5. Endpoint de listagem de formas de pagamento disponíveis (com o limite de parcelas já resolvido
   para o Tenant do usuário logado, para o frontend exibir corretamente).
6. Migration para `FormaPagto` (+ `TenantFormaPagtoConfig`, se aplicável) e seed via migration ou
   `DbContext.Seed`, seguindo o padrão de seed já usado no projeto (se o Estoque já faz seed de
   alguma tabela, reaproveitar a mesma abordagem).

## Testes desta etapa
- Unitários: validação de parcelas para cada forma de pagamento (incluindo tentativa de burlar o
  limite de 1 parcela em débito/PIX/transferência/depósito).
- Unitários: resolução do limite de parcelas de crédito com e sem override de Tenant.
- Integração: seed aplicado corretamente após migration.
- Integração: endpoint de listagem retorna os limites corretos por Tenant.

## Critérios de conclusão
- [ ] Entidade `FormaPagto` (+ eventual `TenantFormaPagtoConfig`) e seed aplicados.
- [ ] Validação de parcelas implementada e testada para todos os métodos de pagamento.
- [ ] Decisão pendente #3 resolvida e documentada.
- [ ] Todos os testes passando, build sem erros.

## Documentar e registrar
- Criar `docs/memoria/MEMORIA_ETAPA_06.md`.
- Declarar conclusão explícita e apontar a próxima etapa (`docs/prompts/PROMPT_ETAPA_07.md` — Finalização da
  Compra / Venda).

## Regra fundamental
A regra "PIX/transferência/depósito/débito = 1 parcela, não alterável" deve ser garantida no backend
independentemente do que o cliente enviar — nunca confiar apenas em validação de frontend.
