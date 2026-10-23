using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta o <see cref="Lancamento"/> com a identidade composta pelos próprios campos, como num layout
/// sem id externo: dois lançamentos com os mesmos campos têm a mesma identidade.
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

        IdentidadeLancamento identidade = IdentidadeLancamento.Composta(data)
            .ComHora(hora)
            .ComValor(valor)
            .ComTexto(descricao)
            .ComTexto(contraparte);

        return Lancamento.Criar(data, hora, valor, descricao, contraparte, identidade);
    }
}
