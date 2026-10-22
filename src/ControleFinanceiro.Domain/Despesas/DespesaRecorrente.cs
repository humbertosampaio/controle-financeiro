using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Despesas;

[ValueObject]
public readonly partial struct IdDespesaRecorrente
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

public sealed class DespesaRecorrente : Entity<IdDespesaRecorrente>
{
    public IdBeneficiario IdBeneficiario { get; private set; }
    public TextoObrigatorio Descricao { get; private set; }
    public IdCategoriaDespesa IdCategoria { get; private set; }
    public FormaPagamento FormaPagamento { get; private set; }
    public IdCartao? IdCartao { get; private set; }
    public ReaisPositivo Valor { get; private set; }
    public DiaCompraRecorrente DiaCompra { get; private set; }

    private DespesaRecorrente(
        IdDespesaRecorrente id,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        FormaPagamento formaPagamento,
        IdCartao? idCartao,
        ReaisPositivo valor,
        DiaCompraRecorrente diaCompra)
            : base(id)
    {
        IdBeneficiario = idBeneficiario;
        Descricao = descricao;
        IdCategoria = idCategoria;
        FormaPagamento = formaPagamento;
        IdCartao = idCartao;
        Valor = valor;
        DiaCompra = diaCompra;
    }

    /// <param name="idCartao">Obrigatório no crédito e proibido nas outras formas de pagamento.</param>
    public static Result<DespesaRecorrente> Criar(
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
            idBeneficiario,
            descricao,
            idCategoria,
            formaPagamento,
            idCartao,
            valor,
            diaCompra);
    }
}
