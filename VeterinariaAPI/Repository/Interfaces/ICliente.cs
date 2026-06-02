using VeterinariaAPI.Models.Usuario.Cliente;
using VeterinariaAPI.Models.Cita;
using VeterinariaAPI.Models.Mascota;

namespace VeterinariaAPI.Repository.Interfaces;
public interface ICliente
{
    IEnumerable<Cliente> ListarClientes();
    IEnumerable<ClienteO> ListarClientesO();
    string GuardarClienteO(ClienteO cliente);
    Cliente BuscarClientePorID(long id);
    string ActualizarCliente(ClienteO cliente);
    string EliminarCliente(long id);

    IEnumerable<CitaCliente> ListarCitasPorCliente(long ide_usr);

    string AgregarMascota(Mascota mascota, long id_usuario);
    IEnumerable<Mascota> ListarMascotasPorCliente(long id_usuario);

    string ActualizarMascota(Mascota mascota);
    string EliminarMascota(long id_mascota, bool confirmar = false);


    MascotaConCliente ObtenerMascotaConCliente(long idMascota);

}