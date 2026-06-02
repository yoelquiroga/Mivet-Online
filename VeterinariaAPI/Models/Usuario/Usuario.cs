namespace VeterinariaAPI.Models.Usuario;

public class Usuario
{
    public string? NombreUsuario { get; set; }
    public string? ApellidoUsuario { get; set; }
    public DateTime FechaNacimiento { get; set; }
    public string? TipoDocumento { get; set; }
    public string? NumeroDocumento { get; set; }
    public string? Rol { get; set; }
}