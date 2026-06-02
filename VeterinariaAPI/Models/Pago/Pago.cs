namespace VeterinariaAPI.Models.Pago;

public class Pago
{
    public long IdPago { get; set; }
    public DateTime HoraPago { get; set; }
    public decimal MontoPago { get; set; }
    public string? TipoPago { get; set; }
    public string? NombreCliente { get; set; } 
    public string? CorreoCliente { get; set; }

    //Estado del pago (Pendiente o Realizado)
    public string EstadoPago { get; set; } = "Pendiente";
    public bool? AutPag { get; set; }
}
