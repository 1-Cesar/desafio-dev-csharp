using System.Globalization;
using DesafioDev.Juros;

const string FormatoData = "yyyy-MM-dd";

CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");

var opcoes = ParseArgs(args);

var valor = opcoes.TryGetValue("valor", out var valorTexto) && decimal.TryParse(
    valorTexto.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var v) && v > 0
    ? v
    : LerValor();

var vencimentoTexto = opcoes.TryGetValue("vencimento", out var venc) && venc.Length > 0
    ? venc
    : LerTexto("Data de vencimento (AAAA-MM-DD): ");

if (!DateTime.TryParseExact(vencimentoTexto.Trim(), FormatoData, CultureInfo.InvariantCulture, DateTimeStyles.None, out var vencimento))
{
    Console.Error.WriteLine("Data de vencimento inválida, use o formato AAAA-MM-DD.");
    return 1;
}

var resultado = CalculadoraJuros.Calcular(valor, vencimento, DateTime.Now);

Console.WriteLine("=== Cálculo de juros por atraso ===");
Console.WriteLine($"Valor original:    R$ {resultado.ValorOriginal:N2}");
Console.WriteLine($"Vencimento:        {vencimento:dd/MM/yyyy}");
Console.WriteLine($"Dias em atraso:    {resultado.DiasAtraso}");
Console.WriteLine($"Multa (2,5%/dia):  R$ {resultado.Juros:N2}");
Console.WriteLine($"Valor final:       R$ {resultado.ValorFinal:N2}");
return 0;

static Dictionary<string, string> ParseArgs(string[] args)
{
    var resultado = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < args.Length; i++)
    {
        if (!args[i].StartsWith("-valor", StringComparison.OrdinalIgnoreCase)
            && !args[i].StartsWith("-vencimento", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        var chave = args[i].TrimStart('-');
        if (i + 1 < args.Length)
        {
            resultado[chave] = args[i + 1];
            i++;
        }
    }
    return resultado;
}

static string LerTexto(string prompt)
{
    Console.Write(prompt);
    return (Console.ReadLine() ?? string.Empty).Trim();
}

static decimal LerValor()
{
    while (true)
    {
        var texto = LerTexto("Valor original: ").Replace(',', '.');
        if (decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var valor) && valor > 0)
        {
            return valor;
        }
        Console.WriteLine("  valor inválido, tente novamente.");
    }
}
