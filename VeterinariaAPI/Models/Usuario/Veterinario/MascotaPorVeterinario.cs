namespace VeterinariaAPI.Models.Usuario.Veterinario;

public class MascotaPorVeterinario
{
    public long ide_mas { get; set; } 
    public string Mascota { get; set; }
    public string Especie { get; set; }
    public string Raza { get; set; }
    public string Doc_Dueño { get; set; } 
    public int Total_Citas { get; set; }
}