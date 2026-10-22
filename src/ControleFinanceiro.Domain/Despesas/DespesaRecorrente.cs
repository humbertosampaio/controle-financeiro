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

public sealed class DespesaRecorrente : AggregateRoot<IdDespesaRecorrente>
{
    public IdUsuario IdUsuario { get; private set; }
    public IdBeneficiario IdBeneficiario { get; private set; }
    public TextoObrigatorio Descricao { get; private set; }
    public IdCategoriaDespesa IdCategoria { get; private set; }
    public FormaPagamento FormaPagamento { get; private set; }
    public IdCartao? IdCartao { get; private set; }
    public ReaisPositivo Valor { get; private set; }
    public DiaCompraRecorrente DiaCompra { get; private set; }

    private DespesaRecorrente(
        IdDespesaRecorrente id,
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        FormaPagamento formaPagamento,
        IdCartao? idCartao,
        ReaisPositivo valor,
        DiaCompraRecorrente diaCompra)
            : base(id)
    {
        IdUsuario = idUsuario;
        IdBeneficiario = idBeneficiario;
        Descricao = descricao;
        IdCategoria = idCategoria;
        FormaPagamento = formaPagamento;
        IdCartao = idCartao;
        Valor = valor;
        DiaCompra = diaCompra;
    }

    /// <remarks>
    /// Interno pois o cartão precisa ser do usuário, e só a <see cref="Services.DespesaRecorrenteFactory"/>,
    /// que recebe o cartão, confere isso.
    /// </remarks>
    /// <param name="idCartao">Obrigatório no crédito e proibido nas outras formas de pagamento.</param>
    internal static Result<DespesaRecorrente> Criar(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        FormaPagamento formaPagamento,
        IdCartao? idCartao,
        ReaisPositivo valor,
        DiaCompraRecorrente diaCompra)
    {
        bool ehCredito = formaPagamento == FormaPagamento.Credito;

        if (ehCredito && idCartao is null)
            return Result.Failure<DespesaRecorrente>("Despesa recorrente no crédito precisa de um cartão.");

        if (!ehCredito && idCartao is not null)
            return Result.Failure<DespesaRecorrente>($"Despesa recorrente paga com {formaPagamento.Name} não pode ter cartão.");

        return new DespesaRecorrente(
            IdDespesaRecorrente.FromNewVersion7Guid(),
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            formaPagamento,
            idCartao,
            valor,
            diaCompra);
    }
}
