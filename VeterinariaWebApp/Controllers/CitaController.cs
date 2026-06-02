using VeterinariaWebApp.Models;
using VeterinariaWebApp.Models.Cita;
using VeterinariaWebApp.Models.Pago;
using VeterinariaWebApp.Models.Usuario.Veterinario;
using VeterinariaWebApp.Models.Usuario.Cliente;
using VeterinariaWebApp.Models.Mascota;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rotativa.AspNetCore;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace VeterinariaWebApp.Controllers;

public class CitaController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _memoryCache;

    public CitaController(IConfiguration configuration, IMemoryCache memoryCache, IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
    }

    private HttpClient GetClient() => _httpClientFactory.CreateClient("ClinicaAPI");

    // Método auxiliar para obtener citas
    public List<Cita> ArregloCitas()
    {
        List<Cita> aCitas = new List<Cita>();
        try
        {
            string url = "Cita/listaCita";
            HttpResponseMessage response = GetClient().GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                var data = response.Content.ReadAsStringAsync().Result;
                aCitas = JsonConvert.DeserializeObject<List<Cita>>(data) ?? new List<Cita>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener citas: {ex.Message}");
        }
        return aCitas;
    }

 
    public List<Veterinario> listadoVeterinario()
    {
        List<Veterinario> aVeterinarios = new List<Veterinario>();
        try
        {
            string url = "Veterinario/listaVeterinarios";
            HttpResponseMessage response = GetClient().GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                var data = response.Content.ReadAsStringAsync().Result;
                aVeterinarios = JsonConvert.DeserializeObject<List<Veterinario>>(data) ?? new List<Veterinario>();
            }
            else
            {
                Console.WriteLine($"Error al obtener veterinarios: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener veterinarios: {ex.Message}");
        }
        return aVeterinarios;
    }

    //  listadoCliente
    public List<Cliente> listadoCliente()
    {
        List<Cliente> aClientes = new List<Cliente>();
        try
        {
            string url = "Cliente/listaClientes";
            HttpResponseMessage response = GetClient().GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                var data = response.Content.ReadAsStringAsync().Result;
                aClientes = JsonConvert.DeserializeObject<List<Cliente>>(data) ?? new List<Cliente>();
            }
            else
            {
                Console.WriteLine($"Error al obtener clientes: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener clientes: {ex.Message}");
        }
        return aClientes;
    }





    //LISTADO DE CITAS PENDIENTES 

    public async Task<IActionResult> CitasPendientes()
    {
        var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
        if (recepcionistaId == null || recepcionistaId == 0)
            return RedirectToAction("Index", "Login");
        var response = await GetClient().GetAsync("Cita/listaCitasPendientes");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var lista = JsonConvert.DeserializeObject<List<Cita>>(json) ?? new List<Cita>();
            return View(lista);
        }
        return View(new List<Cita>());
    }


    // GET: Cita/Reagendar/{id}
    [HttpGet]
    public async Task<IActionResult> Reagendar(long id)
    {
        var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
        if (recepcionistaId == null || recepcionistaId == 0)
            return RedirectToAction("Index", "Login");

        var response = await GetClient().GetAsync($"Cita/buscarCitaConDetalle/{id}");
        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "No se encontró la cita.";
            return RedirectToAction("CitasCanceladas");
        }

        var content = await response.Content.ReadAsStringAsync();
        var jsonObj = Newtonsoft.Json.Linq.JObject.Parse(content);

        var nomMascota = (string?)jsonObj["nombreMascota"];
        var montoPago = (decimal?)jsonObj["montoPago"];
        var nomVet = (string?)jsonObj["nombreVeterinario"];
        DateTime.TryParse(jsonObj["calendarioCita"]?.ToString(), out var fechaCita);

        ViewBag.NombreMascota = nomMascota ?? "";
        ViewBag.MontoPago = montoPago ?? 0m;
        ViewBag.NombreVeterinario = nomVet ?? "";
        ViewBag.FechaOriginal = fechaCita.ToString("yyyy-MM-dd");

        var cita = new CitaO
        {
            IdCita = (long?)jsonObj["idCita"] ?? 0,
            CalendarioCita = fechaCita,
            Consultorio = (long?)jsonObj["consultorio"] ?? 0,
            IdVeterinario = (long?)jsonObj["idVeterinario"] ?? 0
        };

        // Forzar el consultorio y veterinario desde la cita original
        try
        {
            var conResp = await GetClient().GetAsync("Consultorio/listar");
            if (conResp.IsSuccessStatusCode)
            {
                var conJson = await conResp.Content.ReadAsStringAsync();
                var consultorios = JsonConvert.DeserializeObject<List<Consultorio>>(conJson);
                ViewBag.ConsultorioNombre = consultorios?.FirstOrDefault(c => c.IdConsultorio == cita.Consultorio)?.Nombre ?? "Sin asignar";
            }
            else
            {
                ViewBag.ConsultorioNombre = "Sin asignar";
            }
        }
        catch
        {
            ViewBag.ConsultorioNombre = "Sin asignar";
        }

        return View(cita);
    }

    // POST: Cita/Reagendar
    [HttpPost]
    public async Task<IActionResult> Reagendar(CitaO obj)
    {
        var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
        if (recepcionistaId == null || recepcionistaId == 0)
            return RedirectToAction("Index", "Login");

        var json = JsonConvert.SerializeObject(obj);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await GetClient().PutAsync($"Cita/reagendar/{obj.IdCita}", content);

        if (response.IsSuccessStatusCode)
        {
            TempData["Exito"] = "Cita reagendada correctamente.";
            return RedirectToAction("CitasCanceladas");
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            TempData["Error"] = error;
            return RedirectToAction("Reagendar", new { id = obj.IdCita });
        }
    }

    //Listado de Citas Atendidas
    public async Task<IActionResult> CitasAtendidas()
    {
        var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
        if (recepcionistaId == null || recepcionistaId == 0)
            return RedirectToAction("Index", "Login");
        var response = await GetClient().GetAsync("Cita/listaCitasAtendidas");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var lista = JsonConvert.DeserializeObject<List<Cita>>(json) ?? new List<Cita>();
            return View(lista);
        }
        return View(new List<Cita>());
    }

    //Listado de Citas Canceladas

    public async Task<IActionResult> CitasCanceladas()
    {
        var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
        if (recepcionistaId == null || recepcionistaId == 0)
            return RedirectToAction("Index", "Login");
        var response = await GetClient().GetAsync("Cita/listaCitasCanceladas");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var lista = JsonConvert.DeserializeObject<List<Cita>>(json) ?? new List<Cita>();
            return View(lista);
        }
        return View(new List<Cita>());
    }






    private async Task<bool> ExisteCita(CitaO obj)
    {
        List<Cita> citas = ArregloCitas();
        return citas.Any(c => c.NombreVeterinario != null &&
                             c.CalendarioCita.Date == obj.CalendarioCita.Date &&
                             c.CalendarioCita.Hour == obj.CalendarioCita.Hour &&
                             c.CalendarioCita.Minute == obj.CalendarioCita.Minute);
    }

    //  Cita/IniciarCreacionCita
    [HttpGet]
    public async Task<IActionResult> IniciarCreacionCita()
    {
        var pagoPendiente = await ObtenerPagoPendiente();

        if (pagoPendiente != null)
        {
            // Si hay un pago pendiente, redirigir directamente al formulario de cita
            return RedirectToAction("nuevaCita", new { PagoId = pagoPendiente.IdPago });
        }
        else
        {
            // Si no hay un pago pendiente, redirigir al formulario de pago
            return RedirectToAction("Crear", "Pago");
        }
    }



    //  Cita/nuevaCita
 
    [HttpGet]
    public async Task<IActionResult> nuevaCita(int PagoId)
    {
        int? idCliente = HttpContext.Session.GetInt32("ClienteId");
        if (idCliente == null || idCliente == 0)
            return RedirectToAction("Index", "Login");

        if (PagoId > 0)
        {
            try
            {
                var response = await GetClient().GetAsync($"Pago/VerificarAutorizacion/{PagoId}");
                if (response.IsSuccessStatusCode)
                {
                    var autPagStr = (await response.Content.ReadAsStringAsync()).Trim('"');
                    ViewBag.AutPag = autPagStr == "true" ? true : (autPagStr == "false" ? false : (bool?)null);
                }
            }
            catch
            {
                ViewBag.AutPag = true;
            }

            if (ViewBag.AutPag == true)
            {
                ViewBag.MensajePago = "✅ Fondos confirmados correctamente.";
            }
            else
            {
                ViewBag.MensajePago = "🔔 Espere la confirmación del recepcionista para agendar su cita.";
            }
        }

        CargarViewBagsParaCita(idCliente.Value);

        CitaO citaPagada = new CitaO() { IdPago = PagoId };
        return View(citaPagada);
    }



    //ULTIMA MODIFICACION 


    // POST: Cita/nuevaCita
    [HttpPost]
    public async Task<IActionResult> nuevaCita(CitaO obj)
    {
        int? idCliente = HttpContext.Session.GetInt32("ClienteId");
        if (idCliente == null || idCliente == 0)
            return RedirectToAction("Index", "Login");

        if (!ModelState.IsValid)
        {
            CargarViewBagsParaCita(idCliente.Value);
            return View(obj);
        }

        var json = JsonConvert.SerializeObject(obj);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var responseC = await GetClient().PostAsync($"Cita/agregaCita?clienteId={idCliente.Value}", content);

        if (responseC.IsSuccessStatusCode)
        {
            if (obj.IdPago > 0)
            {
                try
                {
                    var resp = await GetClient().GetAsync($"Pago/VerificarAutorizacion/{obj.IdPago}");
                    if (resp.IsSuccessStatusCode)
                    {
                        var aut = (await resp.Content.ReadAsStringAsync()).Trim('"');
                        if (aut == "true")
                            TempData["Exito"] = "✅ ¡Pago exitoso! Su cita ha sido agendada y el pago confirmado.";
                        else
                            TempData["Exito"] = "¡Cita agendada correctamente!";
                    }
                    else
                    {
                        TempData["Exito"] = "¡Cita agendada correctamente!";
                    }
                }
                catch
                {
                    TempData["Exito"] = "¡Cita agendada correctamente!";
                }
            }
            else
            {
                TempData["Exito"] = "¡Cita agendada correctamente!";
            }
            return RedirectToAction("listaCitaPorCliente", "Cliente");
        }
        else
        {
            var errorMessage = await responseC.Content.ReadAsStringAsync();

            // Mostrar el mensaje de error real como toast SweetAlert
            TempData["Error"] = errorMessage;

            CargarViewBagsParaCita(idCliente.Value);
            return View(obj);
        }
    }







    // GET: Cita/EditarCitaCliente
    [HttpGet]
    public async Task<IActionResult> EditarCitaCliente(int id)
    {
        int? idCliente = HttpContext.Session.GetInt32("ClienteId");
        if (idCliente == null || idCliente == 0)
            return RedirectToAction("Index", "Login");

        var response = await GetClient().GetAsync($"Cita/buscarCita/{id}");
        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "No se encontró la cita solicitada.";
            return RedirectToAction("listaCitaPorCliente", "Cliente");
        }

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "No se encontró la cita o la respuesta fue inválida.";
            return RedirectToAction("listaCitaPorCliente", "Cliente");
        }

        var cita = JsonConvert.DeserializeObject<CitaO>(content);
        if (cita == null)
        {
            TempData["Error"] = "No se encontró la cita o la respuesta fue inválida.";
            return RedirectToAction("listaCitaPorCliente", "Cliente");
        }

        // Verificar que falten más de 12 horas para la cita
        if (cita.CalendarioCita <= DateTime.Now.AddHours(12))
        {
            TempData["Error"] = "No puede editar una cita cuando faltan menos de 12 horas para la misma.";
            return RedirectToAction("listaCitaPorCliente", "Cliente");
        }

        CargarViewBagsParaCita(idCliente.Value);
        return View(cita);
    }





    //ULTIMA MODIFICACION 


    // POST: Cita/EditarCitaClientePost
    [HttpPost]
    public async Task<IActionResult> EditarCitaClientePost(CitaO obj)
    {
        int? idCliente = HttpContext.Session.GetInt32("ClienteId");
        if (idCliente == null || idCliente == 0)
            return RedirectToAction("Index", "Login");

        if (!ModelState.IsValid)
        {
            CargarViewBagsParaCita(idCliente.Value);
            return View("EditarCitaCliente", obj);
        }

        var json = JsonConvert.SerializeObject(obj);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await GetClient().PutAsync($"Cita/actualizaCita?id={obj.IdCita}&clienteId={idCliente.Value}", content);

        if (response.IsSuccessStatusCode)
        {
            TempData["Exito"] = "¡Cita actualizada correctamente!";
            return RedirectToAction("listaCitaPorCliente", "Cliente");
        }
        else
        {
            var errorMessage = await response.Content.ReadAsStringAsync();

            TempData["Error"] = errorMessage;

            CargarViewBagsParaCita(idCliente.Value);
            return View("EditarCitaCliente", obj);
        }
    }






    // Método auxiliar para cargar ViewBags de citas
    private string ObtenerMappingConsultorioJson()
    {
        var consultorios = new Dictionary<long, int>();
        try
        {
            var conResponse = GetClient().GetAsync("Veterinario/consultoriosPorVet").Result;
            if (conResponse.IsSuccessStatusCode)
            {
                var data = conResponse.Content.ReadAsStringAsync().Result;
                consultorios = JsonConvert.DeserializeObject<Dictionary<long, int>>(data) ?? new();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener consultorios por vet: {ex.Message}");
        }

        var listaConsultorios = new List<Consultorio>();
        try
        {
            var conListResp = GetClient().GetAsync("Consultorio/listar").Result;
            if (conListResp.IsSuccessStatusCode)
            {
                var conListJson = conListResp.Content.ReadAsStringAsync().Result;
                listaConsultorios = JsonConvert.DeserializeObject<List<Consultorio>>(conListJson) ?? new();
            }
        }
        catch { }

        var veterinarios = listadoVeterinario();
        var mapping = veterinarios.ToDictionary(v => v.IdVeterinario, v =>
        {
            var conId = consultorios.GetValueOrDefault(v.IdVeterinario, 0);
            var nombre = listaConsultorios.FirstOrDefault(c => c.IdConsultorio == conId)?.Nombre ?? "Sin asignar";
            return new { id = conId, nombre };
        });

        return JsonConvert.SerializeObject(mapping);
    }

    private void CargarViewBagsParaCita(int idCliente)
    {
        var veterinarios = listadoVeterinario()
            .Select(v => new SelectListItem
            {
                Value = v.IdVeterinario.ToString(),
                Text = $"Dr. {v.NombreUsuario} - {v.especialidad}"
            })
            .ToList();

        var clientMascotas = new List<Mascota>();
        try
        {
            string url = $"Cliente/listarMascotas/{idCliente}";
            var clientResponse = GetClient().GetAsync(url).Result;
            if (clientResponse.IsSuccessStatusCode)
            {
                var data = clientResponse.Content.ReadAsStringAsync().Result;
                clientMascotas = JsonConvert.DeserializeObject<List<Mascota>>(data) ?? new List<Mascota>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener mascotas: {ex.Message}");
        }

        ViewBag.veterinarios = veterinarios;
        ViewBag.mascotas = new SelectList(clientMascotas, "IdMascota", "Nombre");
        ViewBag.clientes = new SelectList(listadoCliente(), "IdCliente", "NombreUsuario");
        ViewBag.consultorioPorVet = ObtenerMappingConsultorioJson();
    }




    [HttpGet]
    public async Task<IActionResult> actualizarCita(int id)
    {
        var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
        if (recepcionistaId == null || recepcionistaId == 0)
            return RedirectToAction("Index", "Login");

        // 1. Obtener la cita completa
        var response = await GetClient().GetAsync($"Cita/buscarCita/{id}");
        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "No se encontro la cita.";
            return RedirectToAction("CitasPendientes");
        }

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "No se encontro la cita o la respuesta fue invalida.";
            return RedirectToAction("CitasPendientes");
        }

        var objC = JsonConvert.DeserializeObject<CitaO>(content);
        if (objC == null)
        {
            TempData["Error"] = "No se encontro la cita o la respuesta fue invalida.";
            return RedirectToAction("CitasPendientes");
        }

        // Validar que falten más de 12 horas para la cita
        if (objC.CalendarioCita <= DateTime.Now.AddHours(12))
        {
            TempData["Error"] = "No puede editar una cita cuando faltan menos de 12 horas para la misma.";
            return RedirectToAction("CitasPendientes");
        }

        // 2. Obtener la mascota para saber quién es el dueño (cliente)
        var mascotaResponse = await GetClient().GetAsync($"Cliente/listarMascotasPorId/{objC.IdMascota}");
        if (!mascotaResponse.IsSuccessStatusCode)
        {
            TempData["Error"] = "Error al obtener los datos de la mascota.";
            return RedirectToAction("CitasPendientes");
        }

        var mascotaData = await mascotaResponse.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(mascotaData))
        {
            TempData["Error"] = "No se encontraron datos de la mascota.";
            return RedirectToAction("CitasPendientes");
        }

        var mascota = JsonConvert.DeserializeObject<MascotaConCliente>(mascotaData);
        if (mascota == null)
        {
            TempData["Error"] = "No se encontraron datos de la mascota.";
            return RedirectToAction("CitasPendientes");
        }

        var todasLasMascotasResponse = await GetClient().GetAsync($"Cliente/listarMascotas/{mascota.IdUsuario}");
        List<Mascota> mascotasDelDueño = new List<Mascota>();
        if (todasLasMascotasResponse.IsSuccessStatusCode)
        {
            var todasLasMascotasData = await todasLasMascotasResponse.Content.ReadAsStringAsync();
            mascotasDelDueño = JsonConvert.DeserializeObject<List<Mascota>>(todasLasMascotasData) ?? new List<Mascota>();
        }

        // 4. Preparar los ViewBag
        var veterinarios = listadoVeterinario()
            .Select(v => new SelectListItem
            {
                Value = v.IdVeterinario.ToString(),
                Text = $"Dr. {v.NombreUsuario} - {v.especialidad}"
            })
            .ToList();

        ViewBag.veterinarios = veterinarios;
        ViewBag.mascotas = new SelectList(mascotasDelDueño, "IdMascota", "Nombre");
        ViewBag.clientes = new SelectList(listadoCliente(), "IdCliente", "NombreUsuario");
        ViewBag.consultorioPorVet = ObtenerMappingConsultorioJson();

        return View(objC);
    }





    [HttpPost]
    public async Task<IActionResult> actualizarCitaPost(int id, CitaO obj)
    {
        var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
        if (recepcionistaId == null || recepcionistaId == 0)
            return RedirectToAction("Index", "Login");

        if (!ModelState.IsValid)
        {
            // Mantener el formato de veterinarios con especialidad cuando hay error de validación
            ViewBag.veterinarios = listadoVeterinario()
                .Select(v => new SelectListItem
                {
                    Value = v.IdVeterinario.ToString(),
                    Text = $"Dr. {v.NombreUsuario} - {v.especialidad}"
                })
                .ToList();
            // Cargar mascotas del cliente para mantener el dropdown poblado
            var mascResp = await GetClient().GetAsync($"Cliente/listarMascotasPorId/{obj.IdMascota}");
            if (mascResp.IsSuccessStatusCode)
            {
                var mascData = await mascResp.Content.ReadAsStringAsync();
                var mascCliente = JsonConvert.DeserializeObject<MascotaConCliente>(mascData);
                if (mascCliente != null)
                {
                    var todasResp = await GetClient().GetAsync($"Cliente/listarMascotas/{mascCliente.IdUsuario}");
                    if (todasResp.IsSuccessStatusCode)
                    {
                        var todasData = await todasResp.Content.ReadAsStringAsync();
                        ViewBag.mascotas = new SelectList(
                            JsonConvert.DeserializeObject<List<Mascota>>(todasData) ?? new List<Mascota>(),
                            "IdMascota", "Nombre");
                    }
                }
            }

            ViewBag.clientes = new SelectList(listadoCliente(), "IdCliente", "NombreUsuario");
            ViewBag.consultorioPorVet = ObtenerMappingConsultorioJson();
            return View("actualizarCita", obj);
        }

        // Validar regla 12h antes de enviar a la API (con protecci�n contra NullReferenceException)
        var citaResponse = await GetClient().GetAsync($"Cita/buscarCita/{obj.IdCita}");
        if (citaResponse.StatusCode == System.Net.HttpStatusCode.NoContent || !citaResponse.IsSuccessStatusCode)
        {
            TempData["Error"] = "No se encontr� la cita o la respuesta fue inv�lida.";
            return RedirectToAction("CitasPendientes");
        }

        var contentStr = await citaResponse.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(contentStr))
        {
            TempData["Error"] = "No se encontr� la cita o la respuesta fue inv�lida.";
            return RedirectToAction("CitasPendientes");
        }

        var citaActual = JsonConvert.DeserializeObject<CitaO>(contentStr);
        if (citaActual == null)
        {
            TempData["Error"] = "No se encontr� la cita o la respuesta fue inv�lida.";
            return RedirectToAction("CitasPendientes");
        }

        if (citaActual.CalendarioCita <= DateTime.Now.AddHours(12))
        {
            TempData["Error"] = "No puede editar una cita cuando faltan menos de 12 horas para la misma.";
            return RedirectToAction("CitasPendientes");
        }

        var json = JsonConvert.SerializeObject(obj);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await GetClient().PutAsync($"Cita/actualizaCita?id={obj.IdCita}&clienteId=0", content);

        if (response.IsSuccessStatusCode)
        {
            TempData["Exito"] = "Cita actualizada correctamente.";
            return RedirectToAction("CitasPendientes");
        }

        var errorMsg = await response.Content.ReadAsStringAsync();
        TempData["Error"] = errorMsg;

        ViewBag.veterinarios = listadoVeterinario()
            .Select(v => new SelectListItem
            {
                Value = v.IdVeterinario.ToString(),
                Text = $"Dr. {v.NombreUsuario} - {v.especialidad}"
            })
            .ToList();
        // Cargar mascotas del cliente para mantener el dropdown poblado
        var mascResp2 = await GetClient().GetAsync($"Cliente/listarMascotasPorId/{obj.IdMascota}");
        if (mascResp2.IsSuccessStatusCode)
        {
            var mascData2 = await mascResp2.Content.ReadAsStringAsync();
            var mascCliente2 = JsonConvert.DeserializeObject<MascotaConCliente>(mascData2);
            if (mascCliente2 != null)
            {
                var todasResp2 = await GetClient().GetAsync($"Cliente/listarMascotas/{mascCliente2.IdUsuario}");
                if (todasResp2.IsSuccessStatusCode)
                {
                    var todasData2 = await todasResp2.Content.ReadAsStringAsync();
                    ViewBag.mascotas = new SelectList(
                        JsonConvert.DeserializeObject<List<Mascota>>(todasData2) ?? new List<Mascota>(),
                        "IdMascota", "Nombre");
                }
            }
        }

        ViewBag.clientes = new SelectList(listadoCliente(), "IdCliente", "NombreUsuario");
        ViewBag.consultorioPorVet = ObtenerMappingConsultorioJson();
        return View("actualizarCita", obj);
    }




    // POST: Cita/CancelarCitaCliente (AJAX) - Cambia estado a Cancelada
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarCitaCliente(int id)
    {
        var clienteId = HttpContext.Session.GetInt32("ClienteId");
        if (clienteId == null || clienteId == 0)
            return Json(new { success = false, message = "Sesión no válida." });

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Put, $"Cita/actualizarEstado/{id}?estado=C");
            request.Headers.Add("X-Cliente-Id", clienteId.Value.ToString());
            var response = await GetClient().SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return Json(new { success = true, message = "La cita ha sido cancelada correctamente." });
            }
            else
            {
                var msg = "No se pudo cancelar la cita.";
                try { var obj = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(body, new { success = false, message = "" }); if (obj != null && !string.IsNullOrEmpty(obj.message)) msg = obj.message; } catch { }
                return Json(new { success = false, message = msg });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error: " + ex.Message });
        }
    }

    // POST: Cita/eliminarCita (AJAX)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> eliminarCita(int id)
    {
        var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
        if (recepcionistaId == null || recepcionistaId == 0)
            return Json(new { success = false, message = "Sesión no válida." });
        var response = await GetClient().DeleteAsync($"Cita/eliminarCita/{id}");
        if (response.IsSuccessStatusCode)
        {
            return Json(new { success = true, message = "La cita ha sido cancelada correctamente." });
        }
        else
        {
            return Json(new { success = false, message = "No se pudo cancelar la cita. Es posible que tenga pagos asociados." });
        }
    }



    public Cita ObtenerCitaPorId(long id)
    {
        Cita cita = null;
        try
        {
            string url = $"Cita/buscarCitaFront/{id}";
            HttpResponseMessage response = GetClient().GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                var data = response.Content.ReadAsStringAsync().Result;
                cita = JsonConvert.DeserializeObject<Cita>(data);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener cita por ID: {ex.Message}");
        }
        return cita;
    }


    public IActionResult DetalleCita(long id)
    {
        var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
        var clienteId = HttpContext.Session.GetInt32("ClienteId");
        var veterinarioId = HttpContext.Session.GetInt32("VeterinarioId");
        if ((recepcionistaId == null || recepcionistaId == 0) &&
            (clienteId == null || clienteId == 0) &&
            (veterinarioId == null || veterinarioId == 0))
            return RedirectToAction("Index", "Login");
        Cita cita = ObtenerCitaPorId(id);
        if (cita == null)
        {
            return NotFound();
        }
        return View(cita);
    }


    public IActionResult GenerarDetalleCitaPDF(long id)
    {
        var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
        var clienteId = HttpContext.Session.GetInt32("ClienteId");
        var veterinarioId = HttpContext.Session.GetInt32("VeterinarioId");
        if ((recepcionistaId == null || recepcionistaId == 0) &&
            (clienteId == null || clienteId == 0) &&
            (veterinarioId == null || veterinarioId == 0))
            return RedirectToAction("Index", "Login");

        String hoy = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        return new ViewAsPdf("GenerarDetalleCitaPDF", ObtenerCitaPorId(id))
        {
            FileName = $"DetalleCita-{hoy}.pdf",
            PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
            PageSize = Rotativa.AspNetCore.Options.Size.A5
        };
    }

    // Generar PDF con historial médico (para citas atendidas del cliente)
    public async Task<IActionResult> GenerarHistorialCitaPDF(long id)
    {
        var clienteId = HttpContext.Session.GetInt32("ClienteId");
        if (clienteId == null || clienteId == 0)
        {
            return RedirectToAction("Index", "Login");
        }

        // Obtener las citas del cliente con historial
        CitaCliente? cita = null;
        try
        {
            string url = $"Cliente/listaCitasPorCliente/{clienteId}";
            HttpResponseMessage response = await GetClient().GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var citas = JsonConvert.DeserializeObject<List<CitaCliente>>(data);
                cita = citas?.FirstOrDefault(c => c.ide_cit == id);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener cita: {ex.Message}");
        }

        if (cita == null)
        {
            TempData["Error"] = "No se encontró la cita especificada.";
            return RedirectToAction("ListaCitaPorCliente", "Cliente");
        }

        String hoy = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        return new ViewAsPdf("GenerarHistorialCitaPDF", cita)
        {
            FileName = $"HistorialMedico-{cita.mascota}-{hoy}.pdf",
            PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
            PageSize = Rotativa.AspNetCore.Options.Size.A4
        };
    }


    private async Task<Pago?> ObtenerPagoPendiente()
    {
        try
        {
            var idCliente = HttpContext.Session.GetInt32("ClienteId");
            if (idCliente == null || idCliente == 0)
                return null;

            var response = await GetClient().GetAsync($"Pago/ListarPagosPorCliente/{idCliente}");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var pagos = JsonConvert.DeserializeObject<List<Pago>>(data) ?? new List<Pago>();
                return pagos.FirstOrDefault(p => p.EstadoPago == "Pendiente");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener pago pendiente: {ex.Message}");
        }
        return null;
    }







    // ==================== PROXY ACTIONS (browser → WebApp → API) ====================

    [HttpGet]
    public async Task<IActionResult> semana(string fecha, int ide_cli, int? ide_vet)
    {
        var clienteId = HttpContext.Session.GetInt32("ClienteId") ?? 0;
        if (clienteId == 0) return Json(new List<object>());

        var url = $"Cita/semana?fecha={fecha}&ide_cli={ide_cli}";
        if (ide_vet.HasValue) url += $"&ide_vet={ide_vet}";
        var response = await GetClient().GetAsync(url);
        var data = await response.Content.ReadAsStringAsync();

        return Content(data, "application/json");
    }

    [HttpPost]
    public async Task<IActionResult> intentarHold()
    {
        var clienteId = HttpContext.Session.GetInt32("ClienteId");
        if (clienteId == null || clienteId == 0)
            return Json(new { success = false, mensaje = "Sesión no válida." });

        using var reader = new StreamReader(Request.Body);
        var bodyStr = await reader.ReadToEndAsync();

        try
        {
            var bodyObj = JObject.Parse(bodyStr);
            var fechaHora = bodyObj["fechaHora"]?.Value<DateTime>();
            var vetId = bodyObj["veterinarioId"]?.Value<long>();
            var cliId = bodyObj["clienteId"]?.Value<long>();
            if (fechaHora.HasValue)
            {
                var fecha = fechaHora.Value.Date;
                var diff = ((int)fecha.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                var lunes = fecha.AddDays(-diff);
                _memoryCache.Remove($"slots_{lunes:yyyy-MM-dd}_{cliId}_{vetId}");
            }
        }
        catch { }

        var content = new StringContent(bodyStr, Encoding.UTF8, "application/json");
        var response = await GetClient().PostAsync("Cita/intentarHold", content);
        var data = await response.Content.ReadAsStringAsync();
        return Content(data, "application/json");
    }

    [HttpPost]
    public async Task<IActionResult> liberarHold()
    {
        var clienteId = HttpContext.Session.GetInt32("ClienteId");
        if (clienteId == null || clienteId == 0)
            return Json(new { success = false, mensaje = "Sesión no válida." });

        using var reader = new StreamReader(Request.Body);
        var bodyStr = await reader.ReadToEndAsync();
        var content = new StringContent(bodyStr, Encoding.UTF8, "application/json");
        var response = await GetClient().PostAsync("Cita/liberarHold", content);
        var data = await response.Content.ReadAsStringAsync();
        return Content(data, "application/json");
    }

    [HttpPost]
    public async Task<IActionResult> renovarHold()
    {
        var clienteId = HttpContext.Session.GetInt32("ClienteId");
        if (clienteId == null || clienteId == 0)
            return Json(new { success = false, mensaje = "Sesión no válida." });

        using var reader = new StreamReader(Request.Body);
        var bodyStr = await reader.ReadToEndAsync();
        var content = new StringContent(bodyStr, Encoding.UTF8, "application/json");
        var response = await GetClient().PostAsync("Cita/renovarHold", content);
        var data = await response.Content.ReadAsStringAsync();
        return Content(data, "application/json");
    }

    public IActionResult Index()
    {
        return View();
    }
}