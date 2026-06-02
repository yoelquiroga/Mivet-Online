using System.Net;
using System.Net.Mail;
using VeterinariaAPI.Models.Cita;
using VeterinariaAPI.Repository.DAO;
using Microsoft.AspNetCore.Mvc;

namespace VeterinariaAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CitaController : ControllerBase
{


    //listaCitasPendientes

    [HttpGet("listaCitasPendientes")]
    public async Task<ActionResult<List<Cita>>> ListaCitasPendientes()
    {
        var lista = await Task.Run(() => new CitaDAO().ListarCitasPendientes());
        return Ok(lista);
    }

    [HttpGet("buscarCitaConDetalle/{id}")]
    public async Task<ActionResult<object>> BuscarCitaConDetalle(long id)
    {
        var cita = await Task.Run(() => new CitaDAO().BuscarCitaConDetalle(id));
        if (cita == null)
            return NotFound(new { mensaje = "No se encontró la cita." });
        return Ok(cita);
    }

    [HttpPut("marcarInasistencia/{id}")]
    public async Task<ActionResult<object>> MarcarInasistencia(long id)
    {
        var (mensaje, email, nombre, mascota, fecha) = await Task.Run(() => new CitaDAO().MarcarInasistencia(id));
        if (mensaje != "ok")
            return BadRequest(new { mensaje });
        return Ok(new { email, nombreCliente = nombre, nombreMascota = mascota, fechaCita = fecha });
    }

    [HttpPost("enviarEmailInasistencia")]
    public async Task<ActionResult> EnviarEmailInasistencia([FromBody] EmailInasistenciaRequest request)
    {
        var result = await Task.Run(() =>
        {
            try
            {
                var config = DbConfig.Configuration.GetSection("EmailConfig");
                var host = config["SmtpServer"] ?? "smtp.gmail.com";
                var port = int.Parse(config["SmtpPort"] ?? "587");
                var ssl = bool.Parse(config["EnableSsl"] ?? "true");
                var fromEmail = config["SenderEmail"] ?? "";
                var pass = config["SenderPassword"] ?? "";

                if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(pass))
                    return (success: false, error: "SMTP no configurado");

                var mail = ConstruirMailInasistencia(
                    fromEmail, "MiVet Online",
                    request.Email, request.NombreCliente,
                    request.NombreMascota, request.FechaCita);

                using var smtp = new SmtpClient(host, port);
                smtp.EnableSsl = ssl;
                smtp.Credentials = new NetworkCredential(fromEmail, pass);
                smtp.Send(mail);

                return (success: true, error: "OK");
            }
            catch (Exception ex)
            {
                return (success: false, error: ex.Message);
            }
        });

        if (result.success)
            return Ok(new { message = "Correo de inasistencia enviado" });

        return StatusCode(500, new { message = result.error });
    }

    [HttpPut("reagendar/{idCitaCancelada}")]
    public async Task<ActionResult<object>> Reagendar(long idCitaCancelada, [FromBody] CitaO nuevaCita)
    {
        var resultado = await Task.Run(() => new CitaDAO().ReagendarCita(
            idCitaCancelada, nuevaCita.CalendarioCita, (int)nuevaCita.Consultorio, nuevaCita.IdVeterinario));
        if (resultado.StartsWith("ok:"))
        {
            var nuevaId = long.Parse(resultado.Split(':')[1]);
            return Ok(new { success = true, nuevaIdCita = nuevaId, mensaje = "Cita reagendada correctamente." });
        }
        return BadRequest(new { success = false, mensaje = resultado });
    }

    //listaCitasAtendidas

    [HttpGet("listaCitasAtendidas")]
    public async Task<ActionResult<List<Cita>>> ListaCitasAtendidas()
    {
        var lista = await Task.Run(() => new CitaDAO().ListarCitasAtendidas());
        return Ok(lista);
    }
    //listaCitasCanceladas

    [HttpGet("listaCitasCanceladas")]
    public async Task<ActionResult<List<Cita>>> ListaCitasCanceladas()
    {
        var lista = await Task.Run(() => new CitaDAO().ListarCitasCanceladas());
        return Ok(lista);
    }


    //Listar Citas por estado 

    [HttpGet("listaCitasPorEstado")]
    public async Task<ActionResult<List<Cita>>> ListaCitasPorEstado(string estado)
    {
        var lista = await Task.Run(() => new CitaDAO().ListarCitasPorEstado(estado));
        return Ok(lista);
    }






    [HttpPost("agregaCita")]
    public async Task<ActionResult<string>> AgregaCita(CitaO obj, [FromQuery] int clienteId)
    {
        var mensaje = await Task.Run(() => new CitaDAO().AgregarCita(obj, clienteId));

        if (mensaje != "Cita registrada correctamente")
        {
            return Conflict(mensaje);
        }

        return Ok(mensaje);
    }


    [HttpPut("actualizaCita")]
    public async Task<ActionResult<string>> ActualizaCita(CitaO obj, [FromQuery] int clienteId)
    {
        var mensaje = await Task.Run(() => new CitaDAO().ModificarCita(obj, clienteId));

        if (mensaje != "Cita actualizada correctamente")
        {
            return Conflict(mensaje);
        }

        return Ok(mensaje);
    }





    [HttpDelete("eliminarCita/{id}")]
    public async Task<ActionResult> EliminarCita(long id)
    {
        await Task.Run(() => new CitaDAO().EliminarCita(id));
        return Ok();
    }

    [HttpGet("buscarCita/{id}")]
    public async Task<ActionResult<CitaO>> BuscarCita(long id)
    {
        var cita = await Task.Run(() => new CitaDAO().BuscarCita(id));
        return Ok(cita);
    }

    [HttpGet("buscarCitaFront/{id}")]
    public async Task<ActionResult<Cita>> BuscarCitaFront(long id)
    {
        var cita = await Task.Run(() => new CitaDAO().BuscarCitaFront(id));
        return Ok(cita);
    }


    // Endpoint para actualizar el estado de una cita
    [HttpPut("actualizarEstado/{id}")]
    public async Task<ActionResult<string>> ActualizarEstadoCita(long id, [FromQuery] string estado)
    {
        // Validar que el estado sea válido
        var estadosValidos = new[] { "P", "E", "A", "C" };
        if (!estadosValidos.Contains(estado.ToUpper()))
        {
            return BadRequest("Estado no válido. Use: P (Pendiente), E (En Atención), A (Atendida), C (Cancelada)");
        }

        // Leer header opcional X-Cliente-Id para validar ownership
        long? clienteId = null;
        if (Request.Headers.TryGetValue("X-Cliente-Id", out var headerValue))
        {
            if (long.TryParse(headerValue, out var idCliente))
                clienteId = idCliente;
        }

        var mensaje = await Task.Run(() => new CitaDAO().ActualizarEstadoCita(id, estado.ToUpper(), clienteId));

        if (mensaje == "No tienes permiso para modificar esta cita.")
            return StatusCode(403, mensaje);

        return Ok(new { success = true, message = mensaje });
    }

    // ==================== HOLD TEMPORAL ====================

    [HttpPost("crearHold")]
    public async Task<ActionResult<object>> CrearHold([FromBody] HoldRequest request)
    {
        var dao = new CitaDAO();
        var holdId = await Task.Run(() => dao.CrearHold(request.ClienteId, request.VeterinarioId, request.FechaHora));
        return Ok(new { success = true, ide_hol = holdId });
    }

    [HttpPost("intentarHold")]
    public async Task<ActionResult<object>> IntentarHold([FromBody] HoldRequest request)
    {
        var dao = new CitaDAO();
        var result = await Task.Run(() => dao.IntentarHold(request.ClienteId, request.VeterinarioId, request.FechaHora));
        if (result == -1)
            return Conflict(new { success = false, codigo = "OCUPADO", mensaje = "El horario ya está ocupado por una cita." });
        if (result == -2)
            return Conflict(new { success = false, codigo = "HOLD_OTRO", mensaje = "Este horario está siendo reservado por otro usuario." });
        if (result == -3)
            return StatusCode(500, new { success = false, codigo = "ERROR", mensaje = "Error al intentar reservar el horario." });
        return Ok(new { success = true, ide_hol = result });
    }

    [HttpPost("liberarHold")]
    public async Task<ActionResult> LiberarHold([FromBody] LiberarHoldRequest request)
    {
        await Task.Run(() => new CitaDAO().LiberarHold(request.IdHold));
        return Ok(new { success = true });
    }

    [HttpPost("renovarHold")]
    public async Task<ActionResult> RenovarHold([FromBody] RenovarHoldRequest request)
    {
        await Task.Run(() => new CitaDAO().RenovarHold(request.IdHold));
        return Ok(new { success = true });
    }

    [HttpGet("semana")]
    public async Task<ActionResult<List<SlotSemana>>> ObtenerSemana(DateTime fecha, long? ide_vet, long ide_cli)
    {
        var dao = new CitaDAO();
        await Task.Run(() => dao.LimpiarHoldsExpirados());
        var slots = await Task.Run(() => dao.ObtenerSlotsSemana(fecha, ide_vet, ide_cli));
        return Ok(slots);
    }

    // ==================== HISTORIAL MÉDICO ====================

    // Agregar historial médico (llamado al finalizar atención)
    [HttpPost("agregarHistorial")]
    public async Task<ActionResult<string>> AgregarHistorialMedico([FromBody] HistorialMedicoDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Diagnostico) || string.IsNullOrWhiteSpace(dto.Tratamiento))
        {
            return BadRequest("Diagnóstico y tratamiento son obligatorios");
        }

        var mensaje = await Task.Run(() => new CitaDAO().AgregarHistorialMedico(dto));
        return Ok(new { success = true, message = mensaje });
    }

    // Obtener historial médico de una cita
    [HttpGet("historial/{idCita}")]
    public async Task<ActionResult<HistorialMedico>> ObtenerHistorialPorCita(long idCita)
    {
        var historial = await Task.Run(() => new CitaDAO().ObtenerHistorialPorCita(idCita));
        if (historial == null)
        {
            return Ok(new { existe = false, message = "No hay historial médico para esta cita" });
        }
        return Ok(historial);
    }

    private static MailMessage ConstruirMailInasistencia(
        string fromEmail, string fromName,
        string toEmail, string nombreCliente,
        string nombreMascota, string fechaCita)
    {
        var safeNombre = System.Net.WebUtility.HtmlEncode(nombreCliente ?? "");
        var safeMascota = System.Net.WebUtility.HtmlEncode(nombreMascota ?? "");
        var safeFecha = System.Net.WebUtility.HtmlEncode(fechaCita ?? "");

        var asunto = "Tu cita ha sido cancelada por inasistencia - MiVet Online";

        var htmlBody = $@"
<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; background-color: #111827; font-family: 'Segoe UI', Arial, sans-serif;"">
    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""background-color: #111827;"">
        <tr>
            <td align=""center"" style=""padding: 30px 10px;"">
                <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""600"" style=""max-width: 600px; width: 100%; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 16px rgba(0,0,0,0.3);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #dc2626, #ef4444); padding: 28px 30px; text-align: center;"">
                            <img src=""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'%3E%3Ccircle cx='50' cy='65' r='20' fill='%23FFFFFF'/%3E%3Ccircle cx='25' cy='35' r='12' fill='%23FFFFFF'/%3E%3Ccircle cx='50' cy='20' r='12' fill='%23FFFFFF'/%3E%3Ccircle cx='75' cy='35' r='12' fill='%23FFFFFF'/%3E%3C/svg%3E"" alt="""" width=""40"" height=""40"" style=""display: inline-block; vertical-align: middle;"" />
                            <h1 style=""margin: 8px 0 0 0; font-size: 26px; font-weight: bold; color: #ffffff; letter-spacing: 1px;"">MiVet Online</h1>
                            <p style=""margin: 4px 0 0 0; font-size: 13px; color: #fecaca;"">Cancelaci&oacute;n de cita por inasistencia</p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 30px; background-color: #ffffff;"">
                            <p style=""margin: 0 0 16px 0; font-size: 16px; color: #1f2937; line-height: 1.6;"">Hola <strong style=""color: #111827;"">{safeNombre}</strong>,</p>

                            <p style=""margin: 0 0 20px 0; font-size: 15px; color: #374151; line-height: 1.6;"">
                                Lamentamos informarte que la cita de <strong>{safeMascota}</strong> programada para el <strong>{safeFecha}</strong> ha sido cancelada por inasistencia.
                            </p>

                            <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""background-color: #fef2f2; border-left: 4px solid #ef4444; border-radius: 0 6px 6px 0; margin: 0 0 20px 0;"">
                                <tr>
                                    <td style=""padding: 16px 20px;"">
                                        <p style=""margin: 0; font-size: 14px; color: #991b1b; line-height: 1.6;"">
                                            <strong>Nota importante:</strong> El cobro de esta cita corresponde al horario reservado y no contempla reembolso por inasistencia.
                                        </p>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 0 0 20px 0; font-size: 15px; color: #374151; line-height: 1.6;"">
                                Si deseas coordinar una nueva atenci&oacute;n, cont&aacute;ctanos hoy antes de las <strong>4:30 PM</strong> al <strong>WhatsApp 963258852</strong>. Recepci&oacute;n verificar&aacute; disponibilidad de slots a partir de esa hora.
                            </p>

                            <p style=""margin: 0; font-size: 15px; color: #374151; line-height: 1.6;"">
                                Agradecemos tu comprensi&oacute;n.
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f9fafb; border-top: 1px solid #e5e7eb; padding: 24px 30px; text-align: center;"">
                            <p style=""margin: 0 0 10px 0; font-size: 12px; color: #6b7280; line-height: 1.8;"">
                                📍 Ej&eacute;rcito 391, Miraflores, Lima, Per&uacute;<br>
                                📞 +51 963 258 852<br>
                                🕐 Lun-Vie: 9:00 AM - 7:00 PM &nbsp;|&nbsp; S&aacute;b: 9:00 AM - 2:00 PM
                            </p>
                            <p style=""margin: 0 0 8px 0; font-size: 13px; color: #374151; font-weight: bold;"">Equipo MiVet Online</p>
                            <p style=""margin: 0; font-size: 11px; color: #9ca3af; line-height: 1.5;"">
                                www.mivetonline.com &nbsp;|&nbsp; Este correo es generado autom&aacute;ticamente.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        var plainText = $@"Hola {safeNombre},

Lamentamos informarte que la cita de {safeMascota} programada para el {safeFecha} ha sido cancelada por inasistencia.

Nota importante: El cobro de esta cita corresponde al horario reservado y no contempla reembolso por inasistencia.

Si deseas coordinar una nueva atención, contáctanos hoy antes de las 4:30 PM al WhatsApp 963258852. Recepción verificará disponibilidad de slots a partir de esa hora.

Agradecemos tu comprensión.

---
Equipo MiVet Online
- Ejército 391, Miraflores, Lima, Perú
- +51 963 258 852
- Lun-Vie: 9:00 AM - 7:00 PM | Sáb: 9:00 AM - 2:00 PM
- www.mivetonline.com";

        var mail = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = asunto,
            IsBodyHtml = false,
            Priority = MailPriority.Normal
        };

        mail.Headers.Add("X-Mailer", "MiVet Online System");
        mail.Headers.Add("Precedence", "list");
        mail.To.Add(toEmail);
        mail.ReplyToList.Add(new MailAddress("contactomivetonline@gmail.com", "Soporte MiVet"));

        mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plainText, null, "text/plain"));
        mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html"));

        return mail;
    }
}