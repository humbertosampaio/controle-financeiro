using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta o <see cref="LancamentoConta"/> e o <see cref="LancamentoCartao"/> com a identidade composta pelos próprios
/// campos, como num layout sem id externo: dois lançamentos com os mesmos campos têm a mesma identidade. A parcela do
/// cartão vem do contexto, e a categoria do banco fica fora da identidade. O <see cref="Lancamento"/> abstrato não é
/// montado: o teste declara a origem que o caso usa.
/// </summary>
public sealed class LancamentoGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || (t != typeof(LancamentoConta) && t != typeof(LancamentoCartao)))
            return new NoSpecimen();

        DateOnly data = context.Create<DateOnly>();
        TimeOnly hora = context.Create<TimeOnly>();
        Reais valor = context.Create<Reais>();
        TextoObrigatorio descricao = context.Create<TextoObrigatorio>();
        string contraparte = context.Create<string>();
        string categoriaBanco = context.Create<string>();
        IdentidadeLancamento identidade = IdentidadeLancamento.Composta(data)
            .ComHora(hora)
            .ComValor(valor)
            .ComTexto(descricao)
            .ComTexto(contraparte);

        if (t == typeof(LancamentoConta))
            return LancamentoConta.Criar(data, hora, valor, descricao, contraparte, categoriaBanco, identidade);

        Parcela parcela = context.Create<Parcela>();
        return LancamentoCartao.Criar(data, hora, valor, descricao, contraparte, parcela, categoriaBanco, identidade.ComParcela(parcela));
    }
}
