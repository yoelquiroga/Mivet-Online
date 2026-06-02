namespace VeterinariaAPI.Models.Usuario.Veterinario;

/// <summary>
/// Modelo para mascotas atendidas por el veterinario con historial médico
/// </summary>
public class MascotaAtendida
{
    public long ide_cit { get; set; }
    public DateTime cal_cit { get; set; }
    public int con_cit { get; set; }
    public long ide_mas { get; set; }
    public string mascota { get; set; }
    public string especie { get; set; }
    public string raza { get; set; }
    public string doc_dueno { get; set; }
    public string nombre_dueno { get; set; }
    public decimal mon_pag { get; set; }
    public string metodo_pago { get; set; }
    
    // Historial Médico
    public string? sintomas { get; set; }
    public string? diagnostico { get; set; }
    public string? tratamiento { get; set; }
    public string? medicamentos { get; set; }
    public string? observaciones { get; set; }
    public DateTime? fecha_atencion { get; set; }
}
