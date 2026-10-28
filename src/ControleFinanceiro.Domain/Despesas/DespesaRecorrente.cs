using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Despesas;

[ValueObject]
public readonly partial struct IdDespesaRecorrente
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

public sealed class DespesaRecorrente : AggregateRoot<IdDespesaRecorrente>, IPertenceAoUsuario
{
    public IdUsuario IdUsuario { get; private set; }
    public IdBeneficiario IdBeneficiario { get; private set; }
    public TextoObrigatorio Descricao { get; private set; }
    public IdCategoriaDespesa IdCategoria { get; private set; }
    public FormaPagamento FormaPagamento { get; private set; }
    public IdCartao? IdCartao { get; private set; }
    public ReaisPositivo Valor { get; private set; }
    public DiaOcorrenciaRecorrente DiaOcorrencia { get; private set; }

    private DespesaRecorrente(
        IdDespesaRecorrente id,
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        FormaPagamento formaPagamento,
        IdCartao? idCartao,
        ReaisPositivo valor,
        DiaOcorrenciaRecorrente diaOcorrencia)
            : base(id)
    {
        IdUsuario = idUsuario;
        IdBeneficiario = idBeneficiario;
        Descricao = descricao;
        IdCategoria = idCategoria;
        FormaPagamento = formaPagamento;
        IdCartao = idCartao;
        Valor = valor;
        DiaOcorrencia = diaOcorrencia;
    }

    /// <remarks>
    /// Interno pois o cartão, o beneficiário e a categoria precisam ser do usuário, e a mesma despesa não pode ser
    /// cadastrada duas vezes; só a <see cref="Services.DespesaRecorrenteFactory"/>, que recebe as instâncias e as
    /// recorrentes do usuário, confere isso. Ela também garante o cartão no crédito, e só nele.
    /// </remarks>
    internal static DespesaRecorrente Criar(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        FormaPagamento formaPagamento,
        IdCartao? idCartao,
        ReaisPositivo valor,
        DiaOcorrenciaRecorrente diaOcorrencia)
    {
        return new DespesaRecorrente(
            IdDespesaRecorrente.FromNewVersion7Guid(),
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            formaPagamento,
            idCartao,
            valor,
            diaOcorrencia);
    }

    /// <summary>Se é do mesmo beneficiário, com a mesma descrição sem contar caixa, acentos e espaços repetidos.</summary>
    public bool PossuiBeneficiarioEDescricao(IdBeneficiario idBeneficiario, TextoObrigatorio descricao) =>
        IdBeneficiario == idBeneficiario && (TextoNormalizado)Descricao == (TextoNormalizado)descricao;
}
