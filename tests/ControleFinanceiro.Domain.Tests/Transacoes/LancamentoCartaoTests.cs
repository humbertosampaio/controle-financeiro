using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Transacoes;

public sealed class LancamentoCartaoTests
{
    private const string TextoQueNormalizaParaVazio = "́";

    [Theory, DomainAutoData]
    public void Criar_Quando_ContraparteECategoriaComEspacosNasPontas_Deve_Aparar(
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao,
        string contraparte,
        string categoriaBanco,
        IdentidadeLancamento identidade)
    {
        LancamentoCartao result = LancamentoCartao.Criar(data, hora: null, valor, descricao, $" {contraparte} ", parcela: null, $" {categoriaBanco} ", identidade);

        result.Contraparte.Should().Be(contraparte);
        result.CategoriaBanco.Should().Be(categoriaBanco);
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_GuardarAParcela(
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao,
        Parcela parcela,
        IdentidadeLancamento identidade)
    {
        LancamentoCartao result = LancamentoCartao.Criar(data, hora: null, valor, descricao, contraparte: null, parcela, categoriaBanco: null, identidade);

        result.Parcela.Should().Be(parcela);
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_NaoMarcarComoPagamentoFatura(DateOnly data, Reais valor, TextoObrigatorio descricao, IdentidadeLancamento identidade)
    {
        LancamentoCartao result = LancamentoCartao.Criar(data, hora: null, valor, descricao, contraparte: null, parcela: null, categoriaBanco: null, identidade);

        result.EhPagamentoFaturaAnterior.Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_MesmosCampos_Deve_SerIgual(
        DateOnly data,
        TimeOnly hora,
        Reais valor,
        TextoObrigatorio descricao,
        string contraparte,
        Parcela parcela,
        string categoriaBanco,
        IdentidadeLancamento identidade)
    {
        LancamentoCartao lancamento = LancamentoCartao.Criar(data, hora, valor, descricao, contraparte, parcela, categoriaBanco, identidade);

        LancamentoCartao outro = LancamentoCartao.Criar(data, hora, valor, descricao, contraparte, parcela, categoriaBanco, identidade);

        outro.Should().Be(lancamento);
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_LinhaDaContaComOsMesmosCampos_Deve_SerDiferente(
        DateOnly data,
        TimeOnly hora,
        Reais valor,
        TextoObrigatorio descricao,
        string contraparte,
        string categoriaBanco,
        IdentidadeLancamento identidade)
    {
        LancamentoCartao lancamento = LancamentoCartao.Criar(data, hora, valor, descricao, contraparte, parcela: null, categoriaBanco, identidade);

        LancamentoConta daConta = LancamentoConta.Criar(data, hora, valor, descricao, contraparte, categoriaBanco, identidade);

        daConta.Should().NotBe(lancamento);
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_SoAMarcaDePagamentoFaturaDifere_Deve_SerDiferente(LancamentoCartao lancamento)
    {
        LancamentoCartao entrada = ComValor(lancamento, lancamento.ValorAbsoluto);

        LancamentoCartao pagamento = entrada.ComoPagamentoFaturaAnterior();

        pagamento.Should().NotBe(entrada);
    }

    [Theory, DomainAutoData]
    public void TextoBeneficiario_Quando_Saida_Deve_RetornarADescricao(LancamentoCartao lancamento, ReaisPositivo valor)
    {
        LancamentoCartao compra = ComValor(lancamento, -valor);

        compra.TextoBeneficiario.Should().Be(lancamento.Descricao);
    }

    [Theory]
    [InlineDomainAutoData(10)]
    [InlineDomainAutoData(0)]
    public void TextoBeneficiario_Quando_EntradaOuValorZero_Deve_RetornarNulo(decimal valor, LancamentoCartao lancamento)
    {
        LancamentoCartao sut = ComValor(lancamento, valor);

        sut.TextoBeneficiario.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void AliasTextoBeneficiario_Quando_Saida_Deve_RetornarOAliasDaDescricao(LancamentoCartao lancamento, ReaisPositivo valor)
    {
        LancamentoCartao compra = ComValor(lancamento, -valor);

        compra.AliasTextoBeneficiario.Should().Be((Alias)lancamento.Descricao.Value);
    }

    [Theory]
    [InlineDomainAutoData(10)]
    [InlineDomainAutoData(0)]
    public void AliasTextoBeneficiario_Quando_EntradaOuValorZero_Deve_RetornarNulo(decimal valor, LancamentoCartao lancamento)
    {
        LancamentoCartao sut = ComValor(lancamento, valor);

        sut.AliasTextoBeneficiario.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void AliasTextoBeneficiario_Quando_DescricaoNormalizaParaVazio_Deve_RetornarNulo(LancamentoCartao lancamento, ReaisPositivo valor)
    {
        LancamentoCartao compra = ComValor(lancamento, -valor, descricao: (TextoObrigatorio)TextoQueNormalizaParaVazio);

        compra.AliasTextoBeneficiario.Should().BeNull();
    }

    [Theory]
    [InlineDomainAutoData(-10)]
    [InlineDomainAutoData(10)]
    public void AliasCategoriaBanco_Quando_TemCategoria_Deve_RetornarOAliasEmSaidaEEntrada(decimal valor, LancamentoCartao lancamento, string categoriaBanco)
    {
        LancamentoCartao comCategoria = ComValor(lancamento, valor, categoriaBanco: categoriaBanco);

        comCategoria.AliasCategoriaBanco.Should().Be((Alias)categoriaBanco);
    }

    [Theory, DomainAutoData]
    public void AliasCategoriaBanco_Quando_SemCategoria_Deve_RetornarNulo(LancamentoCartao lancamento)
    {
        LancamentoCartao semCategoria = ComValor(lancamento, lancamento.Valor);

        semCategoria.AliasCategoriaBanco.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void AliasCategoriaBanco_Quando_TextoNormalizaParaVazio_Deve_RetornarNulo(LancamentoCartao lancamento)
    {
        LancamentoCartao comCategoria = ComValor(lancamento, lancamento.Valor, categoriaBanco: TextoQueNormalizaParaVazio);

        comCategoria.AliasCategoriaBanco.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void EhCompraParcelada_Quando_SaidaComParcela_Deve_SerVerdadeiro(LancamentoCartao lancamento, ReaisPositivo valor, Parcela parcela)
    {
        LancamentoCartao compra = ComValor(lancamento, -valor, parcela: parcela);

        compra.EhCompraParcelada.Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void EhCompraParcelada_Quando_AVista_Deve_SerFalso(LancamentoCartao lancamento, ReaisPositivo valor)
    {
        LancamentoCartao compra = ComValor(lancamento, -valor);

        compra.EhCompraParcelada.Should().BeFalse();
    }

    [Theory]
    [InlineDomainAutoData(10)]
    [InlineDomainAutoData(0)]
    public void EhCompraParcelada_Quando_NaoEhSaida_Deve_SerFalso(decimal valor, LancamentoCartao lancamento, Parcela parcela)
    {
        LancamentoCartao naoSaida = ComValor(lancamento, valor, parcela: parcela);

        naoSaida.EhCompraParcelada.Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void EhCompraParcelada_Quando_DescricaoNormalizaParaVazio_Deve_SerFalso(LancamentoCartao lancamento, ReaisPositivo valor, Parcela parcela)
    {
        LancamentoCartao compra = ComValor(lancamento, -valor, descricao: (TextoObrigatorio)TextoQueNormalizaParaVazio, parcela: parcela);

        compra.EhCompraParcelada.Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void EhAVista_Quando_SemParcela_Deve_SerVerdadeiro(LancamentoCartao lancamento)
    {
        lancamento.NaFatura(lancamento.Data, parcela: null).EhAVista.Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void EhAVista_Quando_ComParcela_Deve_SerFalso(LancamentoCartao lancamento, Parcela parcela)
    {
        lancamento.NaFatura(lancamento.Data, parcela).EhAVista.Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void PrimeiroMesVencimentoPossivel_Quando_AVista_Deve_SerOMesDaData(LancamentoCartao lancamento)
    {
        LancamentoCartao sut = lancamento.NaFatura(lancamento.Data, parcela: null);

        sut.PrimeiroMesVencimentoPossivel.Should().Be((MesReferencia)sut.Data);
    }

    [Theory, DomainAutoData]
    public void PrimeiroMesVencimentoPossivel_Quando_Parcela_Deve_AvancarUmMesPorParcelaAnterior(LancamentoCartao lancamento)
    {
        LancamentoCartao sut = lancamento.NaFatura(new DateOnly(2025, 7, 15), (Parcela)(9, 10));

        sut.PrimeiroMesVencimentoPossivel.Should().Be((MesReferencia)(03, 2026));
    }

    [Theory, DomainAutoData]
    public void PagamentoFaturaAnterior_Deve_MarcarComoPagamentoFaturaSemParcela(LancamentoCartao lancamento)
    {
        LancamentoCartao result = lancamento.ComoPagamentoFaturaAnterior();

        result.EhPagamentoFaturaAnterior.Should().BeTrue();
        result.Parcela.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void PagamentoFaturaAnterior_Deve_SerEntrada(
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade)
    {
        LancamentoCartao result = LancamentoCartao.PagamentoFaturaAnterior(data, hora: null, valor, descricao, contraparte: null, categoriaBanco: null, identidade);

        result.EhEntrada.Should().BeTrue();
        result.Valor.Should().Be((Reais)valor.Value);
    }

    [Theory, DomainAutoData]
    public void PagamentoFaturaAnterior_Quando_ValorZero_Deve_NaoSerEntradaNemSaida(
        DateOnly data,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade)
    {
        LancamentoCartao result = LancamentoCartao.PagamentoFaturaAnterior(data, hora: null, valor: (ReaisPositivo)0m, descricao, contraparte: null, categoriaBanco: null, identidade);

        result.EhEntrada.Should().BeFalse();
        result.EhSaida.Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void PagamentoFaturaAnterior_Quando_ContraparteECategoriaComEspacosNasPontas_Deve_Aparar(
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string contraparte,
        string categoriaBanco,
        IdentidadeLancamento identidade)
    {
        LancamentoCartao result = LancamentoCartao.PagamentoFaturaAnterior(data, hora: null, valor, descricao, $" {contraparte} ", $" {categoriaBanco} ", identidade);

        result.Contraparte.Should().Be(contraparte);
        result.CategoriaBanco.Should().Be(categoriaBanco);
    }

    [Theory, DomainAutoData]
    public void PagamentoFaturaAnterior_Deve_GuardarAIdentidadeRecebida(
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade)
    {
        LancamentoCartao result = LancamentoCartao.PagamentoFaturaAnterior(data, hora: null, valor, descricao, contraparte: null, categoriaBanco: null, identidade);

        result.Identidade.Should().Be(identidade);
    }

    /// <summary>A linha de fatura com o valor pedido e, além da descrição, só os campos opcionais que o caso informa.</summary>
    private static LancamentoCartao ComValor(
        LancamentoCartao lancamento,
        Reais valor,
        TextoObrigatorio? descricao = null,
        Parcela? parcela = null,
        string? categoriaBanco = null)
    {
        return LancamentoCartao.Criar(lancamento.Data, lancamento.Hora, valor, descricao ?? lancamento.Descricao, contraparte: null, parcela, categoriaBanco, lancamento.Identidade);
    }
}
