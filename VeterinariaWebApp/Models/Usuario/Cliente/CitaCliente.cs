using System.ComponentModel;

namespace VeterinariaWebApp.Models.Usuario.Cliente;

public class CitaCliente
{
    [DisplayName("ID CITA")]
    public long ide_cit { get; set; }

    [DisplayName("FECHA")]
    public DateTime cal_cit { get; set; }

    [DisplayName("CONSULTORIO")]
    public int con_cit { get; set; }

    [DisplayName("VETERINARIO")]
    public string veterinario { get; set; }

    [DisplayName("ESPECIALIDAD")]
    public string especialidad { get; set; }

    [DisplayName("MASCOTA")]
    public string mascota { get; set; }

    [DisplayName("ESPECIE")]
    public string especie { get; set; }

    public string raza { get; set; }

    [DisplayName("MONTO A PAGAR")]
    public decimal mon_pag { get; set; }

    public string metodo_pago { get; set; }

    [DisplayName("ESTADO")]
    public string est_cit { get; set; } = "P"; // P=Pendiente, E=EnAtención, A=Atendida, C=Cancelada

    // ========== HISTORIAL MÉDICO (solo cuando est_cit = 'A') ==========
    public string? sintomas { get; set; }
    public string? diagnostico { get; set; }
    public string? tratamiento { get; set; }
    public string? medicamentos { get; set; }
    public string? observaciones { get; set; }
    public DateTime? fecha_atencion { get; set; }

    // Indica si tiene historial médico
    public bool TieneHistorial => !string.IsNullOrEmpty(diagnostico);

    // Propiedad calculada para mostrar el estado en texto
    public string EstadoDescripcion => est_cit switch
    {
        "P" => "Pendiente",
        "E" => "En Atención",
        "A" => "Atendida",
        "C" => "Cancelada",
        _ => "Desconocido"
    };

    // Propiedad para obtener el color del badge según estado
    public string EstadoColor => est_cit switch
    {
        "P" => "warning",      // Amarillo
        "E" => "info",         // Azul
        "A" => "success",      // Verde
        "C" => "danger",       // Rojo
        _ => "secondary"
    };

    // Propiedad para obtener el icono según estado
    public string EstadoIcono => est_cit switch
    {
        "P" => "fa-clock",
        "E" => "fa-stethoscope",
        "A" => "fa-check-circle",
        "C" => "fa-times-circle",
        _ => "fa-question"
    };

    // Emoji de especie
    public string EspecieEmoji => especie?.ToLower() switch
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