using System.ComponentModel;

namespace VeterinariaWebApp.Models.Usuario.Veterinario;

public class Especialidad
{
    public long ide_esp { get; set; }

    [DisplayName("Especialidad")]
    public string nom_esp { get; set; }
}