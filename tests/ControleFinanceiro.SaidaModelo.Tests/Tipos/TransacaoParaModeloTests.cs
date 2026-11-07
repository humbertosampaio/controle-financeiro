using System.Reflection;
using System.Text.Json.Nodes;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ControleFinanceiro.Tests.Shared.Respostas;
using Xunit.Sdk;

namespace ControleFinanceiro.SaidaModelo.Tests.Tipos;

public sealed class TransacaoParaModeloTests
{
    public static readonly TheoryData<FormaDocumento> FormasDocumento = new(Enum.GetValues<FormaDocumento>());

    [Theory, UsuarioUnicoAutoData]
    public void Serializar_Deve_TerSomenteOsCamposPermitidos(TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        JsonObject json = transacoes.DaConta(lancamento.SemCategoria()).ParaModelo().Serializar();

        json.Select(campo => campo.Key).Should().BeEquivalentTo("id", "data", "valor", "descricao", "contraparte");
    }

    [Theory, UsuarioUnicoAutoData]
    public void Serializar_Deve_LevarOsValoresDaTransacao(TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DaConta(lancamento.SemCategoria());

        JsonObject json = transacao.ParaModelo().Serializar();

        TransacaoLida lida = new(json);
        lida.Id.Should().Be(transacao.Id.Value);
        lida.Data.Should().Be(transacao.Data);
        lida.Valor.Should().Be(transacao.Valor);
    }

    [Theory, UsuarioUnicoAutoData]
    public void Serializar_Deve_NaoConterAChaveDeduplicacao(TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DaConta(lancamento.SemCategoria());

        JsonObject json = transacao.ParaModelo().Serializar();

        json.ToJsonString().Should().NotContain(transacao.Chave);
    }

    [Theory, FormasDocumentoAutoData]
    public void Serializar_Quando_DescricaoComDocumento_Deve_SairSemODocumento(
        FormaDocumento forma,
        DocumentosDeTeste documentos,
        TransacoesDeTeste transacoes,
        Lancamento lancamento)
    {
        TextoObrigatorio descricao = (TextoObrigatorio)$"LOJA {documentos.Em(forma)}";

        Transacao transacao = transacoes.DaConta(lancamento.SemCategoria().ComTextos(descricao, lancamento.Contraparte));

        JsonObject json = transacao.ParaModelo().Serializar();

        new TransacaoLida(json).Descricao.Should().Be($"LOJA {DocumentosDeTeste.Rotulo(forma)}");
    }

    [Theory, FormasDocumentoAutoData]
    public void Serializar_Quando_ContraparteComDocumento_Deve_SairSemODocumento(
        FormaDocumento forma,
        DocumentosDeTeste documentos,
        TransacoesDeTeste transacoes,
        Lancamento lancamento)
    {
        Lancamento comDocumento = lancamento.SemCategoria().ComTextos(lancamento.Descricao, $"FULANO {documentos.Em(forma)}");

        JsonObject json = transacoes.DaConta(comDocumento).ParaModelo().Serializar();

        new TransacaoLida(json).Contraparte.Should().Be($"FULANO {DocumentosDeTeste.Rotulo(forma)}");
    }

    [Theory, FormasDocumentoAutoData]
    public void Serializar_Quando_ContraparteSoComDocumento_Deve_OmitirOCampo(
        FormaDocumento forma,
        DocumentosDeTeste documentos,
        TransacoesDeTeste transacoes,
        Lancamento lancamento)
    {
        Lancamento comDocumento = lancamento.SemCategoria().ComTextos(lancamento.Descricao, documentos.Em(forma));

        JsonObject json = transacoes.DaConta(comDocumento).ParaModelo().Serializar();

        json.Should().NotContainKey("contraparte");
    }

    [Theory, UsuarioUnicoAutoData]
    public void Serializar_Quando_ContraparteAusente_Deve_OmitirOCampo(TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DaConta(lancamento.SemCategoria().ComTextos(lancamento.Descricao, contraparte: null));

        JsonObject json = transacao.ParaModelo().Serializar();

        json.Should().NotContainKey("contraparte");
    }

    [Theory, UsuarioUnicoAutoData]
    public void Serializar_Quando_DescricaoSoComDocumento_Deve_SairVazia(DocumentosDeTeste documentos, TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        TextoObrigatorio descricao = (TextoObrigatorio)documentos.Em(FormaDocumento.CpfPuro);

        Transacao transacao = transacoes.DaConta(lancamento.SemCategoria().ComTextos(descricao, lancamento.Contraparte));

        JsonObject json = transacao.ParaModelo().Serializar();

        new TransacaoLida(json).Descricao.Should().BeEmpty();
    }

    [Theory, UsuarioUnicoAutoData]
    public void Serializar_Quando_NumeroComVerificadorErrado_Deve_ManterONumero(TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        TextoObrigatorio descricao = (TextoObrigatorio)"LOJA 11222333000182";

        Transacao transacao = transacoes.DaConta(lancamento.SemCategoria().ComTextos(descricao, lancamento.Contraparte));

        JsonObject json = transacao.ParaModelo().Serializar();

        new TransacaoLida(json).Descricao.Should().Be("LOJA 11222333000182");
    }

    /// <summary>Um caso por forma de documento, das <see cref="FormasDocumento"/>, gerado como o <see cref="UsuarioUnicoAutoDataAttribute"/>.</summary>
    private sealed class FormasDocumentoAutoDataAttribute : DataAttribute
    {
        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            return FormasDocumento.SelectMany<object[], object[]>(
                forma => new InlineUsuarioUnicoAutoDataAttribute(forma).GetData(testMethod));
        }
    }
}
