# MEMORIA_ETAPA_05.md — Cálculo de Frete

> Sexta entrada da memória persistente. Segue a estrutura de `MEMORIA_ETAPA_00.md`.
> Etapa executada conforme `docs/prompts/PROMPT_ETAPA_05.md` (+ roadmap 6.3).

## Resumo
- Objetivo da etapa: abstração de cálculo de frete + implementação inicial mockada,
  plugável no checkout, sem acoplar o domínio a fornecedor.
- Status geral: `IFreteCalculator` + `FreteCalculatorMock` + endpoint preliminar
  `POST /api/v1/carrinho/frete`, build sem erros, **63 testes unitários + 31 de
  integração passando**, compose validado. Sem migration (nada persistido).

## Estado atual
- Novos arquivos — Application/Frete: `IFreteCalculator.cs`, `ItemFrete.cs`,
  `CotacaoFrete.cs`, `FreteOptions.cs`, `CepInvalidoException.cs`.
- Infrastructure/Frete: `FreteCalculatorMock.cs`; DI com seleção por configuração
  (`Frete:Provedor`, só "Mock" nesta etapa); pacotes `Options/Configuration/Binder`
  explícitos na Infrastructure.
- API: `Contracts/Carrinho/CalcularFreteRequest.cs`, `FreteResponse.cs`;
  `POST /api/v1/carrinho/frete` no `CarrinhoController`; `ObterAsync` (só-leitura)
  em `ICarrinhoService`; seção `Frete` no `appsettings.json`.
- Não implementado (fora de escopo): provedor real, persistência da cotação,
  finalização (Etapa 07).

## Decisões técnicas
- **Sem abstração reaproveitável (verificado):** grep por frete/CEP/shipping vazio nos
  dois serviços; `Branch` do Identity não tem endereço/CEP. Criada abstração própria;
  `idUnidadeOrigem` aceito na assinatura para uso futuro (mock o ignora, documentado).
- **Mock determinístico e configurável:** valor = `ValorBase` + `ValorPorUnidade` ×
  unidades (12 + 2×N default, via `Frete:ValorBase/ValorPorUnidade`); prazo pela faixa
  do 1º dígito (0-3: 2d úteis, 4-6: 4d, 7-9: 6d); carrinho vazio cota zero. Provisório
  por desenho (sem frete-grátis nem tabela por região — viriam do provedor real).
- **Troca por configuração:** `Frete:Provedor` seleciona a implementação na DI; valor
  desconhecido falha no startup com mensagem clara. Domínio só conhece a interface.
- **Cotação NÃO persistida — recalculada na finalização (recomendação do prompt adotada):**
  evita divergência cotação × cobrança se carrinho/preço mudarem; endpoint exige carrinho
  existente (404 se não há), sem criá-lo.
- **CEP validado no domínio da cotação:** normaliza (só dígitos) e exige 8 dígitos
  (`CepInvalidoException` → 400); DataAnnotations garante presença no request.

## Banco de dados
- Nenhuma migration (nada de frete é persistido).

## Testes
- Unitários (12 novos; 63 no total): determinismo, fórmula, 3 faixas de prazo, vazio = 0,
  5 CEPs inválidos com mensagem, valores via options, itens nulos.
- Integração (3 novos; 31 no total): cotação com carrinho (16, prazo 2, CEP normalizado),
  400 p/ CEP inválido, 404 sem carrinho, 401 sem JWT.
- Comando: `dotnet test PedidosVendas.slnx` → 63 + 31, 0 falhas; build 0 erros.
- Compose: rebuild, `up`, `/health/ready` 200, `identity-*` intactos.

## Problemas
- `services.Configure<T>(IConfiguration)` + `Get<T>()` exigiram 3 pacotes explícitos na
  Infrastructure (`Options`, `Options.ConfigurationExtensions`, `Configuration.Binder`) —
  na API resolviam transitivamente. Só framework, sem nova dependência arquitetural.

## Decisões pendentes
1. ~~Estrutura~~ — 01. 2. ~~Pedido=carrinho~~ — 03. 3. Parcelas por Tenant — **Etapa 06**.
4. ~~Auth~~ — 03. 5. Mensageria — **Etapa 08**. 6. `ProdutosVenda` — **Etapa 07**.
7. Cache (+ MediatR) — **Etapa 09**. 8. Branch ativa → **Etapa 07** (IdUnidade explícito +
   403). 9. Mock produto → HTTP real (exigindo `IdCategoria`) quando o Estoque expuser.
10. **(novo) Provedor real de frete** (Correios/transportadora): nova classe +
    `Frete:Provedor`, reaproveitando `IFreteCalculator`; origem via Unidade quando houver
    endereço de Branch.

## Próximos passos
- Executar `docs/prompts/PROMPT_ETAPA_06.md` — Formas de Pagamento e Parcelas
  (`FormaPagto` + seed, regra de parcelas, limite por Tenant vs fallback).
