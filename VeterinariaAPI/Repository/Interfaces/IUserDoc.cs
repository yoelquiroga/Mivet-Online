using VeterinariaAPI.Models.Usuario;

namespace VeterinariaAPI.Repository.Interfaces;

public interface IUserDoc
{
    IEnumerable<UserDoc> ListarTiposDeDocumento();
}