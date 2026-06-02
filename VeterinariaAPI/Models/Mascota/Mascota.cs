namespace VeterinariaAPI.Models.Mascota;

public class Mascota
{
    public long IdMascota { get; set; }
    public string? Nombre { get; set; }
    public string? Especie { get; set; }
    public string? Raza { get; set; }
    public DateTime FechaNacimiento { get; set; }

    public string EstadoMascota { get; set; } = "A"; // "A" = Activa, "I" = Inactiva
}