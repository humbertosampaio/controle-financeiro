using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Beneficiarios;

public sealed class BeneficiarioTests
{
    [Theory, DomainAutoData]
    public void Criar_Quando_ParametrosValidos_Deve_RetornarSucesso(long idUsuario, TextoObrigatorio nomeExibicao)
    {
        Beneficiario beneficiario = Beneficiario.Criar(idUsuario, nomeExibicao);

        beneficiario.IdUsuario.Should().Be(idUsuario);
        beneficiario.NomeExibicao.Should().Be(nomeExibicao);
        beneficiario.Aliases.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void AdicionarAlias_Quando_NaoExistente_Deve_Adicionar(Beneficiario beneficiario, TextoObrigatorio textoExtrato)
    {
        beneficiario.AdicionarAlias(textoExtrato);

        AliasBeneficiario alias = beneficiario.Aliases.Should().ContainSingle().Which;
        alias.TextoExtrato.Should().Be(textoExtrato);
        alias.CodigoOrigem.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void AdicionarAlias_Quando_ExistenteMesmoTextoEOrigem_Deve_RetornarFalha(Beneficiario beneficiario, TextoObrigatorio textoExtrato)
    {
        beneficiario.AdicionarAlias(textoExtrato);

        Result result = beneficiario.AdicionarAlias(textoExtrato);

        result.Should().FailWith("Já existe um alias com o mesmo texto e origem para este beneficiário.");
        beneficiario.Aliases.Should().ContainSingle();
    }

    [Theory, DomainAutoData]
    public void AdicionarAlias_Quando_MesmoTextoMasOrigemDiferente_Deve_Adicionar(Beneficiario beneficiario, TextoObrigatorio textoExtrato)
    {
        beneficiario.AdicionarAlias(textoExtrato, "nubank");

        Result result = beneficiario.AdicionarAlias(textoExtrato, "itau");

        result.Should().Succeed();
        beneficiario.Aliases.Should().HaveCount(2);
    }
}
