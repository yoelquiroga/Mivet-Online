using System.ComponentModel;

namespace VeterinariaWebApp.Models.Pago;

public class Pago
{
    [DisplayName("Nro. Pago")]
    public long IdPago { get; set; }

    [DisplayName("Fecha")]
    public DateTime HoraPago { get; set; }

    [DisplayName("Monto")]
    public decimal MontoPago { get; set; }

    [DisplayName("Tipo Pago")]
    public string? TipoPago { get; set; }

    [DisplayName("Cliente")]
    public string? NombreCliente { get; set; }

    [DisplayName("E-mail")]
    public string? CorreoCliente { get; set; }

    [DisplayName("Estado")]
    public string EstadoPago { get; set; } = "Pendiente";
    public bool? AutPag { get; set; }
}