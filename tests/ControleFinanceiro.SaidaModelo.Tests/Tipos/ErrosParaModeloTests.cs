using AutoFixture.Xunit2;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ControleFinanceiro.Tests.Shared.Respostas;
using System.Text.Json.Nodes;

namespace ControleFinanceiro.SaidaModelo.Tests.Tipos;

public sealed class ErrosParaModeloTests
{
    public static readonly TheoryData<ErroParaModelo> Erros = new(
        ErrosParaModelo.Consulta.MesInvalido,
        ErrosParaModelo.Consulta.UsuarioNaoEncontrado,
        ErrosParaModelo.Consulta.TransacaoNaoEncontrada,
        ErrosParaModelo.Consulta.DespesaNaoEncontrada,
        ErrosParaModelo.Consulta.CartaoNaoEncontrado,
        ErrosParaModelo.Vinculo.TransacaoJaVinculada,
        ErrosParaModelo.Vinculo.DespesaJaPaga,
        ErrosParaModelo.Vinculo.DespesaJaCobrada,
        ErrosParaModelo.Vinculo.TransacaoNaoEhSaida,
        ErrosParaModelo.Vinculo.OrigemIncompativel,
        ErrosParaModelo.Vinculo.ParcelaIncompativel,
        ErrosParaModelo.PagamentoFatura.TransacaoPagaOutraFatura,
        ErrosParaModelo.PagamentoFatura.FaturaSemCobrancaPendente,
        ErrosParaModelo.Vinculo.ResumoDivergente,
        ErrosParaModelo.PagamentoFatura.ResumoDivergente,
        ErrosParaModelo.Importacao.OrigemIndisponivel,
        ErrosParaModelo.Importacao.ArquivoForaDaOrigem,
        ErrosParaModelo.Importacao.ExtensaoNaoAceita,
        ErrosParaModelo.Importacao.ArquivoNaoEncontrado,
        ErrosParaModelo.Importacao.ArquivoGrandeDemais,
        ErrosParaModelo.Importacao.LayoutInvalido,
        ErrosParaModelo.Importacao.SufixoContaInvalido,
        ErrosParaModelo.Importacao.ContaDeOutroTipo,
        ErrosParaModelo.Importacao.CartoesNaoResolvidos,
        ErrosParaModelo.Importacao.BeneficiariosNaoResolvidos,
        ErrosParaModelo.Importacao.CategoriasNaoResolvidas,
        ErrosParaModelo.Importacao.IdentificadorRepetido,
        ErrosParaModelo.Importacao.ResumoDivergente);

    public static readonly TheoryData<Erro, ErroParaModelo> ErrosDoDominio = new()
    {
        { ErroDespesaJaPaga.Instancia, ErrosParaModelo.Vinculo.DespesaJaPaga },
        { ErroDespesaJaCobrada.Instancia, ErrosParaModelo.Vinculo.DespesaJaCobrada },
        { ErroTransacaoNaoEhSaida.Instancia, ErrosParaModelo.Vinculo.TransacaoNaoEhSaida },
        { ErroOrigemIncompativelComDespesa.Instancia, ErrosParaModelo.Vinculo.OrigemIncompativel },
        { ErroParcelaIncompativelComDespesa.Instancia, ErrosParaModelo.Vinculo.ParcelaIncompativel },
        { ErroTransacaoPagaOutraFatura.Instancia, ErrosParaModelo.PagamentoFatura.TransacaoPagaOutraFatura },
        { ErroFaturaSemCobrancaPendente.Instancia, ErrosParaModelo.PagamentoFatura.FaturaSemCobrancaPendente },
        { ErroCartoesNaoResolvidos.Instancia, ErrosParaModelo.Importacao.CartoesNaoResolvidos },
        { ErroBeneficiariosNaoResolvidos.Instancia, ErrosParaModelo.Importacao.BeneficiariosNaoResolvidos },
        { ErroCategoriasBancoNaoResolvidas.Instancia, ErrosParaModelo.Importacao.CategoriasNaoResolvidas },
    };

    public static readonly TheoryData<FalhaOrigemArquivoImportacao, ErroParaModelo> FalhasDaOrigem = new()
    {
        { FalhaOrigemArquivoImportacao.OrigemIndisponivel, ErrosParaModelo.Importacao.OrigemIndisponivel },
        { FalhaOrigemArquivoImportacao.ForaDaOrigem, ErrosParaModelo.Importacao.ArquivoForaDaOrigem },
        { FalhaOrigemArquivoImportacao.ExtensaoNaoAceita, ErrosParaModelo.Importacao.ExtensaoNaoAceita },
        { FalhaOrigemArquivoImportacao.NaoEncontrado, ErrosParaModelo.Importacao.ArquivoNaoEncontrado },
        { FalhaOrigemArquivoImportacao.GrandeDemais, ErrosParaModelo.Importacao.ArquivoGrandeDemais },
    };

    [Theory, MemberData(nameof(Erros))]
    public void Serializar_Deve_TerSomenteAMensagem(ErroParaModelo erro)
    {
        JsonObject json = erro.Serializar();

        json.Select(campo => campo.Key).Should().Equal("mensagem");
        new ErroLido(json).Mensagem.Should().NotBeEmpty();
    }

    [Theory, MemberData(nameof(ErrosDoDominio))]
    public void De_Quando_ErroDoDominio_Deve_RetornarAMensagemFixa(Erro erro, ErroParaModelo esperado)
    {
        ErrosParaModelo.De(erro).Should().BeSameAs(esperado);
    }

    // O erro só nasce no serviço, com a despesa que já usa a transação, e ela não vai à mensagem.
    [Theory, UsuarioUnicoAutoData]
    public void De_Quando_TransacaoJaVinculada_Deve_RetornarAMensagemFixa(
        DespesaSimples despesa,
        DespesaSimples outra,
        TransacoesDeTeste transacoes,
        Lancamento lancamento)
    {
        Transacao transacao = transacoes.DaConta(lancamento.SemCategoria().ComoSaida());
        Erro erro = VinculoTransacaoService.Vincular(transacao, despesa, outra, cartoesDoUsuario: []).Error;

        ErrosParaModelo.De(erro).Should().BeSameAs(ErrosParaModelo.Vinculo.TransacaoJaVinculada);
    }

    [Fact]
    public void De_Quando_ErroSemMensagemMapeada_Deve_LancarExcecao()
    {
        Action act = () => ErrosParaModelo.De(ErroTransacaoNaoCobrouADespesa.Instancia);

        act.Should().ThrowExactly<InvalidOperationException>();
    }

    [Theory, DomainAutoData]
    public void De_Quando_ContaDeOutroTipo_Deve_RetornarAMensagemFixa(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Banco banco,
        [Frozen] SufixoConta sufixo,
        ContaBancaria existente)
    {
        TipoConta outroTipo = TipoConta.List.First(t => t != existente.Tipo);
        Erro erro = ContaBancariaFactory.ObterOuCriar(idUsuario, [existente], banco, outroTipo, sufixo).Error;

        ErrosParaModelo.De(erro).Should().BeSameAs(ErrosParaModelo.Importacao.ContaDeOutroTipo);
    }

    [Theory, DomainAutoData]
    public void De_Quando_IdentificadorRepetido_Deve_RetornarAMensagemFixa(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoArquivo importacao,
        Lancamento lancamento,
        IdentidadeLancamento identidade)
    {
        Lancamento repetido = lancamento.SemCategoria().ComIdentidade(identidade);
        Erro erro = LoteTransacoesFactory
            .Criar(idUsuario, beneficiariosDoUsuario: [], categoriasDoUsuario: [], conta, importacao, [repetido, repetido])
            .Error;

        ErrosParaModelo.De(erro).Should().BeSameAs(ErrosParaModelo.Importacao.IdentificadorRepetido);
    }

    [Theory, MemberData(nameof(FalhasDaOrigem))]
    public void ImportacaoDe_Quando_FalhaDaOrigem_Deve_RetornarAMensagemFixa(FalhaOrigemArquivoImportacao falha, ErroParaModelo esperado)
    {
        ErrosParaModelo.Importacao.De(falha).Should().BeSameAs(esperado);
    }

    [Fact]
    public void ArquivoInvalido_Quando_FalhaNumaLinha_Deve_CitarSoALinhaEOTipo()
    {
        FalhaLeituraArquivo falha = new(Linha: 12, TipoFalhaLeituraArquivo.LancamentoInvalido, "A linha 12 do arquivo tem entrada e saída no mesmo lançamento.");

        ErrosParaModelo.Importacao.ArquivoInvalido(falha).Mensagem.Should()
            .Be("A linha 12 do arquivo traz um lançamento que a importação não aceita, e nada foi importado: peça ao usuário para conferir essa linha.");
    }

    [Fact]
    public void ArquivoInvalido_Quando_FalhaDoArquivoInteiro_Deve_DeixarDeForaAMensagemDoLeitor()
    {
        FalhaLeituraArquivo falha = new(Linha: null, TipoFalhaLeituraArquivo.ConteudoInvalido, "O saldo do dia 02/03/2026 não fecha com os lançamentos do extrato.");

        ErrosParaModelo.Importacao.ArquivoInvalido(falha).Mensagem.Should()
            .StartWith("O arquivo não fecha com o layout escolhido")
            .And.NotContain("02/03/2026");
    }

    [Theory]
    [InlineData(nameof(TipoFalhaLeituraArquivo.ForaDoLayout))]
    [InlineData(nameof(TipoFalhaLeituraArquivo.SemColunas))]
    [InlineData(nameof(TipoFalhaLeituraArquivo.LancamentoInvalido))]
    [InlineData(nameof(TipoFalhaLeituraArquivo.ConteudoInvalido))]
    public void ArquivoInvalido_Deve_TerMensagemParaCadaTipo(string tipo)
    {
        FalhaLeituraArquivo falha = new(Linha: 3, TipoFalhaLeituraArquivo.FromName(tipo), "mensagem do leitor");

        ErrosParaModelo.Importacao.ArquivoInvalido(falha).Mensagem.Should().StartWith("A linha 3 do arquivo ").And.NotContain("mensagem do leitor");
    }
}
