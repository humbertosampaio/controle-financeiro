using Bogus;
using Bogus.DataSets;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Realistas;

/// <summary>
/// Troca os valores aleatórios do AutoFixture por valores com cara de dado real: nomes com acento (de
/// usuários e de beneficiários), rendas e descontos com centavos. Serve aos testes de integração,
/// em que o valor atravessa JSON e Postgres e a falha precisa ser legível.
/// </summary>
/// <remarks>
/// Opt-in: o <see cref="DomainFixture"/> não a aplica, porque os testes de domínio testam melhor as
/// bordas com os decimais longos do <see cref="StrictDecimalCustomization"/>. Gera só nomes, valores
/// e descrições: nada de CPF, conta ou número de cartão completo. O Bogus entra só onde o AutoFixture
/// e o <see cref="Random"/> não dão conta, o nome de pessoa.
/// </remarks>
public sealed class RealistasCustomization : CompositeCustomization
{
    // Seed fixa: um teste que falha gera os mesmos dados ao rodar de novo.
    private const int Seed = 4;

    public RealistasCustomization()
        : this(new Random(Seed), new Name("pt_BR") { Random = new Randomizer(Seed) })
    {
    }

    private RealistasCustomization(Random random, Name nomes)
        : base(
            new DescontoSalarialRealistaCustomization(random),
            new UsuarioRealistaCustomization(random, nomes),
            new BeneficiarioRealistaCustomization(nomes))
    {
    }
}
