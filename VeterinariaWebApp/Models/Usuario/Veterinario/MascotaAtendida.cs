using System.ComponentModel;

namespace VeterinariaWebApp.Models.Usuario.Veterinario;
/// <summary>
/// Modelo para mascotas atendidas por el veterinario, incluye historial médico
/// <summary>
public class MascotaAtendida
{
    public long ide_cit { get; set; }
    public DateTime cal_cit { get; set; }
    public int con_cit { get; set; }
    public long ide_mas { get; set; }
    
    [DisplayName("Mascota")]
    public string mascota { get; set; }
    
    [DisplayName("Especie")]
    public string especie { get; set; }
    
    [DisplayName("Raza")]
    public string raza { get; set; }
    
    [DisplayName("Doc. Dueño")]
    public string doc_dueno { get; set; }
    
    [DisplayName("Dueño")]
    public string nombre_dueno { get; set; }
    
    [DisplayName("Monto")]
    public decimal mon_pag { get; set; }
    
    [DisplayName("Método Pago")]
    public string metodo_pago { get; set; }
    
    // Historial Médico
    public string? sintomas { get; set; }
    public string? diagnostico { get; set; }
    public string? tratamiento { get; set; }
    public string? medicamentos { get; set; }
    public string? observaciones { get; set; }
    public DateTime? fecha_atencion { get; set; }
    
    // Propiedades calculadas
    public bool TieneHistorial => !string.IsNullOrEmpty(diagnostico);
    
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
