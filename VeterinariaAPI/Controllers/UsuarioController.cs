using VeterinariaAPI.Models.Usuario;
using VeterinariaAPI.Repository.DAO;
using Microsoft.AspNetCore.Mvc;

namespace VeterinariaAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsuarioController : ControllerBase
{
    [HttpGet("VerificarLogin")]
    public async Task<ActionResult<string>> VerificarLogin(string uid, string pwd)
    {
        var resultado = await Task.Run(() => new UsuarioDAO().verificarLogin(uid, pwd));
        return Ok(resultado);
    }

    [HttpGet("ObtenerIdUsuario")] 
    public async Task<ActionResult<string>> ObtenerIdUsuario(string correo)
    {
        var resultado = await Task.Run(() => new UsuarioDAO().obtenerIdUsuario(correo));
        return Ok(resultado);
    }

    [HttpGet("ObtenerNombreUsuario")]
    public async Task<ActionResult<string>> ObtenerNombreUsuario(long id)
    {
        var nombre = await Task.Run(() => new UsuarioDAO().ObtenerNombreUsuario(id));
        return Ok(nombre);
    }

    [HttpGet("ListarDocumentos")]
    public async Task<ActionResult<List<UserDoc>>> ListarDocumentos()
    {
        var resultado = await Task.Run(() => new UserDocDAO().ListarTiposDeDocumento());
        return Ok(resultado);
    }
}