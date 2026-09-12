# PROMPT_ETAPA_05.md — Cálculo de Frete

## Papel
Continue como o mesmo Arquiteto/Desenvolvedor Sênior especialista em e-commerce e ASP.NET Core 10
responsável por este microsserviço.

## Antes de tudo
1. Leia todas as memórias existentes (`docs/memoria/MEMORIA_ETAPA_00.md` até `docs/memoria/MEMORIA_ETAPA_04.md`).
2. Releia a seção 6.3 de `docs/planejamento/00_ANALISE_E_ROADMAP.md`.
3. Verifique se já existe, em qualquer microsserviço do projeto, alguma abstração ou integração de
   frete/CEP que deva ser reaproveitada em vez de criada do zero.

## Objetivo desta etapa
Criar a abstração de cálculo de frete e uma implementação inicial local/mockada, plugável no fluxo de
checkout, sem acoplar o domínio a um fornecedor específico.

## Escopo desta etapa

1. Definir a interface de domínio/Application `IFreteCalculator` (ou nome equivalente ao padrão do
   projeto) com um método que recebe CEP de origem (Unidade/Branch, se disponível) ou apenas CEP de
   destino, e a lista de itens/peso/volume (o que for necessário conforme o modelo escolhido), e
   retorna valor de frete e prazo estimado.
2. Implementar `FreteCalculatorMock` (ou local): estratégia simples e determinística (ex.: valor fixo
   por faixa de CEP, ou fórmula baseada em quantidade/valor do pedido) — documentar claramente que é
   provisório e preparado para substituição por integração real (Correios, transportadora, etc.) sem
   alterar o domínio.
3. Registrar `IFreteCalculator` na injeção de dependência de forma que a implementação possa ser
   trocada por configuração, sem alterar código de chamada.
4. Endpoint de checkout (preliminar) para informar CEP e obter o valor de frete calculado para o
   carrinho atual — ainda sem finalizar a compra (isso é Etapa 07). Esse valor pode ser
   armazenado temporariamente em memória/sessão ou recalculado no momento da finalização — decidir e
   documentar a abordagem (recomenda-se **recalcular na finalização** para evitar inconsistência
   entre a cotação exibida e o valor realmente cobrado).

## Testes desta etapa
- Unitários: `FreteCalculatorMock` retorna valores consistentes e determinísticos para os mesmos
  parâmetros de entrada.
- Unitários: CEP inválido/vazio é rejeitado com erro de validação claro.
- Integração: endpoint de cálculo de frete responde corretamente para um carrinho existente.

## Critérios de conclusão
- [ ] `IFreteCalculator` e implementação mock criados e registrados via DI.
- [ ] Endpoint de cálculo de frete funcionando.
- [ ] Decisão sobre recálculo no checkout documentada.
- [ ] Todos os testes passando, build sem erros.

## Documentar e registrar
- Criar `docs/memoria/MEMORIA_ETAPA_05.md`.
- Declarar conclusão explícita e apontar a próxima etapa (`docs/prompts/PROMPT_ETAPA_06.md` — Formas de Pagamento
  e Parcelas).

## Regra fundamental
Não acoplar nenhuma classe de domínio diretamente a uma implementação concreta de frete — tudo deve
passar pela interface `IFreteCalculator`.
