namespace VeterinariaAPI.Models.Cita;

public class HistorialMedico
{
    public long ide_his { get; set; }
    public long ide_cit { get; set; }
    public string? sintomas { get; set; }
    public string diagnostico { get; set; } = string.Empty;
    public string tratamiento { get; set; } = string.Empty;
    public string? medicamentos { get; set; }
    public string? observaciones { get; set; }
    public DateTime fecha_atencion { get; set; }
}

// DTO para agregar historial
public class HistorialMedicoDTO
{
    public long IdCita { get; set; }
    public string? Sintomas { get; set; }
    public string Diagnostico { get; set; } = string.Empty;
    public string Tratamiento { get; set; } = string.Empty;
    public string? Medicamentos { get; set; }
    public string? Observaciones { get; set; }
}
