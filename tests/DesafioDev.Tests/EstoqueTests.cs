using DesafioDev.Estoque;

namespace DesafioDev.Tests;

public class EstoqueTests
{
    private static List<Produto> ProdutosBase() => new()
    {
        new Produto { CodigoProduto = 101, DescricaoProduto = "Caneta Azul", Estoque = 150 },
        new Produto { CodigoProduto = 102, DescricaoProduto = "Caderno Universitário", Estoque = 75 },
    };

    [Fact]
    public void Lancar_Entrada_AumentaOEstoque()
    {
        var g = new GerenciadorEstoque(ProdutosBase());

        var mov = g.Lancar(101, TipoMovimentacao.Entrada, 50, "Recebimento de fornecedor");

        Assert.Equal(200, mov.EstoqueResultante);
        Assert.Equal(1, mov.Id);
        Assert.Equal(200, g.SaldoAtual(101));
    }

    [Fact]
    public void Lancar_Saida_ReduzOEstoque()
    {
        var g = new GerenciadorEstoque(ProdutosBase());

        var mov = g.Lancar(102, TipoMovimentacao.Saida, 20, "Venda balcão");

        Assert.Equal(55, mov.EstoqueResultante);
    }

    [Fact]
    public void Lancar_SaidaMaiorQueOSaldo_LancaEstoqueInsuficienteException()
    {
        var g = new GerenciadorEstoque(ProdutosBase());

        Assert.Throws<EstoqueInsuficienteException>(
            () => g.Lancar(102, TipoMovimentacao.Saida, 1000, "Venda grande"));
    }

    [Fact]
    public void Lancar_ProdutoInexistente_LancaProdutoNaoEncontradoException()
    {
        var g = new GerenciadorEstoque(ProdutosBase());

        Assert.Throws<ProdutoNaoEncontradoException>(
            () => g.Lancar(999, TipoMovimentacao.Entrada, 10, "Teste"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Lancar_QuantidadeZeroOuNegativa_LancaQuantidadeInvalidaException(int quantidade)
    {
        var g = new GerenciadorEstoque(ProdutosBase());

        Assert.Throws<QuantidadeInvalidaException>(
            () => g.Lancar(101, TipoMovimentacao.Entrada, quantidade, "Teste"));
    }

    [Fact]
    public void Lancar_DescricaoVazia_LancaDescricaoObrigatoriaException()
    {
        var g = new GerenciadorEstoque(ProdutosBase());

        Assert.Throws<DescricaoObrigatoriaException>(
            () => g.Lancar(101, TipoMovimentacao.Entrada, 10, ""));
    }

    [Fact]
    public void Lancar_GeraIdsUnicosEPreservaOHistorico()
    {
        var g = new GerenciadorEstoque(ProdutosBase());

        var m1 = g.Lancar(101, TipoMovimentacao.Entrada, 10, "Primeira");
        var m2 = g.Lancar(101, TipoMovimentacao.Saida, 5, "Segunda");

        Assert.NotEqual(m1.Id, m2.Id);
        Assert.Equal(2, g.Historico().Count);
    }
}
