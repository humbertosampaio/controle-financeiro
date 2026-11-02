using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Tests.Shared;

public enum FormaDocumento
{
    CpfPuro,
    CpfComPontuacao,
    CpfComEspacos,
    CnpjPuro,
    CnpjComPontuacao,
    CnpjComEspacos,
    CnpjComLetras,
    CnpjComLetrasComPontuacao
}

/// <summary>Um CPF e dois CNPJs aleatórios, escritos em cada forma que um banco pode trazer.</summary>
public sealed class DocumentosDeTeste
{
    private readonly string _cpf;
    private readonly string _cnpjNumerico;
    private readonly string _cnpjComLetras;

    public DocumentosDeTeste(Cpf cpf, CnpjNumerico cnpjNumerico, Cnpj cnpjComLetras)
    {
        _cpf = cpf;
        _cnpjNumerico = cnpjNumerico.Cnpj;
        _cnpjComLetras = cnpjComLetras;
    }

    /// <returns>O rótulo que a máscara dá ao documento nessa forma.</returns>
    public static string Rotulo(FormaDocumento forma) =>
        forma is FormaDocumento.CpfPuro or FormaDocumento.CpfComPontuacao or FormaDocumento.CpfComEspacos ? "[CPF]" : "[CNPJ]";

    public string Em(FormaDocumento forma)
    {
        return forma switch
        {
            FormaDocumento.CpfPuro => _cpf,
            FormaDocumento.CpfComPontuacao => $"{_cpf[..3]}.{_cpf[3..6]}.{_cpf[6..9]}-{_cpf[9..]}",
            FormaDocumento.CpfComEspacos => $"{_cpf[..3]} {_cpf[3..6]} {_cpf[6..9]} {_cpf[9..]}",
            FormaDocumento.CnpjPuro => _cnpjNumerico,
            FormaDocumento.CnpjComPontuacao => ComPontuacao(_cnpjNumerico),
            FormaDocumento.CnpjComEspacos => $"{_cnpjNumerico[..2]} {_cnpjNumerico[2..5]} {_cnpjNumerico[5..8]} {_cnpjNumerico[8..12]} {_cnpjNumerico[12..]}",
            FormaDocumento.CnpjComLetras => _cnpjComLetras,
            FormaDocumento.CnpjComLetrasComPontuacao => ComPontuacao(_cnpjComLetras),
            _ => throw new ArgumentOutOfRangeException(nameof(forma), forma, message: null)
        };
    }

    private static string ComPontuacao(string cnpj) => $"{cnpj[..2]}.{cnpj[2..5]}.{cnpj[5..8]}/{cnpj[8..12]}-{cnpj[12..]}";
}
