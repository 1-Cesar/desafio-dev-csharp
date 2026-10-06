using System.Globalization;
using System.Text.Json;
using DesafioDev.Comissao;

var caminho = args.Length > 0 ? args[0] : "data/vendas.json";

if (!File.Exists(caminho))
{
    Console.Error.WriteLine($"Arquivo não encontrado: {caminho}");
    return 1;
}

CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");

var json = await File.ReadAllTextAsync(caminho);

var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var payload = JsonSerializer.Deserialize<VendasPayload>(json, options)
    ?? throw new InvalidOperationException("Não foi possível interpretar o arquivo de vendas.");

var resultados = CalculadoraComissao.Apurar(payload.Vendas);

Console.WriteLine("=== Apuração de comissões ===");
decimal totalGeral = 0m;
foreach (var r in resultados)
{
    Console.WriteLine(
        $"{r.Vendedor,-20} {r.QuantidadeVendas,2} vendas | total vendido: R$ {r.TotalVendido,10:N2} | comissão: R$ {r.TotalComissao,8:N2}");
    totalGeral += r.TotalComissao;
}

Console.WriteLine();
Console.WriteLine($"Total geral de comissões: R$ {totalGeral:N2}");
return 0;
