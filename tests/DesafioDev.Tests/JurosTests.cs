using DesafioDev.Juros;

namespace DesafioDev.Tests;

public class JurosTests
{
    [Fact]
    public void Calcular_SemAtraso_NaoGeraJuros()
    {
        var hoje = new DateTime(2026, 10, 6);
        var vencimento = new DateTime(2026, 10, 10); // vence no futuro

        var r = CalculadoraJuros.Calcular(1000m, vencimento, hoje);

        Assert.Equal(0, r.DiasAtraso);
        Assert.Equal(0m, r.Juros);
        Assert.Equal(1000m, r.ValorFinal);
    }

    [Fact]
    public void Calcular_VenceHoje_NaoGeraAtraso()
    {
        var hoje = new DateTime(2026, 10, 6);

        var r = CalculadoraJuros.Calcular(1000m, hoje, hoje);

        Assert.Equal(0, r.DiasAtraso);
    }

    [Fact]
    public void Calcular_DezDiasDeAtraso_Calcula25PorCentoDeJuros()
    {
        var hoje = new DateTime(2026, 10, 6);
        var vencimento = new DateTime(2026, 9, 26);

        var r = CalculadoraJuros.Calcular(1000m, vencimento, hoje);

        Assert.Equal(10, r.DiasAtraso);
        Assert.Equal(250m, r.Juros); // 1000 * 0.025 * 10
        Assert.Equal(1250m, r.ValorFinal);
    }

    [Fact]
    public void Calcular_IgnoraHorarioNaComparacaoDeDatas()
    {
        var hoje = new DateTime(2026, 10, 6, 23, 59, 0);
        var vencimento = new DateTime(2026, 10, 5, 0, 1, 0);

        var r = CalculadoraJuros.Calcular(100m, vencimento, hoje);

        Assert.Equal(1, r.DiasAtraso); // conta 1 dia, ignorando o horário
    }
}
