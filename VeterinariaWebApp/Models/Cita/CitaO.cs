using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace VeterinariaWebApp.Models.Cita;

public class CitaO
{
    public long IdCita { get; set; }

    [Required(ErrorMessage = "La fecha y hora son obligatorias.")]
    [Display(Name = "Fecha y Hora de la Cita")]
    [FutureDate(ErrorMessage = "La fecha debe ser futura.")]
    public DateTime CalendarioCita { get; set; }

    [Required(ErrorMessage = "Consultorio es requerido")]
    public long Consultorio { get; set; }

    [DisplayName("Veterinario")] 
    [Required(ErrorMessage = "Veterinario es requerido")]
    public long IdVeterinario { get; set; }

    [DisplayName("Mascota")] 
    [Required(ErrorMessage = "Mascota es requerida")]
    public long IdMascota { get; set; }

    [DisplayName("Pago")]
    [Required(ErrorMessage = "Pago es requerido")]
    public long IdPago { get; set; }
}