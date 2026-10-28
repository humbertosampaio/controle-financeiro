using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Services;

/// <remarks>
/// O <c>[Frozen]</c> vale só para os parâmetros depois dele: o agregado "de outro usuário" vem antes do
/// <c>[Frozen] IdUsuario</c>, e os demais, depois, saem do usuário congelado.
/// </remarks>
public sealed class DespesaRecorrenteFactoryTests
{
    [Theory, DomainAutoData]
    public void CriarNoCredito_Quando_ItensDoUsuario_Deve_GuardarOsIdsEOCartao(
        [Frozen] IdUsuario idUsuario,
        Beneficiario beneficiario,
        TextoObrigatorio descricao,
        CategoriaDespesa categoria,
        Cartao cartao,
        ReaisPositivo valor,
        DiaOcorrenciaRecorrente diaOcorrencia)
    {
        Result<DespesaRecorrente, Erro> result = DespesaRecorrenteFactory.CriarNoCredito(
            idUsuario, recorrentesDoUsuario: [], beneficiario, descricao, categoria, cartao, valor, diaOcorrencia);

        DespesaRecorrente despesa = result.Should().Succeed().Subject;
        despesa.IdUsuario.Should().Be(idUsuario);
        despesa.IdBeneficiario.Should().Be(beneficiario.Id);
        despesa.Descricao.Should().Be(descricao);
        despesa.IdCategoria.Should().Be(categoria.Id);
        despesa.FormaPagamento.Should().Be(FormaPagamento.Credito);
        despesa.IdCartao.Should().Be(cartao.Id);
        despesa.Valor.Should().Be(valor);
        despesa.DiaOcorrencia.Should().Be(diaOcorrencia);
    }

    [Theory]
    [InlineDomainAutoData(1)] // Pix
    [InlineDomainAutoData(3)] // Boleto
    public void CriarSemCartao_Quando_PixOuBoleto_Deve_GuardarAFormaSemCartao(
        int valorFormaPagamento,
        [Frozen] IdUsuario idUsuario,
        Beneficiario beneficiario,
        TextoObrigatorio descricao,
        CategoriaDespesa categoria,
        ReaisPositivo valor,
        DiaOcorrenciaRecorrente diaOcorrencia)
    {
        FormaPagamento formaPagamento = FormaPagamento.FromValue(valorFormaPagamento);

        Result<DespesaRecorrente, Erro> result = DespesaRecorrenteFactory.CriarSemCartao(
            idUsuario, recorrentesDoUsuario: [], beneficiario, descricao, categoria, formaPagamento, valor, diaOcorrencia);

        DespesaRecorrente despesa = result.Should().Succeed().Subject;
        despesa.IdUsuario.Should().Be(idUsuario);
        despesa.FormaPagamento.Should().Be(formaPagamento);
        despesa.IdCartao.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void CriarSemCartao_Quando_Credito_Deve_LancarExcecao(
        [Frozen] IdUsuario idUsuario,
        Beneficiario beneficiario,
        TextoObrigatorio descricao,
        CategoriaDespesa categoria,
        ReaisPositivo valor,
        DiaOcorrenciaRecorrente diaOcorrencia)
    {
        Action act = () => DespesaRecorrenteFactory.CriarSemCartao(
            idUsuario, recorrentesDoUsuario: [], beneficiario, descricao, categoria, FormaPagamento.Credito, valor, diaOcorrencia);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("formaPagamento");
    }

    [Theory, DomainAutoData]
    public void CriarNoCredito_Quando_CartaoDeOutroUsuario_Deve_LancarExcecao(
        Cartao cartaoDeOutroUsuario,
        [Frozen] IdUsuario idUsuario,
        Beneficiario beneficiario,
        TextoObrigatorio descricao,
        CategoriaDespesa categoria,
        ReaisPositivo valor,
        DiaOcorrenciaRecorrente diaOcorrencia)
    {
        Action act = () => DespesaRecorrenteFactory.CriarNoCredito(
            idUsuario, recorrentesDoUsuario: [], beneficiario, descricao, categoria, cartaoDeOutroUsuario, valor, diaOcorrencia);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("cartao");
    }

    [Theory, DomainAutoData]
    public void CriarSemCartao_Quando_BeneficiarioDeOutroUsuario_Deve_LancarExcecao(
        Beneficiario beneficiarioDeOutroUsuario,
        [Frozen] IdUsuario idUsuario,
        TextoObrigatorio descricao,
        CategoriaDespesa categoria,
        ReaisPositivo valor,
        DiaOcorrenciaRecorrente diaOcorrencia)
    {
        Action act = () => DespesaRecorrenteFactory.CriarSemCartao(
            idUsuario, recorrentesDoUsuario: [], beneficiarioDeOutroUsuario, descricao, categoria, FormaPagamento.Pix, valor, diaOcorrencia);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("beneficiario");
    }

    [Theory, DomainAutoData]
    public void CriarSemCartao_Quando_CategoriaDeOutroUsuario_Deve_LancarExcecao(
        CategoriaDespesa categoriaDeOutroUsuario,
        [Frozen] IdUsuario idUsuario,
        Beneficiario beneficiario,
        TextoObrigatorio descricao,
        ReaisPositivo valor,
        DiaOcorrenciaRecorrente diaOcorrencia)
    {
        Action act = () => DespesaRecorrenteFactory.CriarSemCartao(
            idUsuario, recorrentesDoUsuario: [], beneficiario, descricao, categoriaDeOutroUsuario, FormaPagamento.Pix, valor, diaOcorrencia);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("categoria");
    }

    [Theory, DomainAutoData]
    public void CriarSemCartao_Quando_ListaComRecorrenteDeOutroUsuario_Deve_LancarExcecao(
        DespesaRecorrente recorrenteDeOutroUsuario,
        [Frozen] IdUsuario idUsuario,
        Beneficiario beneficiario,
        TextoObrigatorio descricao,
        CategoriaDespesa categoria,
        ReaisPositivo valor,
        DiaOcorrenciaRecorrente diaOcorrencia)
    {
        Action act = () => DespesaRecorrenteFactory.CriarSemCartao(
            idUsuario, [recorrenteDeOutroUsuario], beneficiario, descricao, categoria, FormaPagamento.Pix, valor, diaOcorrencia);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("recorrentesDoUsuario");
    }

    [Theory, DomainAutoData]
    public void CriarSemCartao_Quando_MesmoBeneficiarioEDescricaoSemCaixaEAcentos_Deve_RetornarErroJaCadastrada(
        [Frozen] IdUsuario idUsuario,
        Beneficiario beneficiario,
        CategoriaDespesa categoria,
        ReaisPositivo valor,
        DiaOcorrenciaRecorrente diaOcorrencia)
    {
        DespesaRecorrente existente = DespesaRecorrenteFactory
            .CriarSemCartao(idUsuario, recorrentesDoUsuario: [], beneficiario, (TextoObrigatorio)"Condomínio", categoria, FormaPagamento.Boleto, valor, diaOcorrencia)
            .Value;

        Result<DespesaRecorrente, Erro> result = DespesaRecorrenteFactory.CriarSemCartao(
            idUsuario, [existente], beneficiario, (TextoObrigatorio)"  condominio ", categoria, FormaPagamento.Pix, valor, diaOcorrencia);

        result.Should().Fail()
            .Which.Should().BeOfType<ErroDespesaRecorrenteJaCadastrada>()
            .Which.IdDespesaRecorrente.Should().Be(existente.Id);
    }

    [Theory, DomainAutoData]
    public void CriarSemCartao_Quando_MesmoBeneficiarioComOutraDescricao_Deve_RetornarSucesso(
        [Frozen] IdUsuario idUsuario,
        Beneficiario beneficiario,
        CategoriaDespesa categoria,
        ReaisPositivo valor,
        DiaOcorrenciaRecorrente diaOcorrencia)
    {
        DespesaRecorrente existente = DespesaRecorrenteFactory
            .CriarSemCartao(idUsuario, recorrentesDoUsuario: [], beneficiario, (TextoObrigatorio)"Condomínio", categoria, FormaPagamento.Boleto, valor, diaOcorrencia)
            .Value;

        Result<DespesaRecorrente, Erro> result = DespesaRecorrenteFactory.CriarSemCartao(
            idUsuario, [existente], beneficiario, (TextoObrigatorio)"Fundo de reserva", categoria, FormaPagamento.Boleto, valor, diaOcorrencia);

        result.Should().Succeed();
    }

    [Theory, DomainAutoData]
    public void CriarNoCredito_Quando_MesmaDescricaoDeOutroBeneficiario_Deve_RetornarSucesso(
        [Frozen] IdUsuario idUsuario,
        DespesaRecorrente existente,
        Beneficiario outroBeneficiario,
        CategoriaDespesa categoria,
        Cartao cartao,
        ReaisPositivo valor,
        DiaOcorrenciaRecorrente diaOcorrencia)
    {
        Result<DespesaRecorrente, Erro> result = DespesaRecorrenteFactory.CriarNoCredito(
            idUsuario, [existente], outroBeneficiario, existente.Descricao, categoria, cartao, valor, diaOcorrencia);

        result.Should().Succeed();
    }
}
