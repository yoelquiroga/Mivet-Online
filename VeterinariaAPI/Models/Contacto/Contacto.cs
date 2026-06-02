namespace VeterinariaAPI.Models.Contacto;

public class Contacto
{
    public int id_con { get; set; }
    public string? nom_con { get; set; }
    public string? ape_con { get; set; }
    public string? email_con { get; set; }
    public string? telefono_con { get; set; }
    public string? servicio_con { get; set; }
    public string? mensaje_con { get; set; }
    public DateTime fecha_con { get; set; }
    public string estado_con { get; set; } = "Nuevo";
    public string? respuesta_con { get; set; }
    public DateTime? fecha_respuesta_con { get; set; }
    public bool email_enviado_con { get; set; }
}

public class ContactoContadores
{
    public int Total { get; set; }
    public int Nuevos { get; set; }
    public int Leidos { get; set; }
    public int Respondidos { get; set; }
    public int PendienteEmail { get; set; }
}
