using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Text;
using VeterinariaWebApp.Models.Usuario;
using VeterinariaWebApp.Models.Usuario.Cliente;

namespace VeterinariaWebApp.Controllers
{
    public class LoginController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LoginController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient GetClient() => _httpClientFactory.CreateClient("ClinicaAPI");

        #region Métodos Auxiliares

        private async Task<string> IniciarSesionAsync(string uid, string pwd)
        {
            try
            {
                var client = GetClient();
                var response = await client.GetAsync($"/api/Usuario/VerificarLogin?uid={Uri.EscapeDataString(uid)}&pwd={Uri.EscapeDataString(pwd)}");
                if (response.IsSuccessStatusCode)
                {
                    var contenido = await response.Content.ReadAsStringAsync();
                    return contenido.Trim('"', '\r', '\n', ' ');
                }
                return "denied";
            }
            catch
            {
                return "denied";
            }
        }

        private async Task<string> ObtenerTokenAsync(string uid)
        {
            try
            {
                var client = GetClient();
                var response = await client.GetAsync($"/api/Usuario/ObtenerIdUsuario?correo={Uri.EscapeDataString(uid)}");
                if (response.IsSuccessStatusCode)
                {
                    var contenido = await response.Content.ReadAsStringAsync();
                    return contenido.Trim('"', '\r', '\n', ' ');
                }
                return "0";
            }
            catch
            {
                return "0";
            }
        }

        private async Task<List<UserDoc>> ObtenerTiposDocumentoAsync()
        {
            try
            {
                var client = GetClient();
                var response = await client.GetAsync("/api/Usuario/ListarDocumentos");
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<UserDoc>>(data) ?? new List<UserDoc>();
                }
            }
            catch { }
            return new List<UserDoc>();
        }

        private async Task<string> RegistrarClienteAsync(RegistroClienteViewModel modelo)
        {
            try
            {
                var cliente = new ClienteO
                {
                    cor_usr = modelo.cor_usr,
                    pwd_usr = modelo.pwd_usr,
                    nom_usr = modelo.nom_usr,
                    ape_usr = modelo.ape_usr,
                    fna_usr = modelo.fna_usr,
                    num_doc = modelo.num_doc,
                    ide_doc = modelo.ide_doc,
                    ide_rol = 1 // Rol Cliente
                };

                var client = GetClient();
                var json = JsonConvert.SerializeObject(cliente);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("/api/Cliente/nuevoCliente", content);

                if (response.IsSuccessStatusCode)
                {
                    return "success";
                }
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        private void EstablecerSesionCliente(string token, long idUsuario)
        {
            HttpContext.Session.SetString("token", token);
            HttpContext.Session.SetInt32("ClienteId", (int)idUsuario);
        }

        #endregion

        #region Login

        [HttpGet]
        public IActionResult Index()
        {
            HttpContext.Session.Clear();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string uid, string pwd)
        {
            if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(pwd))
            {
                ViewBag.Mensaje = "Correo y contraseña son obligatorios.";
                return View();
            }

            string rol = await IniciarSesionAsync(uid, pwd);

            if (rol == "denied" || string.IsNullOrWhiteSpace(rol))
            {
                ViewBag.correo = uid;
                ViewBag.Mensaje = "Usuario o contraseña incorrectos.";
                return View();
            }

            string token = await ObtenerTokenAsync(uid);

            if (!long.TryParse(token, out long idUsuario) || idUsuario <= 0)
            {
                ViewBag.correo = uid;
                ViewBag.Mensaje = "Error al autenticar. Inténtelo de nuevo.";
                return View();
            }

            HttpContext.Session.SetString("token", token);

            // Guardar el ID específico por rol
            if (rol == "Cliente")
            {
                HttpContext.Session.SetInt32("ClienteId", (int)idUsuario);
            }
            else if (rol == "Veterinario")
            {
                HttpContext.Session.SetInt32("VeterinarioId", (int)idUsuario);
            }
            else if (rol == "Recepcionista")
            {
                HttpContext.Session.SetInt32("RecepcionistaId", (int)idUsuario);
            }
            else
            {
                ViewBag.correo = uid;
                ViewBag.Mensaje = "Rol no reconocido.";
                return View();
            }

            // >>>> NUEVO: Guardar el nombre del usuario en la sesión <<<<
            var nombreCompleto = await ObtenerNombreUsuarioAsync(idUsuario);
            HttpContext.Session.SetString("NombreUsuario", nombreCompleto);

        

            return RedirectToAction("Index", rol);
        }


        #endregion

        #region Registro

        [HttpGet]
        public async Task<IActionResult> Registro()
        {
            var documentos = await ObtenerTiposDocumentoAsync();
            ViewBag.TiposDocumento = new SelectList(documentos, "ide_doc", "nom_doc");
            return View(new RegistroClienteViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Registro(RegistroClienteViewModel modelo)
        {
            var documentos = await ObtenerTiposDocumentoAsync();
            ViewBag.TiposDocumento = new SelectList(documentos, "ide_doc", "nom_doc");

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            // Registrar el cliente en la API
            var resultado = await RegistrarClienteAsync(modelo);

            if (resultado != "success")
            {
                // Verificar si el error es por correo duplicado
                if (resultado.Contains("duplicate", StringComparison.OrdinalIgnoreCase) || 
                    resultado.Contains("existe", StringComparison.OrdinalIgnoreCase) ||
                    resultado.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("cor_usr", "Este correo ya está registrado.");
                }
                else
                {
                    ViewBag.MensajeError = "Error al registrar. Por favor, intente nuevamente.";
                }
                return View(modelo);
            }

            // Registro exitoso - Iniciar sesión automáticamente
            string token = await ObtenerTokenAsync(modelo.cor_usr!);

            if (long.TryParse(token, out long idUsuario) && idUsuario > 0)
            {
                EstablecerSesionCliente(token, idUsuario);
                return RedirectToAction("Index", "Cliente");
            }

            // Si por alguna razón no se pudo obtener el token, redirigir al login
            return RedirectToAction("Index");
        }

        #endregion

        private async Task<string> ObtenerNombreUsuarioAsync(long idUsuario)
        {
            try
            {
                var client = GetClient();
                var response = await client.GetAsync($"/api/Usuario/ObtenerNombreUsuario?id={idUsuario}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    // >>>> CORRECCIÓN: Eliminar las comillas dobles <<<<
                    var nombre = json.Trim('"'); // Elimina las comillas dobles del principio y final
                    Console.WriteLine($"[DEBUG] Nombre obtenido de la API para ID {idUsuario}: '{nombre}'");
                    return nombre;
                }
                else
                {
                    Console.WriteLine($"[ERROR] API devolvió código {response.StatusCode} para ID {idUsuario}");
                    return "Usuario";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EXCEPTION] Error al obtener nombre: {ex.Message}");
                return "Usuario";
            }
        }


    }
}