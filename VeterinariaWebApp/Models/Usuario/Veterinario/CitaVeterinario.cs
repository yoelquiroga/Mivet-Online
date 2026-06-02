using System.ComponentModel;

namespace VeterinariaWebApp.Models.Usuario.Veterinario;

public class CitaVeterinario
{
    [DisplayName("CÓDIGO CITA")]
    public long ide_cit { get; set; }

    [DisplayName("FECHA")]
    public DateTime cal_cit { get; set; }

    [DisplayName("CONSULTORIO")]
    public int con_cit { get; set; }

    [DisplayName("MASCOTA")]
    public string mascota { get; set; }

    [DisplayName("ESPECIE")]
    public string especie { get; set; }

    [DisplayName("DOC. DUEÑO")]
    public string doc_dueno { get; set; }

    [DisplayName("DUEÑO")]
    public string nombre_dueno { get; set; }

    [DisplayName("MONTO")]
    public decimal mon_pag { get; set; }

    [DisplayName("MÉTODO PAGO")]
    public string nom_pay { get; set; }

    [DisplayName("ESTADO")]
    public string est_cit { get; set; } = "P"; // P=Pendiente, E=EnAtención, A=Atendida, C=Cancelada

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