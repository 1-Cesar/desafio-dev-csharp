# Desafio Dev — Target Sistemas

Solução em **C# / .NET 8**, dividida em três
programas independentes — um para cada exercício proposto — além de um
script **SQL Server** (`sql/schema.sql`) modelando a mesma lógica em T-SQL.

## Estrutura do projeto

```
desafio-dev-csharp/
├── DesafioDev.sln
├── src/
│   ├── DesafioDev.Comissao/        # regra de negócio do exercício 1
│   ├── DesafioDev.Comissao.Cli/    # ponto de entrada (console)
│   ├── DesafioDev.Estoque/         # regra de negócio do exercício 2
│   ├── DesafioDev.Estoque.Cli/     # ponto de entrada (console)
│   ├── DesafioDev.Juros/           # regra de negócio do exercício 3
│   └── DesafioDev.Juros.Cli/       # ponto de entrada (console)
├── tests/
│   └── DesafioDev.Tests/           # testes das três regras de negócio
├── data/
│   ├── vendas.json                 # massa de dados do exercício 1
│   └── estoque.json                # massa de dados do exercício 2
└── sql/
    └── schema.sql                  # modelagem equivalente em SQL Server
```

A lógica de cada exercício fica isolada em uma classlib própria (sem
dependência de console nem de I/O), e os projetos `.Cli` são apenas a
camada de interface que consome essa lógica. Isso deixa as regras de
negócio reutilizáveis e fáceis de testar.

## Requisitos

- .NET SDK 8.0+

## Como rodar

### 1. Comissão de vendedores

Lê um JSON de vendas e calcula, por venda:

- `< R$ 100,00` → sem comissão
- `R$ 100,00` a `< R$ 500,00` → 1%
- `>= R$ 500,00` → 5%

```bash
dotnet run --project src/DesafioDev.Comissao.Cli                      # usa data/vendas.json
dotnet run --project src/DesafioDev.Comissao.Cli -- caminho/outro.json
```

### 2. Movimentação de estoque

Carrega o saldo inicial dos produtos e permite lançar movimentações de
entrada/saída interativamente pelo terminal. Cada lançamento recebe um
número identificador único e sequencial (1, 2, 3, ...), guarda a descrição
informada e retorna o saldo final do produto movimentado.

```bash
dotnet run --project src/DesafioDev.Estoque.Cli                       # usa data/estoque.json
```

No prompt, informe linhas no formato:

```
<codigoProduto> <ENTRADA|SAIDA> <quantidade> <descrição>
```

Exemplo:

```
> 101 ENTRADA 50 Recebimento de fornecedor
  OK [movimentação nº 1] 06/10/2026 15:55:54 | Entrada 50 un. de "Caneta Azul" | motivo: "Recebimento de fornecedor" | estoque final: 200
> 102 SAIDA 20 Venda balcão
  OK [movimentação nº 2] ... | estoque final: 55
> sair
```

Regras aplicadas: quantidade deve ser positiva, descrição é
obrigatória, saída não pode deixar o estoque negativo, e o produto
precisa existir no cadastro carregado.

### 3. Juros por atraso

Calcula a multa de um título vencido, considerando 2,5% de multa por
dia de atraso em relação à data atual (sem atraso = sem multa).

```bash
dotnet run --project src/DesafioDev.Juros.Cli -- -valor 1000.00 -vencimento 2026-09-20
```

Se as flags não forem informadas, o programa pergunta interativamente.

## SQL Server

`sql/schema.sql` modela os mesmos três exercícios em T-SQL, caso o
avaliador queira ver a camada de persistência (a vaga exige SQL Server):

- Tabelas `Vendedores`/`Vendas`, função `fn_CalcularComissaoVenda` e view
  `vw_ComissaoPorVendedor` para o exercício 1.
- Tabelas `Produtos`/`Movimentacoes` e a procedure
  `sp_LancarMovimentacaoEstoque` (com transação e lock pessimista via
  `UPDLOCK`/`ROWLOCK`) para o exercício 2.
- Função de tabela `fn_CalcularJuros` para o exercício 3.

Os programas em C# rodam totalmente em memória (não precisam de banco
para serem avaliados), mas a lógica de negócio é a mesma dos dois lados.

## Testes

```bash
dotnet test
```

Os testes ficam em `tests/DesafioDev.Tests` e usam **xUnit**, cobrindo as
três regras de negócio (`ComissaoTests`, `EstoqueTests` e `JurosTests`).
Para rodar apenas uma classe ou ver o resultado caso a caso:

```bash
dotnet test --filter "FullyQualifiedName~JurosTests"
dotnet test --logger "console;verbosity=detailed"
```

## Decisões e premissas

- **Comissão**: a regra é aplicada por venda individual (não sobre o
  total acumulado do vendedor), seguindo a redação do enunciado. Os
  limites de R$ 100 e R$ 500 são tratados como inclusivos na faixa
  superior (ex.: uma venda de exatamente R$ 500 já entra na faixa de 5%).
  A comissão é calculada com o valor exato (sem arredondar venda a venda,
  pois o enunciado não define essa regra); o arredondamento para duas
  casas acontece apenas na exibição.
- **Estoque**: além do que o enunciado pede (número identificador único,
  descrição e retorno do estoque final), foram adicionadas validações
  que ele não exige: quantidade positiva, descrição obrigatória, produto
  existente e saída limitada ao saldo. `GerenciadorEstoque` guarda o estoque em memória e é
  thread-safe (protegido por `lock`), pensando em uma eventual evolução
  para uma API HTTP/ASP.NET. O histórico completo de movimentações fica
  disponível via `Historico()`.
- **Juros**: a comparação de datas ignora o horário (considera apenas o
  dia calendário) e usa o dia do vencimento como dia 0 (vence hoje = 0
  dias de atraso, vence ontem = 1 dia de atraso).
- Tipos monetários usam `decimal` (não `double`/`float`), como é
  recomendado em C# para evitar erros de arredondamento.

## Autor

Cesar Augusto Bandeira Pereira
