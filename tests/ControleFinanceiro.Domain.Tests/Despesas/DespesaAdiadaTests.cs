using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Despesas;

public sealed class DespesaAdiadaTests
{
    [Theory, DomainAutoData]
    public void Criar_Quando_VencimentoPosteriorADataOcorrencia_Deve_RetornarSucesso(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        ReaisPositivo valor,
        IdCartao idCartao)
    {
        DateOnly dataOcorrencia = new(2026, 02, 05);
        MesReferencia mesVencimento = (MesReferencia)(04, 2026);
        VencimentoFatura vencimento = new(mesVencimento, (DiaVencimentoCartao)05);
        DateOnly dataProjetadaPagamentoEsperada = new(2026, 04, 05);

        DespesaAdiada result = DespesaAdiada.Criar(
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataOcorrencia,
            valor,
            idCartao,
            vencimento,
            idDespesaRecorrente: null);

        result.IdUsuario.Should().Be(idUsuario);
        result.IdBeneficiario.Should().Be(idBeneficiario);
        result.Descricao.Should().Be(descricao);
        result.IdCategoria.Should().Be(idCategoria);
        result.DataOcorrencia.Should().Be(dataOcorrencia);
        result.FormaPagamento.Should().Be(FormaPagamento.Credito);
        result.IdCartao.Should().Be(idCartao);
        result.MesReferencia.Should().Be(mesVencimento);

        Pagamento pagamento = result.Pagamento;
        pagamento.DataProjetada.Should().Be(dataProjetadaPagamentoEsperada);
        pagamento.DataEfetiva.Should().BeNull();
        pagamento.Status.Should().Be(StatusPagamento.Pendente);
        pagamento.ValorProjetado.Should().Be(valor);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_MesVencimentoAnteriorAoDaOcorrencia_Deve_LancarExcecao(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        ReaisPositivo valor,
        IdCartao idCartao,
        DiaVencimentoCartao diaVencimento)
    {
        Action act = () => DespesaAdiada.Criar(
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria: null,
            new DateOnly(2026, 03, 05),
            valor,
            idCartao,
            new VencimentoFatura((MesReferencia)(02, 2026), diaVencimento),
            idDespesaRecorrente: null);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("vencimento");
    }
}
