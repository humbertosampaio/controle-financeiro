using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Transacoes;

public sealed class LancamentoTests
{
    private const string TextoQueNormalizaParaVazio = "\u0301";

    [Theory]
    [InlineDomainAutoData(null)]
    [InlineDomainAutoData("")]
    [InlineDomainAutoData("   ")]
    public void DaConta_Quando_ContraparteEmBranco_Deve_DeixarNula(
        string? contraparte,
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade)
    {
        Lancamento result = Lancamento.DaConta(data, hora: null, valor, descricao, contraparte, categoriaBanco: null, identidade);

        result.Contraparte.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void DaConta_Quando_ContraparteComEspacosNasPontas_Deve_Aparar(
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        Lancamento result = Lancamento.DaConta(data, hora: null, valor, descricao, $" {contraparte} ", categoriaBanco: null, identidade);

        result.Contraparte.Should().Be(contraparte);
    }

    [Theory]
    [InlineDomainAutoData(null)]
    [InlineDomainAutoData("")]
    [InlineDomainAutoData("   ")]
    public void DaConta_Quando_CategoriaBancoEmBranco_Deve_DeixarNula(
        string? categoriaBanco,
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade)
    {
        Lancamento result = Lancamento.DaConta(data, hora: null, valor, descricao, contraparte: null, categoriaBanco, identidade);

        result.CategoriaBanco.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void DaConta_Quando_CategoriaBancoComEspacosNasPontas_Deve_Aparar(
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao,
        string categoriaBanco,
        IdentidadeLancamento identidade)
    {
        Lancamento result = Lancamento.DaConta(data, hora: null, valor, descricao, contraparte: null, $" {categoriaBanco} ", identidade);

        result.CategoriaBanco.Should().Be(categoriaBanco);
    }

    [Theory, DomainAutoData]
    public void DoCartao_Quando_ContraparteECategoriaComEspacosNasPontas_Deve_Aparar(
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao,
        string contraparte,
        string categoriaBanco,
        IdentidadeLancamento identidade)
    {
        Lancamento result = Lancamento.DoCartao(data, hora: null, valor, descricao, $" {contraparte} ", parcela: null, $" {categoriaBanco} ", identidade);

        result.Contraparte.Should().Be(contraparte);
        result.CategoriaBanco.Should().Be(categoriaBanco);
    }

    [Theory, DomainAutoData]
    public void DoCartao_Deve_GuardarAParcela(
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao,
        Parcela parcela,
        IdentidadeLancamento identidade)
    {
        Lancamento result = Lancamento.DoCartao(data, hora: null, valor, descricao, contraparte: null, parcela, categoriaBanco: null, identidade);

        result.Parcela.Should().Be(parcela);
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
        Lancamento lancamento = Lancamento.DoCartao(data, hora, valor, descricao, contraparte, parcela, categoriaBanco, identidade);

        Lancamento outro = Lancamento.DoCartao(data, hora, valor, descricao, contraparte, parcela, categoriaBanco, identidade);

        outro.Should().Be(lancamento);
    }

    [Theory]
    [InlineDomainAutoData(-0.01, true)]
    [InlineDomainAutoData(0, false)]
    [InlineDomainAutoData(0.01, false)]
    public void EhSaida_Deve_SerVerdadeiroSoComValorNegativo(
        decimal valor,
        bool esperado,
        DateOnly data,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade)
    {
        Lancamento lancamento = Lancamento.DaConta(data, hora: null, valor, descricao, contraparte: null, categoriaBanco: null, identidade);

        lancamento.EhSaida.Should().Be(esperado);
    }

    [Theory]
    [InlineDomainAutoData(-0.01, false)]
    [InlineDomainAutoData(0, false)]
    [InlineDomainAutoData(0.01, true)]
    public void EhEntrada_Deve_SerVerdadeiroSoComValorPositivo(
        decimal valor,
        bool esperado,
        DateOnly data,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade)
    {
        Lancamento lancamento = Lancamento.DaConta(data, hora: null, valor, descricao, contraparte: null, categoriaBanco: null, identidade);

        lancamento.EhEntrada.Should().Be(esperado);
    }

    [Theory, DomainAutoData]
    public void TextoBeneficiario_Quando_ValorZeroNaConta_Deve_RetornarNulo(
        DateOnly data,
        TextoObrigatorio descricao,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        Lancamento lancamento = Lancamento.DaConta(data, hora: null, valor: 0m, descricao, contraparte, categoriaBanco: null, identidade);

        lancamento.TextoBeneficiario.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void TextoBeneficiario_Quando_ValorZeroNoCartao_Deve_RetornarNulo(
        DateOnly data,
        TextoObrigatorio descricao,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        Lancamento lancamento = Lancamento.DoCartao(data, hora: null, valor: 0m, descricao, contraparte, parcela: null, categoriaBanco: null, identidade);

        lancamento.TextoBeneficiario.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void TextoBeneficiario_Quando_SaidaNaConta_Deve_RetornarAContraparte(
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        Lancamento saida = Lancamento.DaConta(data, hora: null, -valor.Value, descricao, contraparte, categoriaBanco: null, identidade);

        saida.TextoBeneficiario.Should().Be(contraparte);
    }

    [Theory, DomainAutoData]
    public void TextoBeneficiario_Quando_SaidaNaContaSemContraparte_Deve_RetornarNulo(
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade)
    {
        Lancamento boleto = Lancamento.DaConta(data, hora: null, -valor.Value, descricao, contraparte: null, categoriaBanco: null, identidade);

        boleto.TextoBeneficiario.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void TextoBeneficiario_Quando_SaidaNoCartao_Deve_RetornarADescricao(
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio estabelecimento,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        Lancamento compra = Lancamento.DoCartao(data, hora: null, -valor.Value, estabelecimento, contraparte, parcela: null, categoriaBanco: null, identidade);

        compra.TextoBeneficiario.Should().Be(estabelecimento.Value);
    }

    [Theory, DomainAutoData]
    public void TextoBeneficiario_Quando_EntradaNaConta_Deve_RetornarNulo(
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        Lancamento entrada = Lancamento.DaConta(data, hora: null, valor, descricao, contraparte, categoriaBanco: null, identidade);

        entrada.TextoBeneficiario.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void TextoBeneficiario_Quando_EntradaNoCartao_Deve_RetornarNulo(
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        Lancamento entrada = Lancamento.DoCartao(data, hora: null, valor, descricao, contraparte, parcela: null, categoriaBanco: null, identidade);

        entrada.TextoBeneficiario.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void AliasTextoBeneficiario_Quando_SaidaNoCartao_Deve_RetornarOAliasDaDescricao(Lancamento lancamento, ReaisPositivo valor)
    {
        Lancamento compra = NoCartao(lancamento, -valor.Value);

        compra.AliasTextoBeneficiario.Should().Be((Alias)lancamento.Descricao.Value);
    }

    [Theory, DomainAutoData]
    public void AliasTextoBeneficiario_Quando_SaidaNaConta_Deve_RetornarOAliasDaContraparte(Lancamento lancamento, ReaisPositivo valor, string contraparte)
    {
        Lancamento saida = NaConta(lancamento, -valor.Value, contraparte: contraparte);

        saida.AliasTextoBeneficiario.Should().Be((Alias)contraparte);
    }

    [Theory, DomainAutoData]
    public void AliasTextoBeneficiario_Quando_SaidaNaContaSemContraparte_Deve_RetornarNulo(Lancamento lancamento, ReaisPositivo valor)
    {
        Lancamento boleto = NaConta(lancamento, -valor.Value);

        boleto.AliasTextoBeneficiario.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void AliasTextoBeneficiario_Quando_Entrada_Deve_RetornarNulo(Lancamento lancamento, ReaisPositivo valor)
    {
        Lancamento entrada = NoCartao(lancamento, valor);

        entrada.AliasTextoBeneficiario.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void AliasTextoBeneficiario_Quando_ValorZero_Deve_RetornarNulo(Lancamento lancamento)
    {
        Lancamento zerado = NoCartao(lancamento, valor: 0m);

        zerado.AliasTextoBeneficiario.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void AliasTextoBeneficiario_Quando_DescricaoNormalizaParaVazio_Deve_RetornarNulo(Lancamento lancamento, ReaisPositivo valor)
    {
        Lancamento compra = NoCartao(lancamento, -valor.Value, descricao: (TextoObrigatorio)TextoQueNormalizaParaVazio);

        compra.AliasTextoBeneficiario.Should().BeNull();
    }

    [Theory]
    [InlineDomainAutoData(-10)]
    [InlineDomainAutoData(10)]
    public void AliasCategoriaBanco_Quando_TemCategoria_Deve_RetornarOAliasEmSaidaEEntrada(decimal valor, Lancamento lancamento, string categoriaBanco)
    {
        Lancamento comCategoria = NoCartao(lancamento, valor, categoriaBanco: categoriaBanco);

        comCategoria.AliasCategoriaBanco.Should().Be((Alias)categoriaBanco);
    }

    [Theory, DomainAutoData]
    public void AliasCategoriaBanco_Quando_SemCategoria_Deve_RetornarNulo(Lancamento lancamento)
    {
        Lancamento semCategoria = NoCartao(lancamento, lancamento.Valor);

        semCategoria.AliasCategoriaBanco.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void AliasCategoriaBanco_Quando_TextoNormalizaParaVazio_Deve_RetornarNulo(Lancamento lancamento)
    {
        Lancamento comCategoria = NoCartao(lancamento, lancamento.Valor, categoriaBanco: TextoQueNormalizaParaVazio);

        comCategoria.AliasCategoriaBanco.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void EhCompraParcelada_Quando_SaidaComParcelaNaFatura_Deve_SerVerdadeiro(Lancamento lancamento, ReaisPositivo valor, Parcela parcela)
    {
        Lancamento compra = NoCartao(lancamento, -valor.Value, parcela: parcela);

        compra.EhCompraParcelada.Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void EhCompraParcelada_Quando_AVista_Deve_SerFalso(Lancamento lancamento, ReaisPositivo valor)
    {
        Lancamento compra = NoCartao(lancamento, -valor.Value);

        compra.EhCompraParcelada.Should().BeFalse();
    }

    [Theory]
    [InlineDomainAutoData(10)]
    [InlineDomainAutoData(0)]
    public void EhCompraParcelada_Quando_NaoEhSaida_Deve_SerFalso(decimal valor, Lancamento lancamento, Parcela parcela)
    {
        Lancamento naoSaida = NoCartao(lancamento, valor, parcela: parcela);

        naoSaida.EhCompraParcelada.Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void EhCompraParcelada_Quando_DescricaoNormalizaParaVazio_Deve_SerFalso(Lancamento lancamento, ReaisPositivo valor, Parcela parcela)
    {
        Lancamento compra = NoCartao(lancamento, -valor.Value, descricao: (TextoObrigatorio)TextoQueNormalizaParaVazio, parcela: parcela);

        compra.EhCompraParcelada.Should().BeFalse();
    }

    [Theory]
    [InlineDomainAutoData(-10.5, 10.5)]
    [InlineDomainAutoData(10.5, 10.5)]
    [InlineDomainAutoData(0, 0)]
    public void ValorAbsoluto_Deve_SerOValorSemOSinal(decimal valor, decimal esperado, Lancamento lancamento)
    {
        Lancamento result = NoCartao(lancamento, valor);

        result.ValorAbsoluto.Should().Be((ReaisPositivo)esperado);
    }

    [Theory, DomainAutoData]
    public void DaConta_Deve_TerOTipoDeOrigemConta(DateOnly data, Reais valor, TextoObrigatorio descricao, IdentidadeLancamento identidade)
    {
        Lancamento result = Lancamento.DaConta(data, hora: null, valor, descricao, contraparte: null, categoriaBanco: null, identidade);

        result.TipoOrigem.Should().Be(TipoOrigem.Conta);
    }

    [Theory, DomainAutoData]
    public void DoCartao_Deve_TerOTipoDeOrigemCartao(DateOnly data, Reais valor, TextoObrigatorio descricao, IdentidadeLancamento identidade)
    {
        Lancamento result = Lancamento.DoCartao(data, hora: null, valor, descricao, contraparte: null, parcela: null, categoriaBanco: null, identidade);

        result.TipoOrigem.Should().Be(TipoOrigem.Cartao);
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_OutroTipoDeOrigem_Deve_SerDiferente(Lancamento lancamento)
    {
        Lancamento noCartao = lancamento.NaOrigem(TipoOrigem.Cartao);

        noCartao.Should().NotBe(lancamento);
    }

    /// <summary>A linha de fatura com o valor pedido e, além da descrição, só os campos opcionais que o caso informa.</summary>
    private static Lancamento NoCartao(
        Lancamento lancamento,
        Reais valor,
        TextoObrigatorio? descricao = null,
        Parcela? parcela = null,
        string? categoriaBanco = null)
    {
        return Lancamento.DoCartao(lancamento.Data, lancamento.Hora, valor, descricao ?? lancamento.Descricao, contraparte: null, parcela, categoriaBanco, lancamento.Identidade);
    }

    /// <summary>A linha de extrato com o valor pedido e só os campos opcionais que o caso informa.</summary>
    private static Lancamento NaConta(Lancamento lancamento, Reais valor, string? contraparte = null)
    {
        return Lancamento.DaConta(lancamento.Data, lancamento.Hora, valor, lancamento.Descricao, contraparte, categoriaBanco: null, lancamento.Identidade);
    }
}
