using Microsoft.AspNetCore.Mvc;
using VeterinariaAPI.Models.Contacto;
using VeterinariaAPI.Repository.DAO;

namespace VeterinariaAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ContactoController : ControllerBase
{
    private readonly ILogger<ContactoController> _logger;

    public ContactoController(ILogger<ContactoController> logger)
    {
        _logger = logger;
    }

    [HttpPost("InsertarMensaje")]
    public ActionResult<object> InsertarMensaje([FromBody] ContactoMensajeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.nom_con) ||
            string.IsNullOrWhiteSpace(request.ape_con) ||
            string.IsNullOrWhiteSpace(request.email_con) ||
            string.IsNullOrWhiteSpace(request.servicio_con) ||
            string.IsNullOrWhiteSpace(request.mensaje_con))
        {
            return BadRequest(new { success = false, message = "Todos los campos obligatorios deben ser completados." });
        }

        try
        {
            var dao = new ContactoDAO();
            var id = dao.InsertarMensaje(
                request.nom_con.Trim(),
                request.ape_con.Trim(),
                request.email_con.Trim(),
                request.telefono_con?.Trim(),
                request.servicio_con.Trim(),
                request.mensaje_con.Trim()
            );

            return Ok(new { success = true, id_con = id, message = "Mensaje enviado correctamente." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al insertar mensaje de contacto");
            return StatusCode(500, new { success = false, message = "Error interno del servidor." });
        }
    }

    [HttpGet("ListarMensajes")]
    public ActionResult<object> ListarMensajes()
    {
        try
        {
            var dao = new ContactoDAO();
            var (mensajes, contadores) = dao.ListarConContadores();
            return Ok(new { mensajes, contadores });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar mensajes de contacto");
            return StatusCode(500, new { success = false, message = "Error interno del servidor." });
        }
    }

    [HttpGet("ObtenerMensaje/{id}")]
    public ActionResult<object> ObtenerMensaje(int id)
    {
        try
        {
            var dao = new ContactoDAO();
            var mensaje = dao.ObtenerMensaje(id);

            if (mensaje == null)
                return NotFound(new { success = false, message = "Mensaje no encontrado." });

            return Ok(new { success = true, mensaje });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener mensaje de contacto {Id}", id);
            return StatusCode(500, new { success = false, message = "Error interno del servidor." });
        }
    }

    [HttpPut("ResponderMensaje/{id}")]
    public ActionResult<object> ResponderMensaje(int id, [FromBody] ContactoRespuestaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.respuesta_con))
            return BadRequest(new { success = false, message = "La respuesta no puede estar vacía." });

        try
        {
            var dao = new ContactoDAO();

            var mensaje = dao.ObtenerMensaje(id);
            if (mensaje == null)
                return NotFound(new { success = false, message = "Mensaje no encontrado." });

            var guardado = dao.ActualizarRespuesta(id, request.respuesta_con.Trim());
            if (!guardado)
                return StatusCode(500, new { success = false, message = "Error al guardar la respuesta." });

            var emailExitoso = dao.EnviarEmailRespuesta(
                mensaje.email_con!,
                $"{mensaje.nom_con} {mensaje.ape_con}",
                request.respuesta_con.Trim(),
                mensaje.mensaje_con ?? "",
                mensaje.servicio_con ?? "",
                mensaje.fecha_con
            );

            if (emailExitoso)
            {
                dao.MarcarEmailEnviado(id);
                return Ok(new { success = true, message = "Respuesta enviada correctamente.", email_enviado = true });
            }
            else
            {
                _logger.LogWarning("El email para el mensaje {Id} no pudo ser enviado. Estado = Pendiente Email", id);
                return StatusCode(206, new { success = true, message = "Respuesta guardada pero el email no pudo ser enviado. Puede reintentarlo.", email_enviado = false });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al responder mensaje de contacto {Id}", id);
            return StatusCode(500, new { success = false, message = "Error interno del servidor." });
        }
    }

    [HttpPut("ReintentarEmail/{id}")]
    public ActionResult<object> ReintentarEmail(int id)
    {
        try
        {
            var dao = new ContactoDAO();

            var mensaje = dao.ObtenerMensaje(id);
            if (mensaje == null)
                return NotFound(new { success = false, message = "Mensaje no encontrado." });

            if (string.IsNullOrEmpty(mensaje.respuesta_con))
                return BadRequest(new { success = false, message = "El mensaje no tiene una respuesta guardada aún." });

            var emailExitoso = dao.EnviarEmailRespuesta(
                mensaje.email_con!,
                $"{mensaje.nom_con} {mensaje.ape_con}",
                mensaje.respuesta_con,
                mensaje.mensaje_con ?? "",
                mensaje.servicio_con ?? "",
                mensaje.fecha_con
            );

            if (emailExitoso)
            {
                dao.MarcarEmailEnviado(id);
                return Ok(new { success = true, message = "Email enviado correctamente.", email_enviado = true });
            }
            else
            {
                _logger.LogWarning("Reintento de email falló para el mensaje {Id}", id);
                return StatusCode(206, new { success = true, message = "El email no pudo ser enviado. Intente más tarde.", email_enviado = false });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reintentar email para mensaje {Id}", id);
            return StatusCode(500, new { success = false, message = "Error interno del servidor." });
        }
    }
}

public class ContactoMensajeRequest
{
    public string nom_con { get; set; } = "";
    public string ape_con { get; set; } = "";
    public string email_con { get; set; } = "";
    public string? telefono_con { get; set; }
    public string servicio_con { get; set; } = "";
    public string mensaje_con { get; set; } = "";
}

public class ContactoRespuestaRequest
{
    public string respuesta_con { get; set; } = "";
}
