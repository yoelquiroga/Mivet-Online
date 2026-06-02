using Microsoft.Extensions.Caching.Memory;
using VeterinariaWebApp.Models.Pago;
using VeterinariaWebApp.Models.Usuario; 
using VeterinariaWebApp.Models.Usuario.Cliente; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Rotativa.AspNetCore;
using System.Net.Http;
using System.Text;

namespace VeterinariaWebApp.Controllers;

public class PagoController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;

    public PagoController(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache)
    {
        _httpClientFactory = httpClientFactory;
        _cache = memoryCache;
    }

    private HttpClient GetClient() => _httpClientFactory.CreateClient("ClinicaAPI");

    
    public List<Pago> listadoPagosGeneral()
    {
        List<Pago> aPagos = new List<Pago>();
        try
        {
            string url = $"Pago/ListarPagosGeneral";
            HttpResponseMessage response = GetClient().GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                var data = response.Content.ReadAsStringAsync().Result;
                aPagos = JsonConvert.DeserializeObject<List<Pago>>(data) ?? new List<Pago>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener pagos generales: {ex.Message}");
        }
        return aPagos;
    }

    public Pago ObtenerPagoPorId(long id)
    {
        Pago pago = new Pago();
        try
        {
            string url = $"Pago/ObtenerPagoPorIdFront/{id}";
            HttpResponseMessage response = GetClient().GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                var data = response.Content.ReadAsStringAsync().Result;
                pago = JsonConvert.DeserializeObject<Pago>(data) ?? new Pago();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener pago por ID: {ex.Message}");
        }
        return pago;
    }


    public List<Pago> listadoPagosPorCliente(long token)
    {
        List<Pago> aPagos = new List<Pago>();
        if (token == 0)
            return aPagos;

        try
        {
            string url = $"Pago/ListarPagosPorCliente/{token}";
            HttpResponseMessage response = GetClient().GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                var data = response.Content.ReadAsStringAsync().Result;
                aPagos = JsonConvert.DeserializeObject<List<Pago>>(data) ?? new List<Pago>();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
       
                Console.WriteLine("Token inválido o usuario no autenticado.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener pagos por cliente: {ex.Message}");
        }
        return aPagos;
    }


    public async Task<IActionResult> PagosPendientes()
    {
        var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
        if (recepcionistaId == null || recepcionistaId == 0)
            return RedirectToAction("Index", "Login");
        var pagos = await ListarPagosPendientes();
        return View(pagos);
    }



    //Listar PAGOS PENDIENTES
    public async Task<List<Pago>> ListarPagosPendientes()
    {
        List<Pago> pagos = new List<Pago>();
        try
        {
            string url = $"Pago/ListarPagosPendientes";
            HttpResponseMessage response = await GetClient().GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                pagos = JsonConvert.DeserializeObject<List<Pago>>(data) ?? new List<Pago>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener pagos pendientes: {ex.Message}");
        }
        return pagos;
    }



    // Listar PAGOS REALIZADOS 
    public async Task<List<Pago>> ListarPagosRealizados()
    {
        List<Pago> pagos = new List<Pago>();
        try
        {
            string url = $"Pago/ListarPagosRealizados";
            HttpResponseMessage response = await GetClient().GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                pagos = JsonConvert.DeserializeObject<List<Pago>>(data) ?? new List<Pago>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener pagos realizados: {ex.Message}");
        }
        return pagos;
    }


    public async Task<IActionResult> PagosRealizados()
    {
        var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
        if (recepcionistaId == null || recepcionistaId == 0)
            return RedirectToAction("Index", "Login");
        var pagos = await ListarPagosRealizados();
        return View(pagos);
    }








    public List<UserDoc> listadoTipoDocumentos()
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


    public List<Cliente> listadoCliente()
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


    public List<PayOpts> ListadoPayOpts()
    {
        List<PayOpts> aPayOpts = new List<PayOpts>();
        try
        {
            string url = $"Pago/ObtenerTiposDePago";
            HttpResponseMessage response = GetClient().GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                var data = response.Content.ReadAsStringAsync().Result;
                aPayOpts = JsonConvert.DeserializeObject<List<PayOpts>>(data) ?? new List<PayOpts>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener tipos de pago: {ex.Message}");
        }
        return aPayOpts;
    }


    public IActionResult PagosRecepcionista()
    {
        var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
        if (recepcionistaId == null || recepcionistaId == 0)
            return RedirectToAction("Index", "Login");
        var pagos = listadoPagosGeneral(); 
        return View(pagos); 
    }

    // Acción para mostrar los pagos del cliente logueado
    public IActionResult PagosCliente()
    {
        int? clienteId = HttpContext.Session.GetInt32("ClienteId");
        if (clienteId == null || clienteId == 0)
            return RedirectToAction("Index", "Login");
        long token = long.Parse(HttpContext.Session.GetString("token"));
        var pagos = listadoPagosPorCliente(token);
        return View(pagos);
    }


    public IActionResult DetallePago(long id, string? origen = null)
    {
        var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
        var clienteId = HttpContext.Session.GetInt32("ClienteId");
        if ((recepcionistaId == null || recepcionistaId == 0) &&
            (clienteId == null || clienteId == 0))
            return RedirectToAction("Index", "Login");
        if (id == 0)
        {
            return Content("Error al intentar obtener el pago");
        }
        ViewBag.Origen = origen;
        return View(ObtenerPagoPorId(id));
    }

    
    public IActionResult DetallePagoPDF(long id)
    {
        var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
        var clienteId = HttpContext.Session.GetInt32("ClienteId");
        if ((recepcionistaId == null || recepcionistaId == 0) &&
            (clienteId == null || clienteId == 0))
            return RedirectToAction("Index", "Login");
        String hoy = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        return new ViewAsPdf("DetallePagoPDF", ObtenerPagoPorId(id))
        {
            FileName = $"DetallePago-{hoy}.pdf",
            PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
            PageSize = Rotativa.AspNetCore.Options.Size.A5
        };
    }

    
    public async Task<IActionResult> Crear()
    {
        int? clienteId = HttpContext.Session.GetInt32("ClienteId");
        if (clienteId == null || clienteId == 0)
            return RedirectToAction("Index", "Login");

        // Si ya tiene un pago pendiente, redirigir a nueva cita con ese pago
        var misPagos = listadoPagosPorCliente(clienteId.Value);
        var pagoPendiente = misPagos.FirstOrDefault(p => p.EstadoPago == "Pendiente");
        if (pagoPendiente != null)
        {
            return RedirectToAction("nuevaCita", "Cita", new { PagoId = pagoPendiente.IdPago });
        }

        ViewBag.tipoPagos = new SelectList(ListadoPayOpts(), "ide_pay", "nom_pay");
        return View(new PagoO());
    }

    [HttpPost]
    public async Task<IActionResult> Crear(PagoO obj)
    {
        var clienteId = HttpContext.Session.GetInt32("ClienteId");
        if (clienteId == null || clienteId == 0)
            return RedirectToAction("Index", "Login");
        obj.HoraPago = DateTime.Now;
        obj.IdCliente = clienteId.Value;
        obj.MontoPago = 30.00m; 

        if (!ModelState.IsValid)
        {
            ViewBag.tipoPagos = new SelectList(ListadoPayOpts(), "ide_pay", "nom_pay");
            ViewBag.clientes = new SelectList(listadoCliente(), "IdCliente", "NombreUsuario");
            return View(obj);
        }

        var json = JsonConvert.SerializeObject(obj);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

      
        var idUsuario = obj.IdCliente; 
        var responseC = await GetClient().PostAsync($"Pago/AgregarPago/{idUsuario}", content);

        if (responseC.IsSuccessStatusCode)
        {
            ViewBag.mensaje = "Pago registrado correctamente..!!!";

         
            var idPagoStr = await responseC.Content.ReadAsStringAsync();
            long IdPago = long.Parse(idPagoStr); 

            return RedirectToAction("nuevaCita", "Cita", new { PagoId = IdPago });
        }
        else
        {
           
            ViewBag.mensaje = $"Error al registrar el pago. Código: {responseC.StatusCode}";
            ViewBag.tipoPagos = new SelectList(ListadoPayOpts(), "ide_pay", "nom_pay");
            ViewBag.clientes = new SelectList(listadoCliente(), "IdCliente", "NombreUsuario");
            return View(obj);
        }
    }

    public async Task<IActionResult> ConfirmarPago(long id)
    {
        var recepcionistaId = HttpContext.Session.GetInt32("RecepcionistaId");
        if (recepcionistaId == null || recepcionistaId == 0)
            return RedirectToAction("Index", "Login");
        try
        {
            string url = $"Pago/ConfirmarPago/{id}";
            var response = await GetClient().PutAsync(url, null);
            if (response.IsSuccessStatusCode)
            {
                TempData["Mensaje"] = "Pago confirmado correctamente.";
            }
            else
            {
                TempData["Error"] = "Error al confirmar el pago.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error interno: {ex.Message}";
        }
        return RedirectToAction(nameof(PagosPendientes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EliminarPago(long id)
    {
        try
        {
            long userId = HttpContext.Session.GetInt32("ClienteId") ?? 0;
            if (userId == 0)
            {
                TempData["Error"] = "Sesión no válida.";
                return RedirectToAction("Index", "Login");
            }

            string url = $"Pago/EliminarPago/{id}?userId={userId}";
            var response = GetClient().DeleteAsync(url).Result;

            if (response.IsSuccessStatusCode)
            {
                TempData["Mensaje"] = "El pago ha sido eliminado correctamente.";
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                TempData["Error"] = "No se puede eliminar un pago que ya está asociado a una cita.";
            }
            else
            {
                var errMsg = response.Content.ReadAsStringAsync().Result;
                TempData["Error"] = errMsg;
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error interno: {ex.Message}";
        }

        return RedirectToAction(nameof(PagosCliente));
    }




    // ==================== PROXY ACTIONS (browser → WebApp → API) ====================

    [HttpGet]
    public async Task<IActionResult> VerificarAutorizacion(long id)
    {
        var cacheKey = $"VerifAuto_{id}";
        if (_cache.TryGetValue(cacheKey, out string? cachedData))
        {
            return new ContentResult
            {
                Content = cachedData ?? "false",
                ContentType = "application/json",
                StatusCode = 200
            };
        }

        var response = await GetClient().GetAsync($"Pago/VerificarAutorizacion/{id}");
        var data = await response.Content.ReadAsStringAsync();

        _cache.Set(cacheKey, data, TimeSpan.FromSeconds(5));

        return new ContentResult
        {
            Content = data,
            ContentType = "application/json",
            StatusCode = (int)response.StatusCode
        };
    }

    [HttpPost]
    public async Task<IActionResult> AgregarPago(int id)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await GetClient().PostAsync($"Pago/AgregarPago/{id}", content);
        var data = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            Content = data,
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
            StatusCode = (int)response.StatusCode
        };
    }

    [HttpGet]
    public async Task<IActionResult> ListarPagosPorClienteProxy(int id)
    {
        var response = await GetClient().GetAsync($"Pago/ListarPagosPorCliente/{id}");
        var data = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            Content = data,
            ContentType = "application/json",
            StatusCode = (int)response.StatusCode
        };
    }

    public IActionResult Index()
    {
        return View();
    }
}