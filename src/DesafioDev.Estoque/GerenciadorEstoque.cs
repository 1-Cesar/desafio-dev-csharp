namespace DesafioDev.Estoque;

/// <summary>
/// Controla o saldo de cada produto e o histórico de movimentações de
/// um depósito. Permite lançar entradas e saídas, sempre devolvendo o
/// saldo final do produto movimentado. Thread-safe (protegido por lock),
/// pensando em uma eventual evolução para uma API HTTP.
/// </summary>
public sealed class GerenciadorEstoque
{
    private readonly object _lock = new();
    private readonly Dictionary<int, Produto> _produtos;
    private readonly List<Movimentacao> _movimentacoes = new();
    private int _proximoId;

    public GerenciadorEstoque(IEnumerable<Produto> produtosIniciais)
    {
        ArgumentNullException.ThrowIfNull(produtosIniciais);

        _produtos = produtosIniciais.ToDictionary(
            p => p.CodigoProduto,
            p => new Produto
            {
                CodigoProduto = p.CodigoProduto,
                DescricaoProduto = p.DescricaoProduto,
                Estoque = p.Estoque,
            });
    }

    /// <summary>
    /// Lança uma movimentação de entrada ou saída para o produto informado,
    /// valida as regras de negócio e devolve a movimentação já aplicada
    /// (incluindo o saldo final do produto).
    /// </summary>
    public Movimentacao Lancar(int codigoProduto, TipoMovimentacao tipo, int quantidade, string descricao)
    {
        lock (_lock)
        {
            if (!_produtos.TryGetValue(codigoProduto, out var produto))
            {
                throw new ProdutoNaoEncontradoException(codigoProduto);
            }

            if (quantidade <= 0)
            {
                throw new QuantidadeInvalidaException();
            }

            if (string.IsNullOrWhiteSpace(descricao))
            {
                throw new DescricaoObrigatoriaException();
            }

            switch (tipo)
            {
                case TipoMovimentacao.Entrada:
                    produto.Estoque += quantidade;
                    break;

                case TipoMovimentacao.Saida:
                    if (produto.Estoque < quantidade)
                    {
                        throw new EstoqueInsuficienteException(produto.Estoque, quantidade);
                    }
                    produto.Estoque -= quantidade;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de movimentação desconhecido");
            }

            _proximoId++;
            var movimentacao = new Movimentacao(
                Id: _proximoId,
                CodigoProduto: codigoProduto,
                DescricaoProduto: produto.DescricaoProduto,
                Tipo: tipo,
                Quantidade: quantidade,
                Descricao: descricao.Trim(),
                DataHora: DateTime.Now,
                EstoqueResultante: produto.Estoque);

            _movimentacoes.Add(movimentacao);
            return movimentacao;
        }
    }

    /// <summary>Saldo atual de um produto.</summary>
    public int SaldoAtual(int codigoProduto)
    {
        lock (_lock)
        {
            if (!_produtos.TryGetValue(codigoProduto, out var produto))
            {
                throw new ProdutoNaoEncontradoException(codigoProduto);
            }
            return produto.Estoque;
        }
    }

    /// <summary>Histórico completo de movimentações, na ordem em que ocorreram.</summary>
    public IReadOnlyList<Movimentacao> Historico()
    {
        lock (_lock)
        {
            return _movimentacoes.ToList();
        }
    }

    /// <summary>Estado atual de todos os produtos cadastrados.</summary>
    public IReadOnlyList<Produto> Produtos()
    {
        lock (_lock)
        {
            return _produtos.Values
                .Select(p => new Produto { CodigoProduto = p.CodigoProduto, DescricaoProduto = p.DescricaoProduto, Estoque = p.Estoque })
                .OrderBy(p => p.CodigoProduto)
                .ToList();
        }
    }
}
