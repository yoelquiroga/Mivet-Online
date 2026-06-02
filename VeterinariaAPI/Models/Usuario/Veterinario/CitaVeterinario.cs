namespace VeterinariaAPI.Models.Usuario.Veterinario;

public class CitaVeterinario
{
    public long ide_cit { get; set; }
    public DateTime cal_cit { get; set; }
    public int con_cit { get; set; }
    public string mascota { get; set; }
    public string especie { get; set; }
    public string doc_dueno { get; set; }      // Documento del dueño
    public string nombre_dueno { get; set; }   // Nombre completo del dueño
    public decimal mon_pag { get; set; }
    public string nom_pay { get; set; }        // Método de pago
    public string est_cit { get; set; } = "P"; // Estado de la cita

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