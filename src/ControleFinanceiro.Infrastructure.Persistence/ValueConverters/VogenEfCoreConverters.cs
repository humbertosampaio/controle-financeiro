using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using Vogen;

namespace ControleFinanceiro.Infrastructure.Persistence.ValueConverters;

[EfCoreConverter<IdUsuario>]
[EfCoreConverter<IdDescontoSalarial>]
[EfCoreConverter<IdCartao>]
[EfCoreConverter<IdCategoriaDespesa>]
[EfCoreConverter<IdDespesa>]
[EfCoreConverter<IdBeneficiario>]
[EfCoreConverter<IdConta>]
[EfCoreConverter<IdTransacao>]
[EfCoreConverter<IdImportacao>]
internal partial class VogenEfCoreConverters;
