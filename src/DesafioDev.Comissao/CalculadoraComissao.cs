namespace DesafioDev.Comissao;

/// <summary>
/// Calcula a comissão de vendedores a partir de um conjunto de vendas,
/// aplicando faixas progressivas por venda individual:
///   - menor que R$ 100,00          -> sem comissão
///   - de R$ 100,00 até R$ 499,99   -> 1%
///   - a partir de R$ 500,00        -> 5%
/// </summary>
public static class CalculadoraComissao
{
    private const decimal LimiteSemComissao = 100.00m;
    private const decimal LimiteComissaoBaixa = 500.00m;

    private const decimal AliquotaBaixa = 0.01m; // 1%
    private const decimal AliquotaAlta = 0.05m;  // 5%

    /// <summary>
    /// Calcula a comissão de uma única venda. O valor é devolvido exato,
    /// sem arredondamento: o enunciado não define regra de arredondamento,
    /// então ele fica restrito à exibição.
    /// </summary>
    public static decimal CalcularComissaoVenda(decimal valor)
    {
        if (valor < LimiteSemComissao)
        {
            return 0m;
        }

        if (valor < LimiteComissaoBaixa)
        {
            return valor * AliquotaBaixa;
        }

        return valor * AliquotaAlta;
    }

    /// <summary>
    /// Consolida as vendas por vendedor, retornando o resultado ordenado
    /// alfabeticamente pelo nome do vendedor para uma saída estável.
    /// </summary>
    public static IReadOnlyList<ResultadoVendedor> Apurar(IEnumerable<Venda> vendas)
    {
        ArgumentNullException.ThrowIfNull(vendas);

        var agrupado = vendas
            .GroupBy(v => v.Vendedor)
            .Select(g => new ResultadoVendedor(
                Vendedor: g.Key,
                QuantidadeVendas: g.Count(),
                TotalVendido: g.Sum(v => v.Valor),
                TotalComissao: g.Sum(v => CalcularComissaoVenda(v.Valor))))
            .OrderBy(r => r.Vendedor, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return agrupado;
    }
}
