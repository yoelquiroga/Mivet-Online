namespace VeterinariaAPI.Models.Usuario.Cliente;

public class CitaCliente
{
    public long ide_cit { get; set; }
    public DateTime cal_cit { get; set; }
    public int con_cit { get; set; }
    public string veterinario { get; set; }
    public string especialidad { get; set; }
    public string mascota { get; set; }
    public string especie { get; set; }
    public string raza { get; set; }
    public decimal mon_pag { get; set; }
    public string metodo_pago { get; set; }
    public string est_cit { get; set; } = "P"; // P=Pendiente, E=EnAtención, A=Atendida, C=Cancelada

    // Historial Médico (solo cuando está atendida)
    public string? sintomas { get; set; }
    public string? diagnostico { get; set; }
    public string? tratamiento { get; set; }
    public string? medicamentos { get; set; }
    public string? observaciones { get; set; }
    public DateTime? fecha_atencion { get; set; }

    // Propiedad calculada para mostrar el estado en texto
    public string EstadoDescripcion => est_cit switch
    {
        "P" => "Pendiente",
        "E" => "En Atención",
        "A" => "Atendida",
        "C" => "Cancelada",
        _ => "Desconocido"
    };
}