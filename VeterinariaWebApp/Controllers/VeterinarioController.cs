using VeterinariaWebApp.Models.Usuario.Veterinario;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Rotativa.AspNetCore;

namespace VeterinariaWebApp.Controllers;

public class VeterinarioController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public VeterinarioController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient GetClient() => _httpClientFactory.CreateClient("ClinicaAPI");

    // ==================== DASHBOARD ====================

    public async Task<IActionResult> Index()
    {
        var veterinarioId = HttpContext.Session.GetInt32("VeterinarioId");
        if (veterinarioId == null || veterinarioId == 0)
        {
            return RedirectToAction("Index", "Login");
        }

        // Obtener las estadísticas
        VeterinarioStats stats = new VeterinarioStats();
        try
        {
            string url = $"Veterinario/estadisticasVeterinario/{veterinarioId}";
            HttpResponseMessage response = await GetClient().GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                stats = JsonConvert.DeserializeObject<VeterinarioStats>(data) ?? new VeterinarioStats();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener estadísticas: {ex.Message}");
        }

        // Obtener los datos del veterinario
        var veterinario = new Veterinario();
        try
        {
            string url = $"Veterinario/buscarVeterinario/{veterinarioId}";
            HttpResponseMessage response = await GetClient().GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                veterinario = JsonConvert.DeserializeObject<Veterinario>(data) ?? new Veterinario();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener datos del veterinario: {ex.Message}");
        }

        // Obtener TODAS las citas del veterinario para cálculos del dashboard
        var todasLasCitas = await ArregloCitaVeterinario(veterinarioId.Value);
        var citas = todasLasCitas ?? new List<CitaVeterinario>();

        // Obtener ingresos totales desde pagos realizados
        decimal ingresosTotales = 0;
        try
        {
            var ingResponse = await GetClient().GetAsync($"Veterinario/ingresosRealizados/{veterinarioId}");
            if (ingResponse.IsSuccessStatusCode)
            {
                var ingData = await ingResponse.Content.ReadAsStringAsync();
                ingresosTotales = !string.IsNullOrWhiteSpace(ingData) ? JsonConvert.DeserializeObject<decimal>(ingData) : 0m;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener ingresos: {ex.Message}");
        }

        // --- KPIs ---
            var pendientes = citas.Count(c => c.est_cit == "P");
            var atendidas = citas.Count(c => c.est_cit == "A");
            var citasHoy = citas.Where(c => c.cal_cit.Date == DateTime.Today).ToList();
            var citasHoyCount = citasHoy.Count;
            var enAtencion = citas.Count(c => c.est_cit == "E");

        // --- Datos para Chart.js ---
        // Distribución por estado
        var distribucionEstado = new Dictionary<string, int>
        {
            { "Pendientes", pendientes },
            { "En Atención", enAtencion },
            { "Atendidas", atendidas },
            { "Canceladas", citas.Count(c => c.est_cit == "C") }
        };

        // Citas por día del mes actual
        var citasDelMes = citas
            .Where(c => c.cal_cit.Year == DateTime.Today.Year && c.cal_cit.Month == DateTime.Today.Month)
            .ToList();
        var diasDelMes = Enumerable.Range(1, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
        var citasPorDia = diasDelMes.Select(d => new
        {
            dia = d,
            total = citasDelMes.Count(c => c.cal_cit.Day == d)
        }).ToList();

        // Especies atendidas
        var especiesAtendidas = citas
            .Where(c => c.est_cit == "A")
            .GroupBy(c => c.especie)
            .Select(g => new { especie = g.Key, total = g.Count() })
            .OrderByDescending(x => x.total)
            .ToList();

        // --- Listas ---
        var proximasCitas = citas
            .Where(c => c.cal_cit >= DateTime.Now && c.est_cit != "A" && c.est_cit != "C")
            .OrderBy(c => c.cal_cit)
            .Take(5)
            .ToList();

        var ultimasAtenciones = citas
            .Where(c => c.est_cit == "A")
            .OrderByDescending(c => c.cal_cit)
            .Take(5)
            .ToList();

        // --- ViewBag ---
        ViewBag.Stats = stats;
        ViewBag.Veterinario = veterinario;
        ViewBag.TotalCitas = stats?.TotalCitas ?? 0;
        ViewBag.Pendientes = pendientes;
        ViewBag.Atendidas = atendidas;
        ViewBag.CitasHoyCount = citasHoyCount;
        ViewBag.EnAtencion = enAtencion;
        ViewBag.IngresosTotales = ingresosTotales;
        ViewBag.TodasCitasJSON = JsonConvert.SerializeObject(citas.Select(c => new
        {
            c.ide_cit,
            c.cal_cit,
            c.con_cit,
            c.mascota,
            c.especie,
            c.nombre_dueno,
            c.est_cit,
            c.mon_pag
        }));
        ViewBag.DistribucionEstadoJSON = JsonConvert.SerializeObject(distribucionEstado);
        ViewBag.CitasPorDiaJSON = JsonConvert.SerializeObject(citasPorDia);
        ViewBag.EspeciesAtendidasJSON = JsonConvert.SerializeObject(especiesAtendidas);
        ViewBag.ProximasCitasJSON = JsonConvert.SerializeObject(proximasCitas.Select(c => new
        {
            c.cal_cit,
            c.mascota,
            c.especie,
            c.nombre_dueno,
            c.con_cit,
            c.est_cit
        }));
        ViewBag.UltimasAtencionesJSON = JsonConvert.SerializeObject(ultimasAtenciones.Select(c => new
        {
            c.cal_cit,
            c.mascota,
            c.especie,
            c.nombre_dueno,
            c.mon_pag
        }));

        return View();
    }

    // ==================== CITAS ====================

    // Método auxiliar para obtener citas
    public async Task<List<CitaVeterinario>> ArregloCitaVeterinario(long ide_usr)
    {
        List<CitaVeterinario> aCitaVeterinario = new();
        try
        {
            string url = $"Veterinario/listaCitasPorVeterinario/{ide_usr}";
            HttpResponseMessage response = await GetClient().GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                aCitaVeterinario = JsonConvert.DeserializeObject<List<CitaVeterinario>>(data) ?? new List<CitaVeterinario>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener citas del veterinario: {ex.Message}");
        }
        return aCitaVeterinario;
    }

    // GET: Lista de citas del veterinario
    [HttpGet]
    public async Task<IActionResult> listaCitaPorVeterinarios()
    {
        var veterinarioId = HttpContext.Session.GetInt32("VeterinarioId");
        if (veterinarioId == null || veterinarioId == 0)
        {
            return RedirectToAction("Index", "Login");
        }

        var citas = await ArregloCitaVeterinario(veterinarioId.Value);
        return View(citas);
    }

    // GET: Iniciar atención de cita (cambiar estado a "En Atención")
    [HttpGet]
    public async Task<IActionResult> IniciarAtencion(long id)
    {
        var veterinarioId = HttpContext.Session.GetInt32("VeterinarioId");
        if (veterinarioId == null || veterinarioId == 0)
        {
            return RedirectToAction("Index", "Login");
        }

        try
        {
            // Cambiar estado a "En Atención" (E)
            var response = await GetClient().PutAsync($"Cita/actualizarEstado/{id}?estado=E", null);

            if (response.IsSuccessStatusCode)
            {
                TempData["Exito"] = "Atención iniciada. Complete el formulario de atención médica.";
                return RedirectToAction("AtenderCita", new { id = id });
            }
            else
            {
                TempData["Error"] = "No se pudo iniciar la atención de la cita.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error: " + ex.Message;
        }

        return RedirectToAction("listaCitaPorVeterinarios");
    }

    // POST: Marcar inasistencia + enviar correo
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarInasistencia(long idCita)
    {
        var veterinarioId = HttpContext.Session.GetInt32("VeterinarioId");
        if (veterinarioId == null || veterinarioId == 0)
            return RedirectToAction("Index", "Login");

        try
        {
            var response = await GetClient().PutAsync($"Cita/marcarInasistencia/{idCita}", null);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeAnonymousType(json, new { email = "", nombreCliente = "", nombreMascota = "", fechaCita = "" });

                if (!string.IsNullOrEmpty(data?.email))
                {
                    var emailRequest = new { data.email, data.nombreCliente, data.nombreMascota, data.fechaCita };
                    var jsonContent = new StringContent(JsonConvert.SerializeObject(emailRequest), System.Text.Encoding.UTF8, "application/json");
                    await GetClient().PostAsync("Cita/enviarEmailInasistencia", jsonContent);
                }

                TempData["Exito"] = "Cita cancelada por inasistencia. Se ha notificado al cliente.";
            }
            else
            {
                TempData["Error"] = "No se pudo cancelar la cita.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error: " + ex.Message;
        }

        return RedirectToAction("listaCitaPorVeterinarios");
    }

    // GET: Formulario de atención médica
    [HttpGet]
    public async Task<IActionResult> AtenderCita(long id)
    {
        var veterinarioId = HttpContext.Session.GetInt32("VeterinarioId");
        if (veterinarioId == null || veterinarioId == 0)
        {
            return RedirectToAction("Index", "Login");
        }

        // Obtener datos de la cita
        var citas = await ArregloCitaVeterinario(veterinarioId.Value);
        var cita = citas.FirstOrDefault(c => c.ide_cit == id);

        if (cita == null)
        {
            TempData["Error"] = "No se encontró la cita especificada.";
            return RedirectToAction("listaCitaPorVeterinarios");
        }

        // Verificar que la cita esté en estado "En Atención"
        if (cita.est_cit != "E")
        {
            TempData["Error"] = "Esta cita no está en proceso de atención.";
            return RedirectToAction("listaCitaPorVeterinarios");
        }

        // Crear el ViewModel
        var viewModel = new AtencionCitaViewModel
        {
            IdCita = cita.ide_cit,
            FechaCita = cita.cal_cit,
            Consultorio = cita.con_cit,
            NombreMascota = cita.mascota,
            Especie = cita.especie,
            NombreDueno = cita.nombre_dueno,
            DocumentoDueno = cita.doc_dueno,
            MontoPago = cita.mon_pag,
            MetodoPago = cita.nom_pay,
            EstadoCita = cita.est_cit
        };

        return View(viewModel);
    }

    // POST: Finalizar atención (guardar diagnóstico y cambiar estado a "Atendida")
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FinalizarAtencion(AtencionCitaViewModel model)
    {
        var veterinarioId = HttpContext.Session.GetInt32("VeterinarioId");
        if (veterinarioId == null || veterinarioId == 0)
        {
            return RedirectToAction("Index", "Login");
        }

        // Validar solo los campos obligatorios manualmente
        if (string.IsNullOrWhiteSpace(model.Diagnostico) || string.IsNullOrWhiteSpace(model.Tratamiento))
        {
            TempData["Error"] = "Debe completar el diagnóstico y tratamiento.";

            // Recargar datos de la cita para mostrar la vista correctamente
            var citas = await ArregloCitaVeterinario(veterinarioId.Value);
            var cita = citas.FirstOrDefault(c => c.ide_cit == model.IdCita);
            if (cita != null)
            {
                model.NombreMascota = cita.mascota;
                model.Especie = cita.especie;
                model.NombreDueno = cita.nombre_dueno;
                model.DocumentoDueno = cita.doc_dueno;
                model.MontoPago = cita.mon_pag;
                model.MetodoPago = cita.nom_pay;
                model.FechaCita = cita.cal_cit;
                model.Consultorio = cita.con_cit;
                model.EstadoCita = cita.est_cit;
            }
            return View("AtenderCita", model);
        }

        try
        {
            // 1. Guardar historial médico
            var historialDto = new
            {
                IdCita = model.IdCita,
                Sintomas = model.Sintomas,
                Diagnostico = model.Diagnostico,
                Tratamiento = model.Tratamiento,
                Medicamentos = model.Medicamentos,
                Observaciones = model.Observaciones
            };

            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(historialDto),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var historialResponse = await GetClient().PostAsync($"Cita/agregarHistorial", jsonContent);

            if (!historialResponse.IsSuccessStatusCode)
            {
                TempData["Error"] = "Error al guardar el historial médico.";
                return RedirectToAction("AtenderCita", new { id = model.IdCita });
            }

            // 2. Cambiar estado a "Atendida"
            var estadoResponse = await GetClient().PutAsync($"Cita/actualizarEstado/{model.IdCita}?estado=A", null);

            if (estadoResponse.IsSuccessStatusCode)
            {
                TempData["Exito"] = $"¡Atención finalizada! La cita de {model.NombreMascota} ha sido completada y el historial médico guardado.";
                return RedirectToAction("listaCitaPorVeterinarios");
            }
            else
            {
                TempData["Error"] = "No se pudo finalizar la atención. Intente nuevamente.";
                return RedirectToAction("AtenderCita", new { id = model.IdCita });
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error: " + ex.Message;
            return RedirectToAction("AtenderCita", new { id = model.IdCita });
        }
    }

    // GET: Ver detalle de cita atendida
    [HttpGet]
    public async Task<IActionResult> DetalleCitaAtendida(long id)
    {
        var veterinarioId = HttpContext.Session.GetInt32("VeterinarioId");
        if (veterinarioId == null || veterinarioId == 0)
        {
            return RedirectToAction("Index", "Login");
        }

        var citas = await ArregloCitaVeterinario(veterinarioId.Value);
        var cita = citas.FirstOrDefault(c => c.ide_cit == id);

        if (cita == null)
        {
            TempData["Error"] = "No se encontró la cita especificada.";
            return RedirectToAction("listaCitaPorVeterinarios");
        }

        return View(cita);
    }

    // ==================== MASCOTAS ====================

    // Método auxiliar para obtener mascotas (vista simple)
    public async Task<List<MascotaPorVeterinario>> ArregloMascotaPorVeterinario(long ide_usr)
    {
        List<MascotaPorVeterinario> aMascotaVeterinario = new();
        try
        {
            string url = $"Veterinario/listaMascotasPorVeterinario/{ide_usr}";
            HttpResponseMessage response = await GetClient().GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                aMascotaVeterinario = JsonConvert.DeserializeObject<List<MascotaPorVeterinario>>(data) ?? new List<MascotaPorVeterinario>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener mascotas del veterinario: {ex.Message}");
        }
        return aMascotaVeterinario;
    }

    // Método auxiliar para obtener mascotas atendidas CON HISTORIAL
    public async Task<List<MascotaAtendida>> ArregloMascotasAtendidasConHistorial(long ide_usr)
    {
        List<MascotaAtendida> lista = new();
        try
        {
            string url = $"Veterinario/listaMascotasAtendidasConHistorial/{ide_usr}";
            HttpResponseMessage response = await GetClient().GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                lista = JsonConvert.DeserializeObject<List<MascotaAtendida>>(data) ?? new List<MascotaAtendida>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener mascotas atendidas: {ex.Message}");
        }
        return lista;
    }

    // GET: Lista de mascotas atendidas (vista mejorada con historial)
    [HttpGet]
    public async Task<IActionResult> listaMascotaPorVeterinarios()
    {
        var veterinarioId = HttpContext.Session.GetInt32("VeterinarioId");
        if (veterinarioId == null || veterinarioId == 0)
        {
            return RedirectToAction("Index", "Login");
        }

        var mascotas = await ArregloMascotasAtendidasConHistorial(veterinarioId.Value);
        return View("listaMascotaAtendidasConHistorial", mascotas);
    }

    // GET: Generar PDF de una mascota atendida específica
    [HttpGet]
    public async Task<IActionResult> GenerarPDFMascotaAtendida(long idCita)
    {
        var veterinarioId = HttpContext.Session.GetInt32("VeterinarioId");
        if (veterinarioId == null || veterinarioId == 0)
        {
            return RedirectToAction("Index", "Login");
        }

        var mascotas = await ArregloMascotasAtendidasConHistorial(veterinarioId.Value);
        var mascota = mascotas.FirstOrDefault(m => m.ide_cit == idCita);

        if (mascota == null)
        {
            TempData["Error"] = "No se encontró el registro de atención.";
            return RedirectToAction("listaMascotaPorVeterinarios");
        }

        string hoy = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        return new ViewAsPdf("GenerarPDFMascotaAtendida", mascota)
        {
            FileName = $"HistorialMedico-{mascota.mascota}-{hoy}.pdf",
            PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
            PageSize = Rotativa.AspNetCore.Options.Size.A4
        };
    }

    // GET: Generar PDF de mascotas atendidas (lista general - legacy)
    [HttpGet]
    public async Task<IActionResult> GenerarPDFMascotasAtendidas()
    {
        var veterinarioId = HttpContext.Session.GetInt32("VeterinarioId");
        if (veterinarioId == null || veterinarioId == 0)
        {
            return RedirectToAction("Index", "Login");
        }

        var mascotas = await ArregloMascotaPorVeterinario(veterinarioId.Value);
        string hoy = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        return new ViewAsPdf("GenerarPDFMascotasAtendidas", mascotas)
        {
            FileName = $"MascotasAtendidas-{hoy}.pdf",
            PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
            PageSize = Rotativa.AspNetCore.Options.Size.A4
        };
    }

}
