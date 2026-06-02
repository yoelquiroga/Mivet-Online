using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using VeterinariaWebApp.Models.Contacto;
using Microsoft.Extensions.Configuration;

namespace VeterinariaWebApp.Controllers;

public class ContactoController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ContactoController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient GetClient() => _httpClientFactory.CreateClient("ClinicaAPI");

    // GET: Contacto/ObtenerContadores (para badge en sidebar)
    [HttpGet]
    public async Task<JsonResult> ObtenerContadores()
    {
        var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
        if (recepcionistaId == null || recepcionistaId == 0)
            return Json(new { nuevos = 0 });

        try
        {
            var client = GetClient();

            var response = await client.GetAsync("Contacto/ListarMensajes");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var obj = JObject.Parse(json);
                var contadores = obj?["contadores"];
                if (contadores != null)
                    return Json(new { nuevos = contadores["Nuevos"]?.Value<int>() ?? 0 });
            }
        }
        catch { }
        return Json(new { nuevos = 0 });
    }

    // POST: Contacto/EnviarMensaje (público, desde Home)
    [HttpPost]
    public async Task<IActionResult> EnviarMensaje(string nom_con, string ape_con, string email_con, string? telefono_con, string servicio_con, string mensaje_con)
    {
        try
        {
            var client = GetClient();

            var body = new
            {
                nom_con,
                ape_con,
                email_con,
                telefono_con,
                servicio_con,
                mensaje_con
            };

            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("Contacto/InsertarMensaje", content);

            if (response.IsSuccessStatusCode)
                return Ok(new { success = true, message = "¡Gracias por contactarnos! Te responderemos pronto." });

            var json = await response.Content.ReadAsStringAsync();
            var obj = JsonConvert.DeserializeObject<JObject>(json);
            return BadRequest(new { success = false, message = obj?["message"]?.ToString() ?? "Error al enviar el mensaje." });
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error de conexión con el servidor." });
        }
    }

    // GET: Contacto/Index (Recepcionista)
    public async Task<IActionResult> Index()
    {
        var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
        if (recepcionistaId == null || recepcionistaId == 0)
            return RedirectToAction("Index", "Login");

        try
        {
            var client = GetClient();

            var response = await client.GetAsync("Contacto/ListarMensajes");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var obj = JsonConvert.DeserializeObject<JObject>(json);
                var mensajes = obj?["mensajes"]?.ToObject<List<Contacto>>() ?? new List<Contacto>();
                var contadores = obj?["contadores"]?.ToObject<ContactoContadores>() ?? new ContactoContadores();

                ViewBag.Contadores = contadores;
                return View(mensajes);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al listar mensajes: {ex.Message}");
        }

        ViewBag.Contadores = new ContactoContadores();
        return View(new List<Contacto>());
    }

    // GET: Contacto/Detalle/5 (Recepcionista)
    public async Task<IActionResult> Detalle(int id)
    {
        var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
        if (recepcionistaId == null || recepcionistaId == 0)
            return RedirectToAction("Index", "Login");

        try
        {
            var client = GetClient();

            var response = await client.GetAsync($"Contacto/ObtenerMensaje/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var obj = JsonConvert.DeserializeObject<JObject>(json);
                var mensaje = obj?["mensaje"]?.ToObject<Contacto>();
                if (mensaje != null)
                    return View("Detalle", mensaje);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                TempData["Error"] = "Mensaje no encontrado.";
                return RedirectToAction("Index");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener mensaje: {ex.Message}");
        }

        TempData["Error"] = "Error al cargar el mensaje.";
        return RedirectToAction("Index");
    }

    // POST: Contacto/Responder (Recepcionista, via AJAX)
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Responder(int id_con, string respuesta_con)
    {
        var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
        if (recepcionistaId == null || recepcionistaId == 0)
            return Unauthorized(new { success = false, message = "Sesión expirada." });

        try
        {
            var client = GetClient();

            var body = new { respuesta_con };
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"Contacto/ResponderMensaje/{id_con}", content);
            var json = await response.Content.ReadAsStringAsync();
            var obj = JsonConvert.DeserializeObject<JObject>(json);

            var success = obj?["success"]?.Value<bool>() ?? false;
            var message = obj?["message"]?.ToString() ?? "";
            var email_enviado = obj?["email_enviado"]?.Value<bool>() ?? false;

            if (response.StatusCode == System.Net.HttpStatusCode.OK && success)
                return Ok(new { success = true, message, email_enviado });

            if ((int)response.StatusCode == 206)
                return StatusCode(206, new { success = true, message, email_enviado });

            return BadRequest(new { success = false, message = message ?? "Error al enviar respuesta." });
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error de conexión con el servidor." });
        }
    }

    // POST: Contacto/ReintentarEmail (Recepcionista, via AJAX)
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ReintentarEmail(int id_con)
    {
        var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
        if (recepcionistaId == null || recepcionistaId == 0)
            return Unauthorized(new { success = false, message = "Sesión expirada." });

        try
        {
            var client = GetClient();

            var response = await client.PutAsync($"Contacto/ReintentarEmail/{id_con}", null);
            var json = await response.Content.ReadAsStringAsync();
            var obj = JsonConvert.DeserializeObject<JObject>(json);

            var success = obj?["success"]?.Value<bool>() ?? false;
            var message = obj?["message"]?.ToString() ?? "";
            var email_enviado = obj?["email_enviado"]?.Value<bool>() ?? false;

            if (response.StatusCode == System.Net.HttpStatusCode.OK && success)
                return Ok(new { success = true, message, email_enviado });

            if ((int)response.StatusCode == 206)
                return StatusCode(206, new { success = true, message, email_enviado });

            return BadRequest(new { success = false, message = message ?? "Error al reintentar envío." });
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Error de conexión con el servidor." });
        }
    }
}
