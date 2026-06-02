namespace VeterinariaAPI.Models.Cita;

public class Cita
{
    public long IdCita { get; set; }
    public DateTime CalendarioCita { get; set; }
    public long Consultorio { get; set; }
    public string? NombreVeterinario { get; set; }
    public string? NombreMascota { get; set; }
    public decimal MontoPago { get; set; }
    public string EstadoCita { get; set; } = "P"; // P=Pendiente, E=EnAtención, A=Atendida, C=Cancelada

    // Propiedad calculada para mostrar el estado en texto
    public string EstadoDescripcion => EstadoCita switch
    {
        "P" => "Pendiente",
        "E" => "En Atención",
        "A" => "Atendida",
        "C" => "Cancelada",
        _ => "Desconocido"
    };
}