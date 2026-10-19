using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Usuarios;
using Vogen;

namespace ControleFinanceiro.Infrastructure.Persistence.ValueConverters;

[EfCoreConverter<IdUsuario>]
[EfCoreConverter<IdDescontoSalarial>]
[EfCoreConverter<IdCartao>]
[EfCoreConverter<IdCategoriaDespesa>]
[EfCoreConverter<IdDespesa>]
[EfCoreConverter<IdBeneficiario>]
internal partial class VogenEfCoreConverters;
