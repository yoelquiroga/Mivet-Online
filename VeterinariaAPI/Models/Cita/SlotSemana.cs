namespace VeterinariaAPI.Models.Cita;

public class SlotSemana
{
    public DateTime FechaHora { get; set; }
    public long IdVeterinario { get; set; }
    public string Estado { get; set; }
    public string NombreVeterinario { get; set; }
    public int Consultorio { get; set; }
    public long? IdHold { get; set; }
    public string? EstadoCitaDB { get; set; }
    public long? IdCliente { get; set; }
}
