using VeterinariaAPI.Models.Usuario.Veterinario;

public interface IVeterinario
{
    IEnumerable<Veterinario> ListarVeterinariosFront();
    IEnumerable<VeterinarioO> ListarVeterinariosBack();
    string AgregarVeterinario(VeterinarioO veterinario);
    Veterinario BuscarVeterinarioPorID(long id);
    string ActualizarVeterinarioPorID(VeterinarioO veterinario);
    string EliminarVeterinarioPorID(long id);
    IEnumerable<CitaVeterinario> ListarCitasPorVeterinario(long ide_usr);
    VeterinarioStats ObtenerEstadisticasVeterinario(long ide_usr);
    IEnumerable<MascotaPorVeterinario> ListarMascotasPorVeterinario(long ide_usr);
}