using VeterinariaAPI.Models.Mascota;
using VeterinariaAPI.Models.Usuario.Cliente;
using VeterinariaAPI.Repository.DAO;
using Microsoft.AspNetCore.Mvc;

namespace VeterinariaAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClienteController : ControllerBase
{
    [HttpGet("listaClientes")]
    public async Task<ActionResult<List<Cliente>>> ListaClientes()
    {
        var lista = await Task.Run(() => new ClienteDAO().ListarClientes());
        return Ok(lista);
    }

    [HttpGet("listaClientesBackend")]
    public async Task<ActionResult<List<ClienteO>>> ListaClientesBackend()
    {
        var lista = await Task.Run(() => new ClienteDAO().ListarClientesO());
        foreach (var c in lista) c.pwd_usr = null;
        return Ok(lista);
    }

    [HttpPost("nuevoCliente")]
    public async Task<ActionResult<string>> NuevoCliente(ClienteO cliente)
    {
        if (!string.IsNullOrEmpty(cliente.pwd_usr) && !cliente.pwd_usr.StartsWith("$2"))
        {
            cliente.pwd_usr = BCrypt.Net.BCrypt.HashPassword(cliente.pwd_usr, workFactor: 11);
        }
        var mensaje = await Task.Run(() => new ClienteDAO().GuardarClienteO(cliente));
        return Ok(mensaje);
    }

    [HttpGet("buscarCliente/{id}")]
    public async Task<ActionResult<Cliente>> BuscarCliente(long id)
    {
        var cliente = await Task.Run(() => new ClienteDAO().BuscarClientePorID(id));
        return Ok(cliente);
    }

    [HttpPut("actualizarCliente")]
    public async Task<ActionResult<string>> ActualizarCliente(ClienteO cliente)
    {
        var mensaje = await Task.Run(() => new ClienteDAO().ActualizarCliente(cliente));
        return Ok(mensaje);
    }

    [HttpDelete("eliminarCliente/{id}")]
    public async Task<ActionResult> EliminarCliente(long id)
    {
        await Task.Run(() => new ClienteDAO().EliminarCliente(id));
        return Ok();
    }

    [HttpGet("listaCitasPorCliente/{ide_usr}")]
    public async Task<ActionResult<List<CitaCliente>>> ListaCitasPorCliente(long ide_usr)
    {
        var lista = await Task.Run(() => new ClienteDAO().ListarCitasPorCliente(ide_usr));
        return Ok(lista);
    }


    [HttpPost("agregarMascota/{id_usuario}")]
    public async Task<ActionResult<string>> AgregarMascota(long id_usuario, [FromBody] Mascota mascota)
    {
        if (id_usuario <= 0)
            return BadRequest("ID de usuario inválido.");

        var mensaje = await Task.Run(() => new ClienteDAO().AgregarMascota(mascota, id_usuario));
        return Ok(mensaje);
    }

    [HttpGet("listarMascotas/{ide_usr}")]
    public async Task<ActionResult<List<Mascota>>> ListarMascotas(long ide_usr)
    {
        var lista = await Task.Run(() => new ClienteDAO().ListarMascotasPorCliente(ide_usr));
        return Ok(lista);
    }


    [HttpPut("actualizarMascota")]
    public async Task<ActionResult<string>> ActualizarMascota([FromBody] Mascota mascota)
    {
        if (mascota.IdMascota <= 0)
            return BadRequest("ID de mascota inválido.");

        var mensaje = await Task.Run(() => new ClienteDAO().ActualizarMascota(mascota));
        return Ok(mensaje);
    }


    [HttpDelete("eliminarMascota/{id}")]
    public async Task<ActionResult<string>> EliminarMascota(long id, [FromQuery] bool confirmar = false)
    {
        if (id <= 0)
            return BadRequest("ID de mascota inválido.");

        var mensaje = await Task.Run(() => new ClienteDAO().EliminarMascota(id, confirmar));
        return Ok(mensaje);
    }



    [HttpGet("listarMascotasPorId/{idMascota}")]
    public async Task<ActionResult<MascotaConCliente>> ListarMascotasPorId(long idMascota)
    {
        var resultado = await Task.Run(() => new ClienteDAO().ObtenerMascotaConCliente(idMascota));
        if (resultado == null)
            return NotFound();
        return Ok(resultado);
    }


}