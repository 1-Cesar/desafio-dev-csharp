using System.Text.Json.Serialization;

namespace DesafioDev.Comissao;

/// <summary>Representa uma venda isolada realizada por um vendedor.</summary>
public sealed class Venda
{
    [JsonPropertyName("vendedor")]
    public string Vendedor { get; set; } = string.Empty;

    [JsonPropertyName("valor")]
    public decimal Valor { get; set; }
}

/// <summary>Payload raiz do arquivo de entrada ({"vendas": [...]}).</summary>
public sealed class VendasPayload
{
    [JsonPropertyName("vendas")]
    public List<Venda> Vendas { get; set; } = new();
}

/// <summary>Totais apurados para um vendedor.</summary>
public sealed record ResultadoVendedor(
    string Vendedor,
    int QuantidadeVendas,
    decimal TotalVendido,
    decimal TotalComissao);
