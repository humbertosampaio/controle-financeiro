using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Tests.Shared;

/// <summary>Um <see cref="Domain.Shared.ValueObjects.Cnpj"/> só de dígitos, para o teste que precisa dessa forma: o generator do <c>Cnpj</c> sorteia com letras.</summary>
public sealed record CnpjNumerico(Cnpj Cnpj);
