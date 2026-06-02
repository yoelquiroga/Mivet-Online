using System.ComponentModel;

namespace VeterinariaWebApp.Models.Usuario.Recepcionista;

public class Recepcionista : Usuario
{
    [DisplayName("ID")]
    public long IdRecepcionista { get; set; }

    [DisplayName("Sueldo")]
    public decimal Sueldo { get; set; }
}