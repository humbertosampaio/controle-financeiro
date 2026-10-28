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
        DateOnly dataOcorrencia = new DateOnly(2026, 02, 05);
        DiaVencimentoCartao diaVencimento = (DiaVencimentoCartao)05;
        MesReferencia mesReferenciaEsperado = MesReferencia.Criar(mes: 03, ano: 2026).Value;
        DateOnly dataProjetadaPagamentoEsperada = new DateOnly(2026, 03, 05);

        DespesaAdiada result = DespesaAdiada.Criar(
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataOcorrencia,
            valor,
            idCartao,
            diaVencimento);

        result.IdUsuario.Should().Be(idUsuario);
        result.IdBeneficiario.Should().Be(idBeneficiario);
        result.Descricao.Should().Be(descricao);
        result.IdCategoria.Should().Be(idCategoria);
        result.DataOcorrencia.Should().Be(dataOcorrencia);
        result.FormaPagamento.Should().Be(FormaPagamento.Credito);
        result.IdCartao.Should().Be(idCartao);
        result.MesReferencia.Should().Be(mesReferenciaEsperado);

        Pagamento pagamento = result.Pagamento;
        pagamento.DataProjetada.Should().Be(dataProjetadaPagamentoEsperada);
        pagamento.DataEfetiva.Should().BeNull();
        pagamento.Status.Should().Be(StatusPagamento.Pendente);
        pagamento.Valor.Should().Be(valor);
    }
}
