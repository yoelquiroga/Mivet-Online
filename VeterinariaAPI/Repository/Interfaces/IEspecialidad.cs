using VeterinariaAPI.Models.Usuario.Veterinario;

namespace VeterinariaAPI.Repository.Interfaces;

public interface IEspecialidad
{
    IEnumerable<Especialidad> listarEspecialidad();
}