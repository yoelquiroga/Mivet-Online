using System.ComponentModel;

namespace VeterinariaWebApp.Models.Usuario.Cliente;

public class ClienteO : UsuarioO
{
    [DisplayName("ID CLIENTE")]
    public long ide_cli { get; set; }

    [DisplayName("ID USUARIO")]
    public long ide_usr { get; set; }
}