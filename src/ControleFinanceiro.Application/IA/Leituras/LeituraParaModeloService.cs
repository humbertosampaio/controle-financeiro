using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Application.IA.Leituras;

/// <summary>
/// As leituras que vão ao modelo, já como tipos de saída: quem fala com o modelo chama este serviço e não toca nos
/// agregados. Os repositórios enxergam só o usuário atual. O mês vem do modelo e pode ser inválido: a falha volta no
/// envelope, com mensagem fixa, e não como exceção.
/// </summary>
public sealed class LeituraParaModeloService
{
    private readonly ITransacaoRepository _transacoes;
    private readonly IDespesaRecorrenteRepository _recorrentes;
    private readonly IDespesaRepository _despesas;
    private readonly IUsuarioRepository _usuarios;
    private readonly IUsuarioAtual _usuarioAtual;
    private readonly ICartaoRepository _cartoes;

    public LeituraParaModeloService(
        ITransacaoRepository transacoes,
        IDespesaRecorrenteRepository recorrentes,
        IDespesaRepository despesas,
        IUsuarioRepository usuarios,
        IUsuarioAtual usuarioAtual,
        ICartaoRepository cartoes)
    {
        _transacoes = transacoes;
        _recorrentes = recorrentes;
        _despesas = despesas;
        _usuarios = usuarios;
        _usuarioAtual = usuarioAtual;
        _cartoes = cartoes;
    }

    public async Task<TransacoesDoMesParaModelo> ListarTransacoesAsync(int ano, int mes, CancellationToken cancellationToken)
    {
        Result<MesReferencia> mesValido = MesReferencia.Criar(mes, ano);
        if (mesValido.IsFailure)
            return TransacoesDoMesParaModelo.Falha(ErrosParaModelo.Consulta.MesInvalido);

        MesReferencia mesReferencia = mesValido.Value;

        IReadOnlyList<Transacao> transacoes = await _transacoes.ListarDoMesAsync(mesReferencia, cancellationToken);

        return TransacoesDoMesParaModelo.De(
            mesReferencia,
            transacoes.Select(transacao => transacao.ParaModelo()).ToArray());
    }

    /// <summary>
    /// Toda despesa recorrente do usuário, com a despesa que ela gerou no mês, quando já gerou: um mês ainda não gerado
    /// mostra as recorrentes sem despesa, e não uma lista vazia.
    /// </summary>
    public async Task<DespesasFixasDoMesParaModelo> ListarDespesasFixasAsync(int ano, int mes, CancellationToken cancellationToken)
    {
        Result<MesReferencia> mesValido = MesReferencia.Criar(mes, ano);
        if (mesValido.IsFailure)
            return DespesasFixasDoMesParaModelo.Falha(ErrosParaModelo.Consulta.MesInvalido);

        MesReferencia mesReferencia = mesValido.Value;

        IReadOnlyList<DespesaRecorrente> recorrentes = await _recorrentes.ListarAsync(cancellationToken);
        IReadOnlyList<Despesa> despesasDoMes = await _despesas.ListarDoMesAsync(mesReferencia, cancellationToken);

        // O índice único (id_despesa_recorrente, mes_referencia) garante no máximo uma despesa por recorrente no mês.
        Dictionary<IdDespesaRecorrente, Despesa> geradas = despesasDoMes
            .Where(despesa => despesa.IdDespesaRecorrente is not null)
            .ToDictionary(despesa => despesa.IdDespesaRecorrente!.Value);

        return DespesasFixasDoMesParaModelo.De(
            mesReferencia,
            recorrentes
                .Select(recorrente => recorrente.ParaModelo(geradas.GetValueOrDefault(recorrente.Id), mesReferencia))
                .ToArray());
    }

    /// <summary>
    /// O balanço do mês pela renda líquida atual do usuário e pelos pagamentos das despesas do mês, inclusive a parcela
    /// da parcelada que cai nele. Só totais: nem o nome do usuário nem os descontos vão ao modelo.
    /// </summary>
    public async Task<BalancoMensalParaModelo> ObterBalancoAsync(int ano, int mes, CancellationToken cancellationToken)
    {
        Result<MesReferencia> mesValido = MesReferencia.Criar(mes, ano);
        if (mesValido.IsFailure)
            return BalancoMensalParaModelo.Falha(ErrosParaModelo.Consulta.MesInvalido);

        MesReferencia mesReferencia = mesValido.Value;

        Maybe<Usuario> usuario = await _usuarios.ObterPorIdAsync(_usuarioAtual.Id, cancellationToken);
        if (usuario.HasNoValue)
            return BalancoMensalParaModelo.Falha(ErrosParaModelo.Consulta.UsuarioNaoEncontrado);

        IReadOnlyList<Despesa> despesasDoMes = await _despesas.ListarDoMesAsync(mesReferencia, cancellationToken);

        return BalancoMensalService
            .Calcular(usuario.Value, despesasDoMes, mesReferencia)
            .ParaModelo();
    }

    /// <summary>
    /// Todos os cartões do usuário, com o final, para o usuário reconhecer o cartão, e nunca o número inteiro: o modelo
    /// escolhe o da fatura pelo banco e pelos dias.
    /// </summary>
    public async Task<CartoesParaModelo> ListarCartoesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Cartao> cartoes = await _cartoes.ListarAsync(cancellationToken);

        return CartoesParaModelo.De(cartoes.Select(cartao => cartao.ParaModelo()).ToArray());
    }
}
