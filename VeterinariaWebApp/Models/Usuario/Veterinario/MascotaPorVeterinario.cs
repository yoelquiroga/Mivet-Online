using System.ComponentModel;

namespace VeterinariaWebApp.Models.Usuario.Veterinario;

public class MascotaPorVeterinario
{
    [DisplayName("ID MASCOTA")]
    public long ide_mas { get; set; }

    [DisplayName("MASCOTA")]
    public string Mascota { get; set; }

    [DisplayName("ESPECIE")]
    public string Especie { get; set; }

    [DisplayName("RAZA")]
    public string Raza { get; set; }

    [DisplayName("DOC DUEÑO")]
    public string Doc_Dueño { get; set; }

    [DisplayName("FEC NAC DUEÑO")]
    public DateTime Fec_Nac_Dueño { get; set; }

    [DisplayName("TIPO DOC")]
    public string Tipo_Doc { get; set; }

    [DisplayName("TOTAL CITA")]
    public int Total_Citas { get; set; }
}