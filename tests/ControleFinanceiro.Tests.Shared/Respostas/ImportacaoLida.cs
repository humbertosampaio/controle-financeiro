using System.Text.Json.Nodes;

namespace ControleFinanceiro.Tests.Shared.Respostas;

/// <summary>A importação de um extrato ou de uma fatura, como o modelo a recebe na prévia e na confirmação.</summary>
public sealed class ImportacaoLida
{
    public int Lidas { get; }
    public int Novas { get; }
    public int JaExistentes { get; }

    /// <summary>Nulo na fatura.</summary>
    public bool? ContaNova { get; }

    /// <summary>Os finais dos cartões da fatura, na ordem da resposta; vazio no extrato.</summary>
    public IReadOnlyList<string> Finais { get; }

    public int DespesasParceladas { get; }
    public int DespesasAdiadas { get; }
    public string Resumo { get; }

    /// <summary>Os avisos fixos, que não impedem a importação; vazio quando não há.</summary>
    public IReadOnlyList<string> Avisos { get; }

    public ImportacaoLida(JsonObject json)
    {
        Lidas = json["lidas"]!.GetValue<int>();
        Novas = json["novas"]!.GetValue<int>();
        JaExistentes = json["ja_existentes"]!.GetValue<int>();
        ContaNova = json["conta_nova"]?.GetValue<bool>();

        IReadOnlyList<JsonObject> cartoes = json["cartoes"]?.AsArray().Select(cartao => cartao!.AsObject()).ToArray() ?? [];
        Finais = cartoes.Select(cartao => cartao.Texto("final")).ToArray();
        DespesasParceladas = cartoes.Sum(cartao => cartao["despesas_parceladas"]!.GetValue<int>());
        DespesasAdiadas = cartoes.Sum(cartao => cartao["despesas_adiadas"]!.GetValue<int>());

        Resumo = json.Texto("resumo");
        Avisos = json["avisos"]?.AsArray().Select(aviso => aviso!.GetValue<string>()).ToArray() ?? [];
    }
}
