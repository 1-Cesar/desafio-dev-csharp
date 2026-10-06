using System.Text.Json.Serialization;

namespace DesafioDev.Estoque;

/// <summary>Representa um item do depósito e seu saldo atual.</summary>
public sealed class Produto
{
    [JsonPropertyName("codigoProduto")]
    public int CodigoProduto { get; set; }

    [JsonPropertyName("descricaoProduto")]
    public string DescricaoProduto { get; set; } = string.Empty;

    [JsonPropertyName("estoque")]
    public int Estoque { get; set; }
}

/// <summary>Payload raiz do arquivo de entrada ({"estoque": [...]}).</summary>
public sealed class EstoquePayload
{
    [JsonPropertyName("estoque")]
    public List<Produto> Estoque { get; set; } = new();
}

/// <summary>Tipo de movimentação de estoque.</summary>
public enum TipoMovimentacao
{
    Entrada,
    Saida,
}

/// <summary>Registro de uma movimentação já aplicada ao estoque.</summary>
public sealed record Movimentacao(
    int Id,
    int CodigoProduto,
    string DescricaoProduto,
    TipoMovimentacao Tipo,
    int Quantidade,
    string Descricao,
    DateTime DataHora,
    int EstoqueResultante);
