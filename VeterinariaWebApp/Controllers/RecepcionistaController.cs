using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Rotativa.AspNetCore;
using Rotativa.AspNetCore.Options;
using System.Text;
using VeterinariaWebApp.Models.Cita;
using VeterinariaWebApp.Models.Usuario;
using VeterinariaWebApp.Models.Usuario.Cliente;
using VeterinariaWebApp.Models.Pago;
using VeterinariaWebApp.Models.Usuario.Veterinario;
using Microsoft.Extensions.Configuration;

namespace VeterinariaWebApp.Controllers
{
    public class RecepcionistaController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RecepcionistaController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient GetClient() => _httpClientFactory.CreateClient("ClinicaAPI");



        public async Task<IActionResult> Index()
        {
            var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
            if (recepcionistaId == null || recepcionistaId == 0)
                return RedirectToAction("Index", "Login");
            var pendientes = await ObtenerListaCitas("P");
            var atendidas = await ObtenerListaCitas("A");
            var canceladas = await ObtenerListaCitas("C");

            ViewBag.CitasPendientes = pendientes.Count;
            ViewBag.CitasAtendidas = atendidas.Count;
            ViewBag.CitasCanceladas = canceladas.Count;

            var today = DateTime.Today;

            // Citas Hoy (pendientes de hoy, solo futuras)
            var citasHoy = pendientes
                .Where(c => c.CalendarioCita.Date == today && c.CalendarioCita >= DateTime.Now)
                .OrderBy(c => c.CalendarioCita).ToList();
            ViewBag.CitasHoyCount = citasHoy.Count;
            ViewBag.CitasHoyJson = JsonConvert.SerializeObject(citasHoy);

            // Tasa de Atención (últimos 30 días)
            var desde30 = today.AddDays(-30);
            var total30dias = pendientes.Concat(atendidas).Concat(canceladas)
                .Where(c => c.CalendarioCita >= desde30).ToList();
            var atendidas30 = total30dias.Count(c => c.EstadoCita == "A");
            ViewBag.TasaAtencion = total30dias.Count > 0
                ? Math.Round((double)atendidas30 / total30dias.Count * 100, 1)
                : 0;

            // Ingresos del Mes (pagos realizados, no citas atendidas)
            var pagosRealizados = await ObtenerPagosRealizados();
            var inicioMes = new DateTime(today.Year, today.Month, 1);
            var ingresosMes = pagosRealizados
                .Where(p => p.HoraPago >= inicioMes)
                .Sum(p => p.MontoPago);
            ViewBag.IngresosMes = ingresosMes;
            ViewBag.PagosRealizadosJson = JsonConvert.SerializeObject(pagosRealizados);

            // Mascotas Únicas Atendidas
            ViewBag.MascotasUnicas = atendidas.Select(c => c.NombreMascota).Distinct().Count();

            // Últimas 5 atenciones
            var ultimasAtenciones = atendidas.OrderByDescending(c => c.CalendarioCita).Take(5).ToList();
            ViewBag.UltimasAtencionesJson = JsonConvert.SerializeObject(ultimasAtenciones);

            // Todas las citas para charts (excluye canceladas)
            var todasCitas = pendientes.Concat(atendidas).Concat(canceladas).ToList();
            ViewBag.TodasCitasJson = JsonConvert.SerializeObject(todasCitas);

            return View();
        }


        private async Task<List<Cita>> ObtenerListaCitas(string estado)
        {
            var url = $"Cita/listaCitasPorEstado?estado={estado}";
            var response = await GetClient().GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Cita>>(json) ?? new List<Cita>();
            }
            return new List<Cita>();
        }

        private async Task<List<Pago>> ObtenerPagosRealizados()
        {
            var response = await GetClient().GetAsync($"Pago/ListarPagosRealizados");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Pago>>(json) ?? new List<Pago>();
            }
            return new List<Pago>();
        }





        public List<Veterinario> ArregloVeterinarios()
        {
            List<Veterinario> aVeterinarios = new List<Veterinario>();
            try
            {
                string url = $"Veterinario/listaVeterinarios";
                HttpResponseMessage response = GetClient().GetAsync(url).Result;
                if (response.IsSuccessStatusCode)
                {
                    var data = response.Content.ReadAsStringAsync().Result;
                    aVeterinarios = JsonConvert.DeserializeObject<List<Veterinario>>(data) ?? new List<Veterinario>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener veterinarios: {ex.Message}");
            }
            return aVeterinarios;
        }

      
        public List<Cliente> ArregloClientes()
        {
            List<Cliente> aClientes = new List<Cliente>();
            try
            {
                string url = $"Cliente/listaClientes";
                HttpResponseMessage response = GetClient().GetAsync(url).Result;
                if (response.IsSuccessStatusCode)
                {
                    var data = response.Content.ReadAsStringAsync().Result;
                    aClientes = JsonConvert.DeserializeObject<List<Cliente>>(data) ?? new List<Cliente>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener clientes: {ex.Message}");
            }
            return aClientes;
        }

        public List<Especialidad> ArregloEspecialidad()
        {
            List<Especialidad> aEspecialidad = new List<Especialidad>();
            try
            {
                string url = $"Veterinario/listarEspecialidad";
                HttpResponseMessage response = GetClient().GetAsync(url).Result;
                if (response.IsSuccessStatusCode)
                {
                    var data = response.Content.ReadAsStringAsync().Result;
                    aEspecialidad = JsonConvert.DeserializeObject<List<Especialidad>>(data) ?? new List<Especialidad>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener especialidades: {ex.Message}");
            }
            return aEspecialidad;
        }

        public List<UserDoc> ArregloTipoDocumentos()
        {
            List<UserDoc> aTDocumentos = new List<UserDoc>();
            try
            {
                string url = $"Usuario/ListarDocumentos";
                HttpResponseMessage response = GetClient().GetAsync(url).Result;
                if (response.IsSuccessStatusCode)
                {
                    var data = response.Content.ReadAsStringAsync().Result;
                    aTDocumentos = JsonConvert.DeserializeObject<List<UserDoc>>(data) ?? new List<UserDoc>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener tipos de documento: {ex.Message}");
            }
            return aTDocumentos;
        }

        public String AgregarVeterinario(VeterinarioO veterinario)
        {
            StringContent content = new StringContent(JsonConvert.SerializeObject(veterinario), Encoding.UTF8, "application/json");
            try
            {
                string url = $"Veterinario/nuevoVeterinario";
                HttpResponseMessage response = GetClient().PostAsync(url, content).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al agregar veterinario: {ex.Message}");
                return "Error al guardar veterinario";
            }
        }

        public String AgregarCliente(ClienteO cliente)
        {
            StringContent content = new StringContent(JsonConvert.SerializeObject(cliente), Encoding.UTF8, "application/json");
            try
            {
                string url = $"Cliente/nuevoCliente";
                HttpResponseMessage response = GetClient().PostAsync(url, content).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al agregar cliente: {ex.Message}");
                return "Error al guardar cliente";
            }
        }

        [HttpGet]
        public IActionResult NuevoVeterinario()
        {
            var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
            if (recepcionistaId == null || recepcionistaId == 0)
                return RedirectToAction("Index", "Login");
            ViewBag.especialidad = new SelectList(ArregloEspecialidad(), "ide_esp", "nom_esp");
            ViewBag.documentos = new SelectList(ArregloTipoDocumentos(), "ide_doc", "nom_doc");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NuevoVeterinario(VeterinarioO veterinario)
        {
            var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
            if (recepcionistaId == null || recepcionistaId == 0)
                return RedirectToAction("Index", "Login");

            if (!ModelState.IsValid)
            {
                ViewBag.especialidad = new SelectList(ArregloEspecialidad(), "ide_esp", "nom_esp");
                ViewBag.documentos = new SelectList(ArregloTipoDocumentos(), "ide_doc", "nom_doc");
                return View(veterinario);
            }

            ViewBag.respuesta = AgregarVeterinario(veterinario);
            return RedirectToAction("listarVeterinarios");
        }

        public IActionResult listarVeterinarios()
        {
            var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
            if (recepcionistaId == null || recepcionistaId == 0)
                return RedirectToAction("Index", "Login");
            var veterinarios = ArregloVeterinarios(); 
            return View(veterinarios); 
        }

        public IActionResult listarVeterinariosPDF()
        {
            var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
            if (recepcionistaId == null || recepcionistaId == 0)
                return RedirectToAction("Index", "Login");
            String hoy = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            return new ViewAsPdf("listarVeterinariosPDF", ArregloVeterinarios())
            {
                FileName = $"ListadoVeterinarios-{hoy}.pdf",
                PageOrientation = Orientation.Portrait,
                PageSize = Size.A4
            };
        }

        [HttpGet]
        public IActionResult NuevoCliente()
        {
            var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
            if (recepcionistaId == null || recepcionistaId == 0)
                return RedirectToAction("Index", "Login");
            ViewBag.documentos = new SelectList(ArregloTipoDocumentos(), "ide_doc", "nom_doc");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NuevoCliente(ClienteO cliente)
        {
            var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
            if (recepcionistaId == null || recepcionistaId == 0)
                return RedirectToAction("Index", "Login");

            if (!ModelState.IsValid)
            {
                ViewBag.documentos = new SelectList(ArregloTipoDocumentos(), "ide_doc", "nom_doc");
                return View(cliente);
            }

            ViewBag.respuesta = AgregarCliente(cliente);
            return RedirectToAction("listarClientes");
        }

        public IActionResult listarClientes()
        {
            var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
            if (recepcionistaId == null || recepcionistaId == 0)
                return RedirectToAction("Index", "Login");
            var clientes = ArregloClientes(); 
            return View(clientes);
        }



        public IActionResult listarClientesPDF()
        {
            var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
            if (recepcionistaId == null || recepcionistaId == 0)
                return RedirectToAction("Index", "Login");
            String hoy = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            return new ViewAsPdf("listarClientesPDF", ArregloClientes())
            {
                FileName = $"ListadoClientes-{hoy}.pdf",
                PageOrientation = Orientation.Portrait,
                PageSize = Size.A4
            };
        }

   
    }
}