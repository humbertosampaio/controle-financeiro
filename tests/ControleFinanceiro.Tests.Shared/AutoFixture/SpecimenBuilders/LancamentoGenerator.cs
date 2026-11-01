using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta o <see cref="Lancamento"/> com a identidade composta pelos próprios campos, como num layout
/// sem id externo: dois lançamentos com os mesmos campos têm a mesma identidade. A parcela vem do contexto, e a
/// categoria do banco fica fora da identidade. É uma linha de extrato, a não ser que o contexto dê outro
/// <see cref="TipoOrigem"/>.
/// </summary>
public sealed class LancamentoGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(Lancamento))
            return new NoSpecimen();

        DateOnly data = context.Create<DateOnly>();
        TimeOnly hora = context.Create<TimeOnly>();
        Reais valor = context.Create<Reais>();
        TextoObrigatorio descricao = context.Create<TextoObrigatorio>();
        string contraparte = context.Create<string>();
        Parcela parcela = context.Create<Parcela>();
        string categoriaBanco = context.Create<string>();

        IdentidadeLancamento identidade = IdentidadeLancamento.Composta(data)
            .ComHora(hora)
            .ComValor(valor)
            .ComTexto(descricao)
            .ComTexto(contraparte)
            .ComParcela(parcela);

        return context.Create<TipoOrigem>() == TipoOrigem.Cartao
            ? Lancamento.DoCartao(data, hora, valor, descricao, contraparte, parcela, categoriaBanco, identidade)
            : Lancamento.DaConta(data, hora, valor, descricao, contraparte, categoriaBanco, identidade);
    }
}
