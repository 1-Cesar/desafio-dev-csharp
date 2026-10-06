-- =============================================================================
-- Desafio Dev — modelagem em SQL Server
--
-- A vaga pede SQL Server como banco principal. Os programas em C# deste
-- repositório rodam de forma independente (em memória, sem precisar de
-- banco) para manter o desafio simples de executar, mas este script mostra
-- como os mesmos dados/operações se modelam em T-SQL, caso o avaliador
-- queira ver a camada de persistência.
-- =============================================================================

IF DB_ID('DesafioDev') IS NULL
BEGIN
    CREATE DATABASE DesafioDev;
END
GO

USE DesafioDev;
GO

-- ------------------------------------------------------------------
-- Exercício 1: comissão de vendas
-- ------------------------------------------------------------------
-- Vendas referencia Vendedores (FK), então precisa ser removida antes.
IF OBJECT_ID('dbo.Vendas', 'U') IS NOT NULL DROP TABLE dbo.Vendas;
IF OBJECT_ID('dbo.Vendedores', 'U') IS NOT NULL DROP TABLE dbo.Vendedores;
CREATE TABLE dbo.Vendedores
(
    VendedorId INT IDENTITY(1,1) PRIMARY KEY,
    Nome       NVARCHAR(100) NOT NULL UNIQUE
);
GO

CREATE TABLE dbo.Vendas
(
    VendaId    INT IDENTITY(1,1) PRIMARY KEY,
    VendedorId INT NOT NULL REFERENCES dbo.Vendedores(VendedorId),
    Valor      DECIMAL(12,2) NOT NULL CHECK (Valor >= 0),
    DataVenda  DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

-- Função que aplica a regra de comissão por venda individual:
--   < 100         -> 0%
--   100 a < 500   -> 1%
--   >= 500        -> 5%
IF OBJECT_ID('dbo.fn_CalcularComissaoVenda', 'FN') IS NOT NULL DROP FUNCTION dbo.fn_CalcularComissaoVenda;
GO
CREATE FUNCTION dbo.fn_CalcularComissaoVenda (@Valor DECIMAL(12,2))
RETURNS DECIMAL(14,4) -- valor exato; arredondamento só na exibição
AS
BEGIN
    RETURN CASE
        WHEN @Valor < 100.00 THEN 0.00
        WHEN @Valor < 500.00 THEN @Valor * 0.01
        ELSE @Valor * 0.05
    END;
END;
GO

-- View com o total apurado por vendedor (equivalente ao CalculadoraComissao.Apurar).
IF OBJECT_ID('dbo.vw_ComissaoPorVendedor', 'V') IS NOT NULL DROP VIEW dbo.vw_ComissaoPorVendedor;
GO
CREATE VIEW dbo.vw_ComissaoPorVendedor AS
SELECT
    ve.Nome                                        AS Vendedor,
    COUNT(*)                                        AS QuantidadeVendas,
    SUM(v.Valor)                                     AS TotalVendido,
    SUM(dbo.fn_CalcularComissaoVenda(v.Valor))       AS TotalComissao
FROM dbo.Vendas v
JOIN dbo.Vendedores ve ON ve.VendedorId = v.VendedorId
GROUP BY ve.Nome;
GO

-- ------------------------------------------------------------------
-- Exercício 2: movimentação de estoque
-- ------------------------------------------------------------------
-- Movimentacoes referencia Produtos (FK), então precisa ser removida antes.
IF OBJECT_ID('dbo.Movimentacoes', 'U') IS NOT NULL DROP TABLE dbo.Movimentacoes;
IF OBJECT_ID('dbo.Produtos', 'U') IS NOT NULL DROP TABLE dbo.Produtos;
CREATE TABLE dbo.Produtos
(
    CodigoProduto    INT PRIMARY KEY,
    DescricaoProduto NVARCHAR(200) NOT NULL,
    Estoque          INT NOT NULL CHECK (Estoque >= 0)
);
GO

CREATE TABLE dbo.Movimentacoes
(
    MovimentacaoId     INT IDENTITY(1,1) PRIMARY KEY, -- número identificador único
    CodigoProduto      INT NOT NULL REFERENCES dbo.Produtos(CodigoProduto),
    Tipo               VARCHAR(10) NOT NULL CHECK (Tipo IN ('ENTRADA', 'SAIDA')),
    Quantidade         INT NOT NULL CHECK (Quantidade > 0),
    Descricao          NVARCHAR(400) NOT NULL,
    DataHora           DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    EstoqueResultante  INT NOT NULL
);
GO

-- Procedure que lança uma movimentação, valida as regras de negócio
-- (produto existe, saldo suficiente para saída) e retorna o estoque final,
-- igual ao comportamento de GerenciadorEstoque.Lancar em C#.
IF OBJECT_ID('dbo.sp_LancarMovimentacaoEstoque', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_LancarMovimentacaoEstoque;
GO
CREATE PROCEDURE dbo.sp_LancarMovimentacaoEstoque
    @CodigoProduto INT,
    @Tipo          VARCHAR(10),
    @Quantidade    INT,
    @Descricao     NVARCHAR(400),
    @EstoqueFinal  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Quantidade <= 0
    BEGIN
        RAISERROR('Quantidade deve ser maior que zero', 16, 1);
        RETURN;
    END

    IF @Descricao IS NULL OR LTRIM(RTRIM(@Descricao)) = ''
    BEGIN
        RAISERROR('Descrição da movimentação é obrigatória', 16, 1);
        RETURN;
    END

    BEGIN TRANSACTION;

    DECLARE @EstoqueAtual INT;
    SELECT @EstoqueAtual = Estoque FROM dbo.Produtos WITH (UPDLOCK, ROWLOCK) WHERE CodigoProduto = @CodigoProduto;

    IF @EstoqueAtual IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR('Produto não encontrado: código %d', 16, 1, @CodigoProduto);
        RETURN;
    END

    IF @Tipo = 'ENTRADA'
        SET @EstoqueFinal = @EstoqueAtual + @Quantidade;
    ELSE IF @Tipo = 'SAIDA'
    BEGIN
        IF @EstoqueAtual < @Quantidade
        BEGIN
            ROLLBACK TRANSACTION;
            RAISERROR('Estoque insuficiente: saldo atual %d, solicitado %d', 16, 1, @EstoqueAtual, @Quantidade);
            RETURN;
        END
        SET @EstoqueFinal = @EstoqueAtual - @Quantidade;
    END
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR('Tipo de movimentação inválido (use ENTRADA ou SAIDA)', 16, 1);
        RETURN;
    END

    UPDATE dbo.Produtos SET Estoque = @EstoqueFinal WHERE CodigoProduto = @CodigoProduto;

    INSERT INTO dbo.Movimentacoes (CodigoProduto, Tipo, Quantidade, Descricao, EstoqueResultante)
    VALUES (@CodigoProduto, @Tipo, @Quantidade, @Descricao, @EstoqueFinal);

    COMMIT TRANSACTION;
END;
GO

-- ------------------------------------------------------------------
-- Exercício 3: juros por atraso
-- ------------------------------------------------------------------
IF OBJECT_ID('dbo.fn_CalcularJuros', 'IF') IS NOT NULL DROP FUNCTION dbo.fn_CalcularJuros;
GO
CREATE FUNCTION dbo.fn_CalcularJuros (@Valor DECIMAL(12,2), @Vencimento DATE, @Hoje DATE)
RETURNS TABLE
AS
RETURN
(
    SELECT
        @Valor                                                              AS ValorOriginal,
        DiasAtraso = CASE WHEN DATEDIFF(DAY, @Vencimento, @Hoje) < 0 THEN 0
                          ELSE DATEDIFF(DAY, @Vencimento, @Hoje) END,
        Juros = ROUND(@Valor * 0.025 *
                      (CASE WHEN DATEDIFF(DAY, @Vencimento, @Hoje) < 0 THEN 0
                            ELSE DATEDIFF(DAY, @Vencimento, @Hoje) END), 2),
        ValorFinal = @Valor + ROUND(@Valor * 0.025 *
                      (CASE WHEN DATEDIFF(DAY, @Vencimento, @Hoje) < 0 THEN 0
                            ELSE DATEDIFF(DAY, @Vencimento, @Hoje) END), 2)
);
GO

-- ------------------------------------------------------------------
-- Massa de dados inicial (mesmos dados dos arquivos JSON em /data)
-- ------------------------------------------------------------------
INSERT INTO dbo.Vendedores (Nome) VALUES (N'João Silva'), (N'Maria Souza'), (N'Carlos Oliveira'), (N'Ana Lima');

INSERT INTO dbo.Vendas (VendedorId, Valor)
SELECT ve.VendedorId, d.Valor
FROM (VALUES
    (N'João Silva', 1200.50),
    (N'João Silva', 950.75),
    (N'João Silva', 1800.00),
    (N'João Silva', 1400.30),
    (N'João Silva', 1100.90),
    (N'João Silva', 1550.00),
    (N'João Silva', 1700.80),
    (N'João Silva', 250.30),
    (N'João Silva', 480.75),
    (N'João Silva', 320.40),
    (N'Maria Souza', 2100.40),
    (N'Maria Souza', 1350.60),
    (N'Maria Souza', 950.20),
    (N'Maria Souza', 1600.75),
    (N'Maria Souza', 1750.00),
    (N'Maria Souza', 1450.90),
    (N'Maria Souza', 400.50),
    (N'Maria Souza', 180.20),
    (N'Maria Souza', 90.75),
    (N'Carlos Oliveira', 800.50),
    (N'Carlos Oliveira', 1200.00),
    (N'Carlos Oliveira', 1950.30),
    (N'Carlos Oliveira', 1750.80),
    (N'Carlos Oliveira', 1300.60),
    (N'Carlos Oliveira', 300.40),
    (N'Carlos Oliveira', 500.00),
    (N'Carlos Oliveira', 125.75),
    (N'Ana Lima', 1000.00),
    (N'Ana Lima', 1100.50),
    (N'Ana Lima', 1250.75),
    (N'Ana Lima', 1400.20),
    (N'Ana Lima', 1550.90),
    (N'Ana Lima', 1650.00),
    (N'Ana Lima', 75.30),
    (N'Ana Lima', 420.90),
    (N'Ana Lima', 315.40)
) AS d (Vendedor, Valor)
JOIN dbo.Vendedores ve ON ve.Nome = d.Vendedor;

INSERT INTO dbo.Produtos (CodigoProduto, DescricaoProduto, Estoque) VALUES
    (101, 'Caneta Azul', 150),
    (102, N'Caderno Universitário', 75),
    (103, 'Borracha Branca', 200),
    (104, N'Lápis Preto HB', 320),
    (105, 'Marcador de Texto Amarelo', 90);
GO

-- Exemplos de uso:
-- SELECT * FROM dbo.vw_ComissaoPorVendedor ORDER BY Vendedor;
--
-- DECLARE @Final INT;
-- EXEC dbo.sp_LancarMovimentacaoEstoque 101, 'ENTRADA', 50, 'Recebimento de fornecedor', @Final OUTPUT;
-- SELECT @Final AS EstoqueFinal;
--
-- SELECT * FROM dbo.fn_CalcularJuros(1000.00, '2026-09-20', CAST(GETDATE() AS DATE));
