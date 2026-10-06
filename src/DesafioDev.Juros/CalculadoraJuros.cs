namespace DesafioDev.Juros;

/// <summary>Resultado detalhado do cálculo de juros de um título.</summary>
public sealed record ResultadoJuros(
    decimal ValorOriginal,
    int DiasAtraso,
    decimal Juros,
    decimal ValorFinal);

/// <summary>
/// Calcula o valor de multa/juros de um título vencido, aplicando uma
/// taxa fixa de 2,5% por dia de atraso em relação à data de referência.
/// </summary>
public static class CalculadoraJuros
{
    /// <summary>Multa aplicada por dia de atraso.</summary>
    public const decimal TaxaDiaria = 0.025m;

    /// <summary>
    /// Apura os juros de atraso de <paramref name="valor"/> com
    /// vencimento em <paramref name="vencimento"/>, considerando
    /// <paramref name="hoje"/> como data de referência.
    /// As datas são comparadas apenas pelo dia calendário (hora, minuto
    /// e segundo são ignorados). Se ainda não houve atraso, o valor
    /// final é igual ao valor original (sem juros negativos).
    /// </summary>
    public static ResultadoJuros Calcular(decimal valor, DateTime vencimento, DateTime hoje)
    {
        var dataVencimento = vencimento.Date;
        var dataHoje = hoje.Date;

        var dias = (int)(dataHoje - dataVencimento).TotalDays;
        if (dias < 0)
        {
            dias = 0;
        }

        var juros = Math.Round(valor * TaxaDiaria * dias, 2, MidpointRounding.AwayFromZero);
        var valorFinal = valor + juros;

        return new ResultadoJuros(valor, dias, juros, valorFinal);
    }
}
