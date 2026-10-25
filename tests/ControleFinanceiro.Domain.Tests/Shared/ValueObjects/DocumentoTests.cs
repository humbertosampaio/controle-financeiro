using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Shared.ValueObjects;

public sealed class DocumentoTests
{
    [Theory]
    [InlineData("123.456.789-09")]
    [InlineData("12345678909")]
    public void Criar_Quando_OnzeCaracteresSemAMascara_Deve_RetornarCpf(string numero)
    {
        Result<Documento> result = Documento.Criar(numero);

        result.Should().Succeed().Which.Should().Be((Cpf)"12345678909");
    }

    [Theory]
    [InlineData("12.ABC.345/01DE-35")]
    [InlineData("12ABC34501DE35")]
    public void Criar_Quando_QuatorzeCaracteresSemAMascara_Deve_RetornarCnpj(string numero)
    {
        Result<Documento> result = Documento.Criar(numero);

        result.Should().Succeed().Which.Should().Be((Cnpj)"12ABC34501DE35");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123456789")]
    [InlineData("123456789090")]
    public void Criar_Quando_OutraQuantidadeDeCaracteres_Deve_RetornarFalhaSemONumero(string? numero)
    {
        Result<Documento> result = Documento.Criar(numero);

        result.Should().FailWith("O documento não é um CPF nem um CNPJ válido");
    }

    [Theory]
    [InlineData("12345678919", "O documento não é um CPF válido")]
    [InlineData("11222333000191", "O documento não é um CNPJ válido")]
    public void Criar_Quando_VerificadorErrado_Deve_RetornarAFalhaDoTipoDoTamanho(string numero, string erro)
    {
        Result<Documento> result = Documento.Criar(numero);

        result.Should().FailWith(erro);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_CpfGerado_Deve_ReconhecerOCpf(Cpf cpf)
    {
        Result<Documento> result = Documento.Criar(cpf.Value);

        result.Should().Succeed().Which.Should().Be(cpf);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_CnpjGerado_Deve_ReconhecerOCnpj(Cnpj cnpj)
    {
        Result<Documento> result = Documento.Criar(cnpj.Value);

        result.Should().Succeed().Which.Should().Be(cnpj);
    }
}
