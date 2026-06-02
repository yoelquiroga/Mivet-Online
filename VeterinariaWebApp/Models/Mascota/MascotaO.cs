using System.ComponentModel;

namespace VeterinariaWebApp.Models.Mascota;

public class MascotaO
{
    [DisplayName("ID MASCOTA")]
    public long ide_mas { get; set; }

    [DisplayName("NOMBRE")]
    public string nom_mas { get; set; } = string.Empty;

    [DisplayName("ESPECIE")]
    public string esp_mas { get; set; } = string.Empty;

    [DisplayName("RAZA")]
    public string raz_mas { get; set; } = string.Empty;

    [DisplayName("FECHA NACIMIENTO")]
    public DateTime fna_mas { get; set; }

    [DisplayName("ID CLIENTE")]
    public long ide_cli { get; set; }
}