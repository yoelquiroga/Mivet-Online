namespace VeterinariaAPI.Models.Cita;

public class EmailInasistenciaRequest
{
    public string Email { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;
    public string NombreMascota { get; set; } = string.Empty;
    public string FechaCita { get; set; } = string.Empty;
}
