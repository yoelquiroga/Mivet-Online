using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace VeterinariaWebApp.Models.Usuario.Veterinario;

/// <summary>
/// ViewModel para el formulario de atención de cita del veterinario
/// </summary>
public class AtencionCitaViewModel
{
    // Datos de la cita (solo lectura)
    public long IdCita { get; set; }
    public DateTime FechaCita { get; set; }
    public int Consultorio { get; set; }

    // Datos de la mascota (solo lectura)
    public string NombreMascota { get; set; }
    public string Especie { get; set; }
    public string Raza { get; set; }

    // Datos del dueño (solo lectura)
    public string NombreDueno { get; set; }
    public string DocumentoDueno { get; set; }

    // Datos del pago (solo lectura)
    public decimal MontoPago { get; set; }
    public string MetodoPago { get; set; }

    // Estado actual
    public string EstadoCita { get; set; }

    // ========== CAMPOS DE ATENCIÓN (editables) ==========

    [DisplayName("Síntomas Observados")]
    [StringLength(500, ErrorMessage = "Máximo 500 caracteres")]
    public string Sintomas { get; set; }

    [DisplayName("Diagnóstico")]
    [Required(ErrorMessage = "El diagnóstico es obligatorio")]
    [StringLength(500, ErrorMessage = "Máximo 500 caracteres")]
    public string Diagnostico { get; set; }

    [DisplayName("Tratamiento")]
    [Required(ErrorMessage = "El tratamiento es obligatorio")]
    [StringLength(500, ErrorMessage = "Máximo 500 caracteres")]
    public string Tratamiento { get; set; }

    [DisplayName("Medicamentos Recetados")]
    [StringLength(500, ErrorMessage = "Máximo 500 caracteres")]
    public string Medicamentos { get; set; }

    [DisplayName("Observaciones Adicionales")]
    [StringLength(1000, ErrorMessage = "Máximo 1000 caracteres")]
    public string Observaciones { get; set; }

    [DisplayName("Próxima Cita Recomendada")]
    public DateTime? ProximaCita { get; set; }

    // Propiedades calculadas
    public string EstadoDescripcion => EstadoCita switch
    {
        "P" => "Pendiente",
        "E" => "En Atención",
        "A" => "Atendida",
        "C" => "Cancelada",
        _ => "Desconocido"
    };

    public string EspecieEmoji => Especie?.ToLower() switch
    {
        "perro" => "🐕",
        "gato" => "🐱",
        "ave" => "🐦",
        "conejo" => "🐰",
        "hámster" => "🐹",
        "pez" => "🐠",
        "tortuga" => "🐢",
        _ => "🐾"
    };
}
