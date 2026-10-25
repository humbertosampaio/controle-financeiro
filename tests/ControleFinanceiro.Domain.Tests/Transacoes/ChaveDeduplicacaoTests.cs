using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Transacoes;

public sealed class ChaveDeduplicacaoTests
{
    private static readonly DateOnly _dia = new(2026, 03, 05);

    public static readonly TheoryData<string?> ChavesInvalidas = new(
        null,
        "",
        new string('a', 63),
        new string('a', 65),
        new string('A', 64),
        new string('g', 64));

    [Fact]
    public void Calcular_Quando_IdentidadeComposta_Deve_RetornarHashDoTextoCanonico()
    {
        // v1|336|conta|0192a5b0-0000-7000-8000-000000000001|composta|2026-03-05|14:30:00|-123.40|PADARIA SÃO JOÃO||1
        OrigemTransacao origem = OrigemTransacao.DaConta(IdConta.From(Guid.Parse("0192a5b0-0000-7000-8000-000000000001")));
        IdentidadeLancamento identidade = IdentidadeLancamento.Composta(_dia)
            .ComHora(new TimeOnly(14, 30))
            .ComValor(-123.4m)
            .ComTexto("  padaria   são  joão ")
            .ComTexto(null);

        ChaveDeduplicacao result = ChaveDeduplicacao.Calcular(Banco.C6, origem, identidade, ordinal: 1);

        result.Value.Should().Be("76b82abae72afb9f1a6997ce782577f1fad8a1548d003f98093497f5e8094ccd");
    }

    [Fact]
    public void Calcular_Quando_IdentidadeUnica_Deve_RetornarHashDoTextoCanonico()
    {
        // v1|323|cartao|0192a5b0-0000-7000-8000-000000000002|unica|aB-123|1
        OrigemTransacao origem = OrigemTransacao.DoCartao(IdCartao.From(Guid.Parse("0192a5b0-0000-7000-8000-000000000002")));

        ChaveDeduplicacao result = ChaveDeduplicacao.Calcular(Banco.MercadoPago, origem, IdentidadeLancamento.Unica(" aB-123 ").Value, ordinal: 1);

        result.Value.Should().Be("fb19f7c1e07acfa84ac9a5197b6a09acf4016dc1293e1fe0ead8ef1247a4f168");
    }

    [Theory, DomainAutoData]
    public void Calcular_Quando_UnicaECompostaComOsMesmosCampos_Deve_RetornarChavesDiferentes(
        Banco banco,
        OrigemTransacao origem,
        DateOnly data)
    {
        IdentidadeLancamento composta = IdentidadeLancamento.Composta(data);
        IdentidadeLancamento unica = IdentidadeLancamento.Unica(composta.Campos[0]).Value;

        ChaveDeduplicacao result = ChaveDeduplicacao.Calcular(banco, origem, unica, ordinal: 1);

        result.Should().NotBe(ChaveDeduplicacao.Calcular(banco, origem, composta, ordinal: 1));
    }

    [Theory, DomainAutoData]
    public void Calcular_Quando_UnicaECompostaComOMesmoIdentificador_Deve_RetornarChavesDiferentes(
        Banco banco,
        OrigemTransacao origem,
        IdentidadeLancamento unica)
    {
        IdentidadeLancamento composta = IdentidadeLancamento.Composta(unica.Campos[0]).Value;

        ChaveDeduplicacao result = ChaveDeduplicacao.Calcular(banco, origem, composta, ordinal: 1);

        result.Should().NotBe(ChaveDeduplicacao.Calcular(banco, origem, unica, ordinal: 1));
    }

    [Theory, DomainAutoData]
    public void Calcular_Quando_SeparadorDentroDeUmCampo_Deve_RetornarChavesDiferentes(
        Banco banco,
        OrigemTransacao origem,
        DateOnly data)
    {
        IdentidadeLancamento separadorNoPrimeiro = IdentidadeLancamento.Composta(data).ComTexto("A|B").ComTexto("C");
        IdentidadeLancamento separadorNoSegundo = IdentidadeLancamento.Composta(data).ComTexto("A").ComTexto("B|C");

        ChaveDeduplicacao result = ChaveDeduplicacao.Calcular(banco, origem, separadorNoPrimeiro, ordinal: 1);

        result.Should().NotBe(ChaveDeduplicacao.Calcular(banco, origem, separadorNoSegundo, ordinal: 1));
    }

    [Theory, DomainAutoData]
    public void Calcular_Quando_ContaECartaoComOMesmoId_Deve_RetornarChavesDiferentes(
        Banco banco,
        Guid id,
        IdentidadeLancamento identidade)
    {
        ChaveDeduplicacao result = ChaveDeduplicacao.Calcular(banco, OrigemTransacao.DaConta(IdConta.From(id)), identidade, ordinal: 1);

        result.Should().NotBe(ChaveDeduplicacao.Calcular(banco, OrigemTransacao.DoCartao(IdCartao.From(id)), identidade, ordinal: 1));
    }

    [Theory, DomainAutoData]
    public void Calcular_Quando_BancoDiferente_Deve_RetornarChavesDiferentes(OrigemTransacao origem, IdentidadeLancamento identidade)
    {
        ChaveDeduplicacao result = ChaveDeduplicacao.Calcular(Banco.C6, origem, identidade, ordinal: 1);

        result.Should().NotBe(ChaveDeduplicacao.Calcular(Banco.MercadoPago, origem, identidade, ordinal: 1));
    }

    [Theory, DomainAutoData]
    public void Calcular_Quando_OrdinalDiferente_Deve_RetornarChavesDiferentes(
        Banco banco,
        OrigemTransacao origem,
        IdentidadeLancamento identidade)
    {
        ChaveDeduplicacao result = ChaveDeduplicacao.Calcular(banco, origem, identidade, ordinal: 1);

        result.Should().NotBe(ChaveDeduplicacao.Calcular(banco, origem, identidade, ordinal: 2));
    }

    [Theory, DomainAutoData]
    public void Calcular_Quando_OrdinalMenorQueUm_Deve_LancarExcecao(
        Banco banco,
        OrigemTransacao origem,
        IdentidadeLancamento identidade)
    {
        Func<ChaveDeduplicacao> act = () => ChaveDeduplicacao.Calcular(banco, origem, identidade, ordinal: 0);

        act.Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    [Theory, DomainAutoData]
    public void CalcularLote_Quando_IdentidadesIguais_Deve_NumerarDeUmAN(
        Banco banco,
        OrigemTransacao origem,
        IdentidadeLancamento identidade,
        IdentidadeLancamento outra)
    {
        IReadOnlyList<ChaveDeduplicacao> result = ChaveDeduplicacao.CalcularLote(banco, origem, [identidade, outra, identidade]);

        result.Should().Equal(
            ChaveDeduplicacao.Calcular(banco, origem, identidade, ordinal: 1),
            ChaveDeduplicacao.Calcular(banco, origem, outra, ordinal: 1),
            ChaveDeduplicacao.Calcular(banco, origem, identidade, ordinal: 2));
    }

    [Theory, DomainAutoData]
    public void CalcularLote_Quando_LoteReimportado_Deve_RetornarAsMesmasChaves(
        Banco banco,
        OrigemTransacao origem,
        IdentidadeLancamento identidade,
        IdentidadeLancamento outra)
    {
        IdentidadeLancamento[] lote = [identidade, identidade, outra];

        IReadOnlyList<ChaveDeduplicacao> result = ChaveDeduplicacao.CalcularLote(banco, origem, lote);

        result.Should().Equal(ChaveDeduplicacao.CalcularLote(banco, origem, lote));
    }

    [Theory, DomainAutoData]
    public void CalcularLote_Quando_PeriodoParcialmenteSobreposto_Deve_RepetirAsChavesDoDiaEmComum(
        Banco banco,
        OrigemTransacao origem)
    {
        IdentidadeLancamento diaAnterior = Identidade(_dia.AddDays(-1), "Padaria");
        IdentidadeLancamento compraNoDiaEmComum = Identidade(_dia, "Padaria");
        IdentidadeLancamento outraCompraNoDiaEmComum = Identidade(_dia, "Mercado");
        IdentidadeLancamento diaSeguinte = Identidade(_dia.AddDays(1), "Padaria");

        IReadOnlyList<ChaveDeduplicacao> primeiroLote = ChaveDeduplicacao.CalcularLote(
            banco, origem, [diaAnterior, compraNoDiaEmComum, compraNoDiaEmComum, outraCompraNoDiaEmComum]);
        IReadOnlyList<ChaveDeduplicacao> result = ChaveDeduplicacao.CalcularLote(
            banco, origem, [compraNoDiaEmComum, compraNoDiaEmComum, outraCompraNoDiaEmComum, diaSeguinte]);

        result.Intersect(primeiroLote).Should().Equal(primeiroLote.Skip(1));
    }

    [Fact]
    public void Criar_Quando_SessentaEQuatroHexMinusculos_Deve_RetornarSucesso()
    {
        string chave = "76b82abae72afb9f1a6997ce782577f1fad8a1548d003f98093497f5e8094ccd";

        Result<ChaveDeduplicacao> result = ChaveDeduplicacao.Criar(chave);

        result.Should().Succeed().Which.Value.Should().Be(chave);
    }

    [Theory]
    [MemberData(nameof(ChavesInvalidas))]
    public void Criar_Quando_NaoSaoSessentaEQuatroHexMinusculos_Deve_RetornarFalha(string? chave)
    {
        Result<ChaveDeduplicacao> result = ChaveDeduplicacao.Criar(chave);

        result.Should().FailWith("A chave de deduplicação deve ter 64 caracteres hexadecimais minúsculos");
    }

    [Fact]
    public void OperadorExplicito_Quando_ValorValido_Deve_RetornarInstancia()
    {
        string chave = "76b82abae72afb9f1a6997ce782577f1fad8a1548d003f98093497f5e8094ccd";

        ChaveDeduplicacao result = (ChaveDeduplicacao)chave;

        result.Value.Should().Be(chave);
    }

    [Theory]
    [MemberData(nameof(ChavesInvalidas))]
    public void OperadorExplicito_Quando_ValorInvalido_Deve_LancarExcecao(string? chave)
    {
        Func<ChaveDeduplicacao> act = () => (ChaveDeduplicacao)chave;

        act.Should().ThrowExactly<ResultFailureException>();
    }

    // A data e a descrição são o assunto do teste: a sobreposição depende delas.
    private static IdentidadeLancamento Identidade(DateOnly data, string descricao) =>
        IdentidadeLancamento.Composta(data).ComTexto(descricao);
}
