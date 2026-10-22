using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Contas;

[ValueObject]
public readonly partial struct IdConta
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

/// <summary>
/// Conta de onde vêm os lançamentos de um extrato. Guarda só o sufixo do número: o número completo, a
/// agência e o titular não são gravados.
/// </summary>
public sealed class ContaBancaria : AggregateRoot<IdConta>
{
    public IdUsuario IdUsuario { get; private set; }
    public Banco Banco { get; private set; } = null!;
    public TipoConta Tipo { get; private set; } = null!;
    public SufixoConta Sufixo { get; private set; } = null!;
    public TextoObrigatorio Apelido { get; private set; } = null!;

    private ContaBancaria()
    {
    }

    private ContaBancaria(IdConta id, IdUsuario idUsuario, Banco banco, TipoConta tipo, SufixoConta sufixo, TextoObrigatorio apelido)
        : base(id)
    {
        IdUsuario = idUsuario;
        Banco = banco;
        Tipo = tipo;
        Sufixo = sufixo;
        Apelido = apelido;
    }

    /// <summary>A conta nasce com o apelido "&lt;tipo&gt; &lt;banco&gt; final &lt;sufixo&gt;".</summary>
    /// <remarks>
    /// Interno pois o usuário não pode ter duas contas do mesmo banco com o mesmo final, e só a
    /// <see cref="Services.ContaBancariaFactory"/>, que recebe as contas do usuário, confere isso.
    /// </remarks>
    internal static ContaBancaria Criar(IdUsuario idUsuario, Banco banco, TipoConta tipo, SufixoConta sufixo)
    {
        TextoObrigatorio apelido = (TextoObrigatorio)$"{tipo.Name} {banco.Name} final {sufixo}";
        return new ContaBancaria(IdConta.FromNewVersion7Guid(), idUsuario, banco, tipo, sufixo, apelido);
    }
}
