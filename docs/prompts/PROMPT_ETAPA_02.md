# PROMPT_ETAPA_02.md — Domínio de Cupom

## Papel
Continue como o mesmo Arquiteto/Desenvolvedor Sênior especialista em e-commerce e ASP.NET Core 10
responsável por este microsserviço.

## Antes de tudo
1. Leia todas as memórias existentes (`docs/memoria/MEMORIA_ETAPA_00.md`, `docs/memoria/MEMORIA_ETAPA_01.md`).
2. Releia a seção 4.1–4.4 e 4.9 de `docs/planejamento/00_ANALISE_E_ROADMAP.md` (regras de cupom e concorrência).
3. Inspecione o código já criado na Etapa 01 (não presuma nomes de classes — confirme no projeto
   real) e reutilize os padrões de camada, DbContext e resposta de API já estabelecidos.
4. Confirme no serviço de Estoque como `Produto` e `Categoria` são identificados (Id, formato) para
   garantir que as FKs de referência (`IdProduto`, `IdCategoria`) sejam compatíveis.

## Objetivo desta etapa
Implementar o domínio de Cupom: entidades `Cupom` e `CupomProduto`, regras de validação e a lógica de
elegibilidade/prioridade de aplicação — **sem ainda integrar com Pedido** (isso é a Etapa 04).

## Escopo desta etapa

1. Criar entidade `Cupom` no Domain com os campos da especificação original: `Id`, `Descricao`,
   `ValorDesconto`, `PercDesconto`, `ValorMinimoCompra`, `IdCategoria` (corrigido de `IdCategora`),
   `DataValidade`, `DataCriacao`, `Quantidade`, `isCupomProduto`, mais `TenantId` (obrigatório) e
   `RowVersion`/concurrency token.
2. Criar entidade `CupomProduto` (`Id`, `IdCupom`, `IdProduto`).
3. Regra de domínio: um `Cupom` deve ter exatamente um entre `ValorDesconto` e `PercDesconto`
   preenchido (> 0); validar na criação/atualização.
4. Implementar serviço de domínio/Application `ICupomEligibilityService` (ou nome equivalente ao
   padrão do projeto) que, dado um cupom e uma lista de itens (IdProduto, IdCategoria, Quantidade,
   ValorUnitario), retorna quais itens são elegíveis ao desconto, seguindo a prioridade:
   produto específico (via `CupomProduto`) > categoria (`IdCategoria`) > global.
5. Implementar validações de aplicabilidade de cupom: `TenantId` do cupom bate com o do
   contexto, `DataValidade` não expirada, `Quantidade > 0`, `ValorMinimoCompra` atingido pelo
   subtotal do carrinho (recebido como parâmetro — ainda não há Pedido real nesta etapa).
6. CRUD de Cupom via API (criar, listar paginado, obter por Id, atualizar, inativar/remover), sempre
   filtrando/gravando por `TenantId` do contexto autenticado.
7. Migration criando as tabelas `Cupom` e `CupomProduto` com:
   - Índice em `(TenantId)` e em `(TenantId, DataValidade)`.
   - Constraint garantindo que apenas um entre `ValorDesconto`/`PercDesconto` seja > 0 (check
     constraint no banco, além da validação de domínio).
   - `CHECK (Quantidade >= 0)`.
   - FK de `CupomProduto.IdCupom` para `Cupom.Id` com delete cascade (cupom excluído remove seus
     vínculos de produto).

## Testes desta etapa
- Unitários: prioridade de elegibilidade (produto > categoria > global) com casos de borda (produto
  em duas categorias, cupom sem nenhum vínculo, cupom com `CupomProduto` mas produto do carrinho não
  vinculado).
- Unitários: validação de exclusividade `ValorDesconto` x `PercDesconto`.
- Unitários: validação de `ValorMinimoCompra`, `DataValidade`, `Quantidade`.
- Integração: isolamento de Tenant (Tenant A não enxerga/edita cupom do Tenant B).
- Integração: constraint de banco realmente impede os dois campos de desconto preenchidos.

## Critérios de conclusão
- [ ] Entidades e migration aplicadas com sucesso.
- [ ] CRUD de Cupom funcionando e respeitando Tenant.
- [ ] Serviço de elegibilidade implementado e coberto por testes unitários.
- [ ] Todos os testes passando, build sem erros.

## Documentar e registrar
- Criar `docs/memoria/MEMORIA_ETAPA_02.md` a partir do template, detalhando as entidades criadas, a migration, os
  testes e qualquer divergência encontrada em relação ao planejado em `docs/planejamento/00_ANALISE_E_ROADMAP.md`.
- Declarar conclusão explícita e apontar a próxima etapa (`docs/prompts/PROMPT_ETAPA_03.md` — Carrinho e Pedido).

## Regra fundamental
Não implemente ainda a aplicação do cupom a um Pedido real — isso é a Etapa 04. Esta etapa entrega
apenas o domínio de Cupom isolado e testável.
