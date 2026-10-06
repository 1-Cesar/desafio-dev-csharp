using System.Text.Json;
using DesafioDev.Estoque;

var caminho = args.Length > 0 ? args[0] : "data/estoque.json";

if (!File.Exists(caminho))
{
    Console.Error.WriteLine($"Arquivo não encontrado: {caminho}");
    return 1;
}

var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var json = await File.ReadAllTextAsync(caminho);
var payload = JsonSerializer.Deserialize<EstoquePayload>(json, options)
    ?? throw new InvalidOperationException("Não foi possível interpretar o arquivo de estoque.");

var gerenciador = new GerenciadorEstoque(payload.Estoque);

Console.WriteLine("=== Lançamento de movimentações de estoque ===");
Console.WriteLine("Produtos carregados:");
foreach (var p in payload.Estoque)
{
    Console.WriteLine($"  [{p.CodigoProduto}] {p.DescricaoProduto,-30} estoque atual: {p.Estoque}");
}

Console.WriteLine();
Console.WriteLine("Formato de cada lançamento:");
Console.WriteLine("  <codigoProduto> <ENTRADA|SAIDA> <quantidade> <descrição>");
Console.WriteLine("Exemplo: 101 ENTRADA 50 Recebimento de fornecedor");
Console.WriteLine("Digite 'sair' para encerrar.");
Console.WriteLine();

while (true)
{
    Console.Write("> ");
    var linha = Console.ReadLine();
    if (linha is null)
    {
        break;
    }

    linha = linha.Trim();
    if (linha.Length == 0)
    {
        continue;
    }

    if (string.Equals(linha, "sair", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    try
    {
        var mov = ProcessarLinha(gerenciador, linha);
        Console.WriteLine(
            $"  OK [movimentação nº {mov.Id}] {mov.DataHora:dd/MM/yyyy HH:mm:ss} | {mov.Tipo} {mov.Quantidade} un. de \"{mov.DescricaoProduto}\" | motivo: \"{mov.Descricao}\" | estoque final: {mov.EstoqueResultante}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  erro: {ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine("=== Resumo final de estoque ===");
foreach (var p in gerenciador.Produtos())
{
    Console.WriteLine($"  [{p.CodigoProduto}] {p.DescricaoProduto,-30} estoque final: {p.Estoque}");
}

return 0;

static Movimentacao ProcessarLinha(GerenciadorEstoque gerenciador, string linha)
{
    var partes = linha.Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
    if (partes.Length < 4)
    {
        throw new ArgumentException("Formato inválido, esperado: <codigo> <ENTRADA|SAIDA> <quantidade> <descrição>");
    }

    if (!int.TryParse(partes[0], out var codigo))
    {
        throw new ArgumentException($"Código de produto inválido: \"{partes[0]}\"");
    }

    if (!NormalizarTipo(partes[1], out var tipo))
    {
        throw new ArgumentException($"Tipo inválido: \"{partes[1]}\" (use ENTRADA ou SAIDA)");
    }

    if (!int.TryParse(partes[2], out var quantidade))
    {
        throw new ArgumentException($"Quantidade inválida: \"{partes[2]}\"");
    }

    var descricao = partes[3];

    return gerenciador.Lancar(codigo, tipo, quantidade, descricao);
}

static bool NormalizarTipo(string texto, out TipoMovimentacao tipo)
{
    switch (texto.Trim().ToUpperInvariant())
    {
        case "ENTRADA":
            tipo = TipoMovimentacao.Entrada;
            return true;
        case "SAIDA":
        case "SAÍDA":
            tipo = TipoMovimentacao.Saida;
            return true;
        default:
            tipo = default;
            return false;
    }
}
