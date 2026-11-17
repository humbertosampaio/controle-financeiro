using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class AliasesBeneficiarioPendentesTests
{
    [Theory, DomainAutoData]
    public void Listar_Quando_SaidasNaConta_Deve_TrazerAsContrapartesNaOrdemDoArquivo(
        IdUsuario idUsuario,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade,
        string primeira,
        string segunda)
    {
        LancamentoConta[] lancamentos = [Saida(data, valor, descricao, primeira, identidade), Saida(data, valor, descricao, segunda, identidade)];

        IReadOnlyList<Alias> result = AliasesBeneficiarioPendentes.Listar(idUsuario, beneficiariosDoUsuario: [], lancamentos);

        result.Select(a => a.TextoOriginal).Should().Equal(primeira, segunda);
    }

    [Theory, DomainAutoData]
    public void Listar_Quando_SaidasNoCartao_Deve_TrazerAsDescricoes(
        IdUsuario idUsuario,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio estabelecimento,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        IReadOnlyList<Alias> result = AliasesBeneficiarioPendentes.Listar(
            idUsuario,
            beneficiariosDoUsuario: [],
            [LancamentoCartao.Criar(data, hora: null, -valor, estabelecimento, contraparte, parcela: null, categoriaBanco: null, identidade)]);

        result.Should().ContainSingle().Which.TextoOriginal.Should().Be(estabelecimento);
    }

    [Theory, DomainAutoData]
    public void Listar_Quando_TextoRepetidoComOutraGrafia_Deve_TrazerUmSoComOTextoDaPrimeiraVez(
        IdUsuario idUsuario,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade)
    {
        LancamentoConta[] lancamentos =
        [
            Saida(data, valor, descricao, "Padaria São João", identidade),
            Saida(data, valor, descricao, "PADARIA  SAO JOAO", identidade),
        ];

        IReadOnlyList<Alias> result = AliasesBeneficiarioPendentes.Listar(idUsuario, beneficiariosDoUsuario: [], lancamentos);

        result.Should().ContainSingle().Which.TextoOriginal.Should().Be("Padaria São João");
    }

    [Theory, DomainAutoData]
    public void Listar_Quando_TextoJaEAliasDeUmBeneficiario_Deve_DeixarDeFora(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Alias alias,
        Beneficiario beneficiario,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade)
    {
        IReadOnlyList<Alias> result = AliasesBeneficiarioPendentes.Listar(
            idUsuario,
            [beneficiario],
            [Saida(data, valor, descricao, alias.TextoOriginal, identidade)]);

        result.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void Listar_Quando_Entrada_Deve_DeixarDeFora(
        IdUsuario idUsuario,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        LancamentoConta entrada = LancamentoConta.Criar(data, hora: null, valor, descricao, contraparte, categoriaBanco: null, identidade);

        IReadOnlyList<Alias> result = AliasesBeneficiarioPendentes.Listar(idUsuario, beneficiariosDoUsuario: [], [entrada]);

        result.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void Listar_Quando_SaidaNaContaSemContraparte_Deve_DeixarDeFora(
        IdUsuario idUsuario,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade)
    {
        LancamentoConta boleto = LancamentoConta.Criar(data, hora: null, -valor, descricao, contraparte: null, categoriaBanco: null, identidade);

        IReadOnlyList<Alias> result = AliasesBeneficiarioPendentes.Listar(idUsuario, beneficiariosDoUsuario: [], [boleto]);

        result.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void Listar_Quando_BeneficiarioDeOutroUsuario_Deve_LancarArgumentException(
        IdUsuario idUsuario,
        Beneficiario deOutroUsuario,
        LancamentoConta lancamento)
    {
        Action listar = () => AliasesBeneficiarioPendentes.Listar(idUsuario, [deOutroUsuario], [lancamento]);

        listar.Should().ThrowExactly<ArgumentException>().WithParameterName("beneficiariosDoUsuario");
    }

    [Theory, DomainAutoData]
    public void Conferir_Quando_TodosOsTextosSaoAlias_Deve_RetornarSucesso(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Alias alias,
        Beneficiario beneficiario,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade)
    {
        UnitResult<Erro> result = AliasesBeneficiarioPendentes.Conferir(
            idUsuario,
            [beneficiario],
            [Saida(data, valor, descricao, alias.TextoOriginal, identidade)]);

        result.Should().Succeed();
    }

    [Theory, DomainAutoData]
    public void Conferir_Quando_TextoSemAlias_Deve_RetornarBeneficiariosNaoResolvidos(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Alias alias,
        Beneficiario beneficiario,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        IdentidadeLancamento identidade,
        string semAlias)
    {
        LancamentoConta[] lancamentos = [Saida(data, valor, descricao, alias.TextoOriginal, identidade), Saida(data, valor, descricao, semAlias, identidade)];

        UnitResult<Erro> result = AliasesBeneficiarioPendentes.Conferir(idUsuario, [beneficiario], lancamentos);

        result.Should().Fail().Which.Should().Be(ErroBeneficiariosNaoResolvidos.Instancia);
    }

    [Theory, DomainAutoData]
    public void Conferir_Quando_SoEntradasEBoletos_Deve_RetornarSucesso(
        IdUsuario idUsuario,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string contraparte,
        IdentidadeLancamento identidade)
    {
        LancamentoConta entrada = LancamentoConta.Criar(data, hora: null, valor, descricao, contraparte, categoriaBanco: null, identidade);
        LancamentoConta boleto = LancamentoConta.Criar(data, hora: null, -valor, descricao, contraparte: null, categoriaBanco: null, identidade);

        UnitResult<Erro> result = AliasesBeneficiarioPendentes.Conferir(idUsuario, beneficiariosDoUsuario: [], [entrada, boleto]);

        result.Should().Succeed();
    }

    [Theory, DomainAutoData]
    public void Conferir_Quando_ListaComBeneficiarioDeOutroUsuario_Deve_LancarExcecao(
        IdUsuario idUsuario,
        Beneficiario deOutroUsuario,
        LancamentoConta lancamento)
    {
        Action conferir = () => AliasesBeneficiarioPendentes.Conferir(idUsuario, [deOutroUsuario], [lancamento]);

        conferir.Should().ThrowExactly<ArgumentException>().WithParameterName("beneficiariosDoUsuario");
    }

    private static LancamentoConta Saida(DateOnly data, ReaisPositivo valor, TextoObrigatorio descricao, string contraparte, IdentidadeLancamento identidade)
    {
        return LancamentoConta.Criar(data, hora: null, -valor, descricao, contraparte, categoriaBanco: null, identidade);
    }
}
