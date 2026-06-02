namespace VeterinariaAPI.Models.Mascota;

public class MascotaO
{
    public long ide_mas { get; set; } 
    public string nom_mas { get; set; }
    public string esp_mas { get; set; } 
    public string raz_mas { get; set; } 
    public DateTime fna_mas { get; set; } 
    public long ide_cli { get; set; } // ID del cliente (dueño)

   
}