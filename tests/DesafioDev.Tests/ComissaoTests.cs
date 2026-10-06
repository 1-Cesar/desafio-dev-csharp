using DesafioDev.Comissao;

namespace DesafioDev.Tests;

public class ComissaoTests
{
    // Valores em string porque decimal não é uma constante válida em atributos.
    [Theory]
    [InlineData("99.99", "0")]     // abaixo de 100 não gera comissão
    [InlineData("0", "0")]         // zero não gera comissão
    [InlineData("100", "1.00")]    // exatamente 100 entra na faixa de 1%
    [InlineData("499.99", "4.9999")] // abaixo de 500 gera 1% (valor exato, sem arredondar)
    [InlineData("250.30", "2.503")]  // idem
    [InlineData("500", "25.00")]   // exatamente 500 entra na faixa de 5%
    [InlineData("1000", "50.00")]  // acima de 500 gera 5%
    public void CalcularComissaoVenda_AplicaFaixaCorreta(string valor, string comissaoEsperada)
    {
        var comissao = CalculadoraComissao.CalcularComissaoVenda(Dec(valor));

        Assert.Equal(Dec(comissaoEsperada), comissao);
    }

    [Fact]
    public void Apurar_AgrupaEOrdenaPorVendedor()
    {
        var vendas = new List<Venda>
        {
            new() { Vendedor = "Ana", Valor = 50m },
            new() { Vendedor = "Ana", Valor = 200m },
            new() { Vendedor = "Ana", Valor = 1000m },
            new() { Vendedor = "Bruno", Valor = 500m },
        };

        var resultados = CalculadoraComissao.Apurar(vendas);

        Assert.Equal(2, resultados.Count);
        Assert.Equal("Ana", resultados[0].Vendedor); // ordem alfabética
        Assert.Equal("Bruno", resultados[1].Vendedor);
        Assert.Equal(3, resultados[0].QuantidadeVendas);
        Assert.Equal(1250m, resultados[0].TotalVendido);
        Assert.Equal(52m, resultados[0].TotalComissao); // 0 + 2 + 50
        Assert.Equal(25m, resultados[1].TotalComissao);
    }

    [Fact]
    public void Apurar_ComListaVazia_NaoLancaExcecao()
    {
        var resultados = CalculadoraComissao.Apurar(new List<Venda>());

        Assert.Empty(resultados);
    }

    private static decimal Dec(string valor) =>
        decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture);
}
