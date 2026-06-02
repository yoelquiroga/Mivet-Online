using System.ComponentModel;

namespace VeterinariaWebApp.Models.Usuario.Veterinario;

public class Veterinario : Usuario
{
    [DisplayName("ID")]
    public long IdVeterinario { get; set; }

    [DisplayName("Sueldo")]
    public decimal sueldo { get; set; }

    [DisplayName("Especialidad")]
    public string? especialidad { get; set; }
}