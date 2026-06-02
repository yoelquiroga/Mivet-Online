namespace VeterinariaAPI.Models.Cita;

public class HoldRequest
{
    public long ClienteId { get; set; }
    public long VeterinarioId { get; set; }
    public DateTime FechaHora { get; set; }
}

public class LiberarHoldRequest
{
    public long IdHold { get; set; }
}

public class RenovarHoldRequest
{
    public long IdHold { get; set; }
}
