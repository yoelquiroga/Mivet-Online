using System.ComponentModel;

namespace VeterinariaWebApp.Models.Usuario.Cliente;

public class Cliente : Usuario
{
    [DisplayName("ID")]
    public long IdCliente { get; set; } 
}