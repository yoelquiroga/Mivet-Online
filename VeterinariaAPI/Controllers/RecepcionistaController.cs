using VeterinariaAPI.Models.Usuario.Recepcionista;
using VeterinariaAPI.Repository.DAO;
using Microsoft.AspNetCore.Mvc;

namespace VeterinariaAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecepcionistaController : ControllerBase
{
    [HttpPost("GuardarRecepcionista")]
    public async Task<ActionResult<string>> GuardarRecepcionista(RecepcionistaO recepcionistaO)
    {
        if (!string.IsNullOrEmpty(recepcionistaO.pwd_usr) && !recepcionistaO.pwd_usr.StartsWith("$2"))
        {
            recepcionistaO.pwd_usr = BCrypt.Net.BCrypt.HashPassword(recepcionistaO.pwd_usr, workFactor: 11);
        }
        var mensaje = await Task.Run(() => new RecepcionistaDAO().AgregarRecepcionista(recepcionistaO));
        return Ok(mensaje);
    }

    [HttpGet("ListarRecepcionistasFront")]
    public async Task<ActionResult<List<Recepcionista>>> ListarRecepcionistasFront()
    {
        var lista = await Task.Run(() => new RecepcionistaDAO().ListarRecepcionistasFront());
        return Ok(lista);
    }

    [HttpGet("ListarRecepcionistasBack")]
    public async Task<ActionResult<List<RecepcionistaO>>> ListarRecepcionistasBack()
    {
        var lista = await Task.Run(() => new RecepcionistaDAO().ListarRecepcionistasBack());
        foreach (var r in lista) r.pwd_usr = null;
        return Ok(lista);
    }

    [HttpGet("BuscarRecepcionistaPorId/{id}")]
    public async Task<ActionResult<Recepcionista>> BuscarRecepcionistaPorId(long id)
    {
        var recepcionista = await Task.Run(() => new RecepcionistaDAO().BuscarRecepcionistaPorID(id));
        return Ok(recepcionista);
    }

    [HttpPut("ActualizarRecepcionista")]
    public async Task<ActionResult<string>> ActualizarRecepcionista(RecepcionistaO recepcionistaO)
    {
        var mensaje = await Task.Run(() => new RecepcionistaDAO().ActualizarRecepcionistaPorID(recepcionistaO));
        return Ok(mensaje);
    }

    [HttpDelete("EliminarRecepcionistaPorId/{id}")]
    public async Task<ActionResult<string>> EliminarRecepcionistaPorId(long id)
    {
        var mensaje = await Task.Run(() => new RecepcionistaDAO().EliminarRecepcionistaPorID(id));
        return Ok(mensaje);
    }
}