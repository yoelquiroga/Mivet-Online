using VeterinariaAPI.Models.Contacto;

namespace VeterinariaAPI.Repository.Interfaces;

public interface IContacto
{
    int InsertarMensaje(string nom_con, string ape_con, string email_con, string? telefono_con, string servicio_con, string mensaje_con);
    (List<Contacto> Mensajes, ContactoContadores Contadores) ListarConContadores();
    Contacto? ObtenerMensaje(int id_con);
    bool ActualizarRespuesta(int id_con, string respuesta_con);
    void MarcarEmailEnviado(int id_con);
    bool EnviarEmailRespuesta(string destinatario, string nombre, string respuesta, string mensajeOriginal, string servicio, DateTime? fecha);
}
