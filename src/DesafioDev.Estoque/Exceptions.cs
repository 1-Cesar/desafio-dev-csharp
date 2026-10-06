namespace DesafioDev.Estoque;

public sealed class ProdutoNaoEncontradoException : Exception
{
    public ProdutoNaoEncontradoException(int codigoProduto)
        : base($"Produto não encontrado: código {codigoProduto}") { }
}

public sealed class QuantidadeInvalidaException : Exception
{
    public QuantidadeInvalidaException()
        : base("Quantidade deve ser maior que zero") { }
}

public sealed class DescricaoObrigatoriaException : Exception
{
    public DescricaoObrigatoriaException()
        : base("Descrição da movimentação é obrigatória") { }
}

public sealed class EstoqueInsuficienteException : Exception
{
    public EstoqueInsuficienteException(int saldoAtual, int solicitado)
        : base($"Estoque insuficiente para a saída solicitada: saldo atual {saldoAtual}, solicitado {solicitado}") { }
}
