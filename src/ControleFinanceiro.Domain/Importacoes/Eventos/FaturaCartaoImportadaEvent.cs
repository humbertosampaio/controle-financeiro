using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Importacoes.Eventos;

/// <summary>A fatura de um cartão foi importada, registrada pela <see cref="ImportacaoFatura"/> que nasceu dela.</summary>
[DomainEventName("fatura-cartao-importada")]
public sealed record FaturaCartaoImportadaEvent(IdUsuario IdUsuario, IdImportacao IdImportacao, IdCartao IdCartao)
    : IDomainEvent, IPertenceAoUsuario;
