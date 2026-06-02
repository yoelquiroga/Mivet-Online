using VeterinariaAPI.Models.Mascota;

namespace VeterinariaAPI.Repository.Interfaces;

public interface IMascota
{
    IEnumerable<Mascota> ListarMascotasPorCliente(long id_usuario);
    string AgregarMascota(Mascota mascota);
}