using System.ComponentModel;

namespace VeterinariaWebApp.Models.Cita;

public class Cita
{
    [DisplayName("ID")]
    public long IdCita { get; set; }

    [DisplayName("Fecha")]
    public DateTime CalendarioCita { get; set; }

    [DisplayName("Nro. Consultorio")]
    public long Consultorio { get; set; }

    [DisplayName("Veterinario")]
    public string? NombreVeterinario { get; set; }

    [DisplayName("Mascota")]
    public string? NombreMascota { get; set; }

    [DisplayName("Precio")]
    public decimal MontoPago { get; set; }

    [DisplayName("Estado")]
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