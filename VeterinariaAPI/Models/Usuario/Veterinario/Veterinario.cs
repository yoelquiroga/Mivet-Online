namespace VeterinariaAPI.Models.Usuario.Veterinario;

public class Veterinario : Usuario
{
    public long IdVeterinario { get; set; } 
    public decimal sueldo { get; set; }
    public string? especialidad { get; set; }
}