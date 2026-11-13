using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Application.IA.Saida;

/// <summary>As falhas previstas que vão ao modelo, cada uma com a mensagem fixa, agrupadas pelo contexto em que nascem.</summary>
public static class ErrosParaModelo
{
    /// <summary>O que o modelo pediu e não existe, ou não é válido, em qualquer ferramenta.</summary>
    public static class Consulta
    {
        public static readonly ErroParaModelo MesInvalido = Fixo("Mês inválido: o mês vai de 1 a 12, e o ano, de 1 a 9999.");

        public static readonly ErroParaModelo UsuarioNaoEncontrado = Fixo("O usuário configurado no servidor não foi encontrado.");

        public static readonly ErroParaModelo TransacaoNaoEncontrada = Fixo("A transação não foi encontrada: use o id de uma transação de listar_transacoes.");

        public static readonly ErroParaModelo DespesaNaoEncontrada = Fixo("A despesa não foi encontrada: use o id de uma despesa de listar_despesas_fixas.");

        public static readonly ErroParaModelo CartaoNaoEncontrado = Fixo("O cartão não foi encontrado: use o id de um cartão de listar_cartoes.");
    }

    /// <summary>O vínculo entre uma transação e a despesa que ela paga ou cobra, também no pagamento da fatura.</summary>
    public static class Vinculo
    {
        public static readonly ErroParaModelo TransacaoJaVinculada = Fixo("A transação já paga ou cobra uma despesa, e cada transação é ligada a uma só.");

        public static readonly ErroParaModelo DespesaJaPaga = Fixo("A despesa já foi paga por outra saída.");

        public static readonly ErroParaModelo DespesaJaCobrada = Fixo("A despesa já foi cobrada por outra compra do cartão.");

        public static readonly ErroParaModelo TransacaoNaoEhSaida = Fixo("A transação é uma entrada: só uma saída paga despesa.");

        public static readonly ErroParaModelo OrigemIncompativel = Fixo("A origem da transação não combina com a despesa: a despesa sem cartão é paga por uma saída da conta, a do crédito é cobrada por uma compra do cartão dela, e a fatura do cartão é paga por uma saída da conta.");

        public static readonly ErroParaModelo ParcelaIncompativel = Fixo("A parcela da transação não combina com a despesa: só a despesa parcelada é cobrada por parcela, e só pela parcela da mesma compra.");

        public static readonly ErroParaModelo ResumoDivergente = Fixo("O resumo não corresponde ao vínculo desses ids, e nada foi gravado: chame prever_vinculo de novo, mostre o novo resumo ao usuário e confirme com ele sem alteração.");
    }

    /// <summary>O pagamento da fatura do cartão por uma saída da conta.</summary>
    public static class PagamentoFatura
    {
        public static readonly ErroParaModelo TransacaoPagaOutraFatura = Fixo("A saída já pagou a fatura de outro cartão ou de outro mês de vencimento, e cada saída paga uma fatura só.");

        public static readonly ErroParaModelo FaturaSemCobrancaPendente = Fixo("A fatura não tem cobrança pendente nesse mês de vencimento: confira o cartão e o mês em que a fatura vence, e se a fatura já foi importada.");

        public static readonly ErroParaModelo ResumoDivergente = Fixo("O resumo não corresponde ao pagamento da fatura com essa saída, esse cartão e esse mês, e nada foi gravado: chame prever_pagamento_fatura de novo, mostre o novo resumo ao usuário e confirme com ele sem alteração.");
    }

    /// <summary>A importação de um extrato ou de uma fatura: a origem do arquivo, a leitura dele e o que falta cadastrar.</summary>
    public static class Importacao
    {
        public static readonly ErroParaModelo OrigemIndisponivel = Fixo("A pasta de origem da importação não existe no servidor: peça ao usuário para criar a pasta dados/ do repositório ou configurar Mcp:OrigemArquivoImportacao.");

        public static readonly ErroParaModelo ArquivoForaDaOrigem = Fixo("O caminho não aponta para um arquivo dentro da pasta de origem da importação, a dados/ do repositório: peça ao usuário o caminho de um arquivo dessa pasta, sem passar por links.");

        public static readonly ErroParaModelo ExtensaoNaoAceita = Fixo("A importação só lê arquivos .csv.");

        public static readonly ErroParaModelo ArquivoNaoEncontrado = Fixo("O arquivo não existe na pasta de origem da importação: confira o caminho com o usuário.");

        public static readonly ErroParaModelo ArquivoGrandeDemais = Fixo("O arquivo passa de 5 MB, o tamanho máximo da importação.");

        public static readonly ErroParaModelo LayoutInvalido = Fixo("Layout inválido: use um dos códigos de layout da descrição da ferramenta.");

        public static readonly ErroParaModelo SufixoContaInvalido = Fixo("O layout mercado-pago-conta-csv pede o sufixo da conta: os 4 últimos caracteres do número da conta, que o usuário informa.");

        public static readonly ErroParaModelo ContaDeOutroTipo = Fixo("O usuário já tem uma conta nesse banco com esse final, mas de outro tipo: peça a ele para resolver pela API antes de importar.");

        public static readonly ErroParaModelo CartoesNaoResolvidos = Fixo("A fatura tem finais de cartão sem cartão cadastrado, em pendencias.finais_sem_cartao, e nada foi importado: peça ao usuário para cadastrar os cartões pela API e importe de novo.");

        public static readonly ErroParaModelo BeneficiariosNaoResolvidos = Fixo("O arquivo tem textos de beneficiário que ainda não são alias de nenhum beneficiário, contados em pendencias.beneficiarios, e nada foi importado: peça ao usuário para cadastrar os aliases pela API, que lista os textos na prévia da importação, e importe de novo.");

        public static readonly ErroParaModelo CategoriasNaoResolvidas = Fixo("O arquivo tem categorias do banco que ainda não são alias de nenhuma categoria, contadas em pendencias.categorias, e nada foi importado: peça ao usuário para cadastrar os aliases pela API, que lista as categorias na prévia da importação, e importe de novo.");

        public static readonly ErroParaModelo IdentificadorRepetido = Fixo("O arquivo repete o identificador de um lançamento, e nada foi importado: peça ao usuário para conferir o arquivo.");

        public static readonly ErroParaModelo ResumoDivergente = Fixo("O resumo não corresponde à importação desse arquivo, e nada foi gravado: o arquivo mudou ou já foi importado. Chame prever_importacao de novo, mostre o novo resumo ao usuário e confirme com ele sem alteração.");

        private static readonly Dictionary<FalhaOrigemArquivoImportacao, ErroParaModelo> _porFalhaOrigem = new()
        {
            [FalhaOrigemArquivoImportacao.OrigemIndisponivel] = OrigemIndisponivel,
            [FalhaOrigemArquivoImportacao.ForaDaOrigem] = ArquivoForaDaOrigem,
            [FalhaOrigemArquivoImportacao.ExtensaoNaoAceita] = ExtensaoNaoAceita,
            [FalhaOrigemArquivoImportacao.NaoEncontrado] = ArquivoNaoEncontrado,
            [FalhaOrigemArquivoImportacao.GrandeDemais] = ArquivoGrandeDemais,
        };

        /// <summary>O fim da frase que começa com "A linha N do arquivo" ou com "O arquivo".</summary>
        private static readonly Dictionary<TipoFalhaLeituraArquivo, string> _fimPorTipoFalhaLeitura = new()
        {
            [TipoFalhaLeituraArquivo.ForaDoLayout] = "não está no layout escolhido, e nada foi importado: confira o layout com o usuário.",
            [TipoFalhaLeituraArquivo.SemColunas] = "não traz as colunas do layout escolhido, e nada foi importado: confira o layout com o usuário.",
            [TipoFalhaLeituraArquivo.LancamentoInvalido] = "traz um lançamento que a importação não aceita, e nada foi importado: peça ao usuário para conferir essa linha.",
            [TipoFalhaLeituraArquivo.ConteudoInvalido] = "não fecha com o layout escolhido, no cabeçalho, nos saldos ou nos lançamentos, e nada foi importado: peça ao usuário para importar pela API, que mostra o motivo.",
        };

        /// <summary>A mensagem fixa de quando a origem não abriu o arquivo, sem repetir o caminho.</summary>
        public static ErroParaModelo De(FalhaOrigemArquivoImportacao falha) => _porFalhaOrigem[falha];

        /// <summary>
        /// A mensagem fixa de quando o leitor recusou o arquivo: só o número da linha, quando há um, e o tipo da falha. A
        /// mensagem do leitor fica de fora, porque pode citar datas do arquivo.
        /// </summary>
        public static ErroParaModelo ArquivoInvalido(FalhaLeituraArquivo falha)
        {
            string inicio = falha.Linha is long linha ? $"A linha {linha} do arquivo" : "O arquivo";
            return Fixo($"{inicio} {_fimPorTipoFalhaLeitura[falha.Tipo]}");
        }
    }

    /// <summary>
    /// A mensagem fixa de um erro do domínio, sem a descrição dele nem a despesa que já usa a transação.
    /// </summary>
    /// <exception cref="InvalidOperationException">Quando o erro não tem mensagem mapeada.</exception>
    public static ErroParaModelo De(Erro erro)
    {
        return erro switch
        {
            ErroTransacaoJaVinculada => Vinculo.TransacaoJaVinculada,
            ErroDespesaJaPaga => Vinculo.DespesaJaPaga,
            ErroDespesaJaCobrada => Vinculo.DespesaJaCobrada,
            ErroTransacaoNaoEhSaida => Vinculo.TransacaoNaoEhSaida,
            ErroOrigemIncompativelComDespesa => Vinculo.OrigemIncompativel,
            ErroParcelaIncompativelComDespesa => Vinculo.ParcelaIncompativel,
            ErroTransacaoPagaOutraFatura => PagamentoFatura.TransacaoPagaOutraFatura,
            ErroFaturaSemCobrancaPendente => PagamentoFatura.FaturaSemCobrancaPendente,
            ErroContaDeOutroTipo => Importacao.ContaDeOutroTipo,
            ErroCartoesNaoResolvidos => Importacao.CartoesNaoResolvidos,
            ErroBeneficiariosNaoResolvidos => Importacao.BeneficiariosNaoResolvidos,
            ErroCategoriasBancoNaoResolvidas => Importacao.CategoriasNaoResolvidas,
            ErroIdentificadorRepetido => Importacao.IdentificadorRepetido,
            _ => throw new InvalidOperationException($"Erro sem mensagem mapeada: {erro}"),
        };
    }

    /// <summary>Para mensagem escrita aqui, sem dado do usuário: ela passa pela máscara como qualquer texto.</summary>
    private static ErroParaModelo Fixo(string mensagem) => new((DocumentoMascarado)mensagem);
}
