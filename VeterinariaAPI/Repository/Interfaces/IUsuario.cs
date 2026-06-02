using VeterinariaAPI.Models.Usuario;

namespace VeterinariaAPI.Repository.Interfaces;

public interface IUsuario
{
    string verificarLogin(string uid, string pwd);
    string obtenerIdUsuario(string correo);
}