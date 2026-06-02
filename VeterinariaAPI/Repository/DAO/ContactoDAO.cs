using VeterinariaAPI.Models.Contacto;
using VeterinariaAPI.Repository.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Net;
using System.Net.Mail;

namespace VeterinariaAPI.Repository.DAO;

public class ContactoDAO : IContacto
{
    private static readonly string _connectionString = DbConfig.Configuration.GetConnectionString("cn") 
        ?? throw new NullReferenceException("Cadena de conexión no encontrada.");
    private static readonly string _smtpServer = DbConfig.Configuration.GetSection("EmailConfig")["SmtpServer"] ?? "smtp.gmail.com";
    private static readonly int _smtpPort = int.Parse(DbConfig.Configuration.GetSection("EmailConfig")["SmtpPort"] ?? "587");
    private static readonly string _senderEmail = DbConfig.Configuration.GetSection("EmailConfig")["SenderEmail"] ?? "";
    private static readonly string _senderPassword = DbConfig.Configuration.GetSection("EmailConfig")["SenderPassword"] ?? "";
    private static readonly bool _enableSsl = bool.Parse(DbConfig.Configuration.GetSection("EmailConfig")["EnableSsl"] ?? "true");

    public int InsertarMensaje(string nom_con, string ape_con, string email_con, string? telefono_con, string servicio_con, string mensaje_con)
    {
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("SP_Contacto_Insertar", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@nom_con", nom_con);
        cmd.Parameters.AddWithValue("@ape_con", ape_con);
        cmd.Parameters.AddWithValue("@email_con", email_con);
        cmd.Parameters.AddWithValue("@telefono_con", (object?)telefono_con ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@servicio_con", servicio_con);
        cmd.Parameters.AddWithValue("@mensaje_con", mensaje_con);

        cn.Open();
        var result = cmd.ExecuteScalar();
        return Convert.ToInt32(result);
    }

    public (List<Contacto> Mensajes, ContactoContadores Contadores) ListarConContadores()
    {
        var mensajes = new List<Contacto>();
        var contadores = new ContactoContadores();

        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("SP_Contacto_SeleccionarTodos", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        cn.Open();
        using var dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            mensajes.Add(new Contacto
            {
                id_con = Convert.ToInt32(dr["id_con"]),
                nom_con = dr["nom_con"].ToString(),
                ape_con = dr["ape_con"].ToString(),
                email_con = dr["email_con"].ToString(),
                telefono_con = dr["telefono_con"] == DBNull.Value ? null : dr["telefono_con"].ToString(),
                servicio_con = dr["servicio_con"].ToString(),
                mensaje_con = dr["mensaje_con"].ToString(),
                fecha_con = Convert.ToDateTime(dr["fecha_con"]),
                estado_con = dr["estado_con"].ToString() ?? "Nuevo",
                respuesta_con = dr["respuesta_con"] == DBNull.Value ? null : dr["respuesta_con"].ToString(),
                fecha_respuesta_con = dr["fecha_respuesta_con"] == DBNull.Value ? null : Convert.ToDateTime(dr["fecha_respuesta_con"]),
                email_enviado_con = dr["email_enviado_con"] != DBNull.Value && Convert.ToBoolean(dr["email_enviado_con"])
            });
        }

        if (dr.NextResult() && dr.Read())
        {
            contadores.Total = Convert.ToInt32(dr["Total"]);
            contadores.Nuevos = Convert.ToInt32(dr["Nuevos"]);
            contadores.Leidos = Convert.ToInt32(dr["Leidos"]);
            contadores.Respondidos = Convert.ToInt32(dr["Respondidos"]);
            contadores.PendienteEmail = Convert.ToInt32(dr["PendienteEmail"]);
        }

        return (mensajes, contadores);
    }

    public Contacto? ObtenerMensaje(int id_con)
    {
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("SP_Contacto_SeleccionarPorId", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@id_con", id_con);

        cn.Open();
        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            return new Contacto
            {
                id_con = Convert.ToInt32(dr["id_con"]),
                nom_con = dr["nom_con"].ToString(),
                ape_con = dr["ape_con"].ToString(),
                email_con = dr["email_con"].ToString(),
                telefono_con = dr["telefono_con"] == DBNull.Value ? null : dr["telefono_con"].ToString(),
                servicio_con = dr["servicio_con"].ToString(),
                mensaje_con = dr["mensaje_con"].ToString(),
                fecha_con = Convert.ToDateTime(dr["fecha_con"]),
                estado_con = dr["estado_con"].ToString() ?? "Nuevo",
                respuesta_con = dr["respuesta_con"] == DBNull.Value ? null : dr["respuesta_con"].ToString(),
                fecha_respuesta_con = dr["fecha_respuesta_con"] == DBNull.Value ? null : Convert.ToDateTime(dr["fecha_respuesta_con"]),
                email_enviado_con = dr["email_enviado_con"] != DBNull.Value && Convert.ToBoolean(dr["email_enviado_con"])
            };
        }
        return null;
    }

    public bool ActualizarRespuesta(int id_con, string respuesta_con)
    {
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("SP_Contacto_ActualizarRespuesta", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@id_con", id_con);
        cmd.Parameters.AddWithValue("@respuesta_con", respuesta_con);

        cn.Open();
        var result = cmd.ExecuteScalar();
        return result != null && Convert.ToInt32(result) == 1;
    }

    public void MarcarEmailEnviado(int id_con)
    {
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("SP_Contacto_MarcarEmailEnviado", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@id_con", id_con);

        cn.Open();
        cmd.ExecuteNonQuery();
    }

    public bool EnviarEmailRespuesta(string destinatario, string nombre, string respuesta, string mensajeOriginal, string servicio, DateTime? fecha)
    {
        try
        {
            var safeNombre = System.Net.WebUtility.HtmlEncode(nombre ?? "");
            var safeRespuesta = System.Net.WebUtility.HtmlEncode(respuesta ?? "");
            var safeMensaje = System.Net.WebUtility.HtmlEncode(mensajeOriginal ?? "");
            var safeServicio = System.Net.WebUtility.HtmlEncode(servicio ?? "");

            using var smtp = new SmtpClient(_smtpServer, _smtpPort);
            smtp.Credentials = new NetworkCredential(_senderEmail, _senderPassword);
            smtp.EnableSsl = _enableSsl;

            var asunto = "Respuesta a tu consulta - MiVet Online";
            var fechaStr = fecha?.ToString("dd/MM/yyyy HH:mm") ?? "—";

            var cuerpoHtml = $@"
<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; background-color: #f4f6f8; font-family: 'Segoe UI', Arial, sans-serif;"">
    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""background-color: #f4f6f8;"">
        <tr>
            <td align=""center"" style=""padding: 20px 10px;"">
                <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""600"" style=""max-width: 600px; width: 100%; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(90deg, #94827F, #D7C8C1); padding: 25px 30px; text-align: center;"">
                            <img src=""data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'%3E%3Ccircle cx='50' cy='65' r='20' fill='%23FFFFFF'/%3E%3Ccircle cx='25' cy='35' r='12' fill='%23FFFFFF'/%3E%3Ccircle cx='50' cy='20' r='12' fill='%23FFFFFF'/%3E%3Ccircle cx='75' cy='35' r='12' fill='%23FFFFFF'/%3E%3C/svg%3E"" alt=""🐾"" width=""36"" height=""36"" style=""display: inline-block; vertical-align: middle;"" />
                            <h1 style=""margin: 0; font-size: 24px; font-weight: bold; color: #ffffff; letter-spacing: 1px;"">MiVet Online</h1>
                            <p style=""margin: 5px 0 0 0; font-size: 13px; color: #f0e8e4;"">Sistema de Gesti&oacute;n Veterinaria</p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 30px; background-color: #ffffff;"">
                            <p style=""margin: 0 0 15px 0; font-size: 15px; color: #575252; line-height: 1.6;"">Hola <strong>{safeNombre}</strong>,</p>
                            <p style=""margin: 0 0 20px 0; font-size: 15px; color: #575252; line-height: 1.6;"">Gracias por contactarnos. A continuaci&oacute;n te compartimos la respuesta a tu consulta:</p>

                            <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""background-color: #F4F6F8; border-left: 3px solid #94827F; border-radius: 0 8px 8px 0; margin: 0 0 20px 0;"">
                                <tr>
                                    <td style=""padding: 15px;"">
                                        <p style=""margin: 0 0 6px 0; font-size: 13px; font-weight: bold; color: #575252;"">📋 Tu consulta:</p>
                                        <p style=""margin: 0 0 8px 0; font-size: 13px; color: #666666; font-style: italic; line-height: 1.5;"">&ldquo;{safeMensaje}&rdquo;</p>
                                        <p style=""margin: 0; font-size: 12px; color: #888888;"">Servicio: <strong style=""color: #575252;"">{safeServicio}</strong> &nbsp;|&nbsp; Fecha: {fechaStr}</p>
                                    </td>
                                </tr>
                            </table>

                            <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""background-color: #FFF9E6; border-left: 3px solid #EEB447; border-radius: 0 8px 8px 0; margin: 0 0 20px 0;"">
                                <tr>
                                    <td style=""padding: 15px;"">
                                        <p style=""margin: 0 0 6px 0; font-size: 13px; font-weight: bold; color: #575252;"">💬 Nuestra respuesta:</p>
                                        <p style=""margin: 0; font-size: 14px; color: #333333; font-weight: bold; line-height: 1.6;"">{safeRespuesta}</p>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 0 0 5px 0; font-size: 15px; color: #575252; line-height: 1.6;"">Si tienes m&aacute;s preguntas, no dudes en escribirnos nuevamente.</p>
                            <p style=""margin: 0; font-size: 15px; color: #575252; line-height: 1.6;"">Estaremos encantados de ayudarte.</p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #F4F6F8; border-top: 1px solid #e0e0e0; padding: 20px 30px; text-align: center;"">
                            <p style=""margin: 0 0 10px 0; font-size: 12px; color: #888888; line-height: 1.8;"">
                                📍 Direcci&oacute;n: Ej&eacute;rcito 391, Miraflores, Lima, Per&uacute;<br>
                                📞 Tel&eacute;fono: +51 963 258 852<br>
                                🕐 Horario de atenci&oacute;n:<br>
                                &nbsp;&nbsp;&nbsp;Lunes a Viernes: 9:00 AM - 7:00 PM<br>
                                &nbsp;&nbsp;&nbsp;S&aacute;bados: 9:00 AM - 2:00 PM
                            </p>
                            <p style=""margin: 0 0 5px 0; font-size: 13px; color: #575252; font-weight: bold;"">Saludos cordiales,</p>
                            <p style=""margin: 0 0 10px 0; font-size: 13px; color: #575252; font-weight: bold;"">Equipo MiVet Online</p>
                            <p style=""margin: 0; font-size: 11px; color: #aaaaaa; line-height: 1.5;"">
                                Este correo es generado autom&aacute;ticamente. Por favor no respondas directamente a esta direcci&oacute;n.<br>
                                Si necesitas contactarnos, usa nuestro formulario web en www.mivetonline.com
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

            var cuerpoTexto = $"Hola {safeNombre},\n\nGracias por contactarnos. A continuación te compartimos la respuesta a tu consulta:\n\nTu consulta:\"{safeMensaje}\"\nServicio: {safeServicio} | Fecha: {fechaStr}\n\nNuestra respuesta:\n{safeRespuesta}\n\nSi tienes más preguntas, no dudes en escribirnos nuevamente. Estaremos encantados de ayudarte.\n\nSaludos cordiales,\nEquipo MiVet Online\n\n📍 Dirección: Ejército 391, Miraflores, Lima, Perú\n📞 Teléfono: +51 963 258 852\n🕐 Horario: Lun-Vie 9:00-19:00 | Sáb 9:00-14:00";

            using var mail = new MailMessage
            {
                From = new MailAddress(_senderEmail, "MiVet Online"),
                Subject = asunto,
                IsBodyHtml = true,
                Priority = MailPriority.Normal
            };
            mail.Headers.Add("X-Mailer", "MiVet Online System");
            mail.To.Add(destinatario);
            mail.ReplyToList.Add(new MailAddress("contactomivetonline@gmail.com", "Soporte MiVet"));

            mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(cuerpoTexto, null, "text/plain"));
            mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(cuerpoHtml, null, "text/html"));

            smtp.Send(mail);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al enviar email: {ex.Message}");
            return false;
        }
    }
}
