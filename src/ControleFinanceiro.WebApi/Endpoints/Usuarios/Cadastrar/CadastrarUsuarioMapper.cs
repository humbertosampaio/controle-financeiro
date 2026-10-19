using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;

public sealed class CadastrarUsuarioMapper : Mapper<CadastrarUsuarioRequest, CadastrarUsuarioResponse, Usuario>
{
    public override Usuario ToEntity(CadastrarUsuarioRequest r)
    {
        IEnumerable<DescontoSalarial> descontosSalariais = r.DescontosSalariais
            .Select(d => DescontoSalarial.Criar((TextoObrigatorio)d.Descricao, (ReaisPositivo)d.Valor));

        return Usuario.Criar(
            (TextoObrigatorio)r.Nome,
            (ReaisPositivo)r.RendaBruta,
            descontosSalariais);
    }

    public override CadastrarUsuarioResponse FromEntity(Usuario e) => new(e.Id.Value);
}