using VeterinariaAPI.Models.Usuario.Veterinario;
using VeterinariaAPI.Repository.DAO;
using Microsoft.AspNetCore.Mvc;

namespace VeterinariaAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VeterinarioController : ControllerBase
{
    [HttpGet("consultoriosPorVet")]
    public async Task<ActionResult<Dictionary<long, int>>> ConsultoriosPorVet()
    {
        var result = await Task.Run(() => new VeterinarioDAO().ObtenerConsultoriosPorVet());
        return Ok(result);
    }

    [HttpGet("listaVeterinarios")]
    public async Task<ActionResult<List<Veterinario>>> ListaVeterinarios()
    {
        var lista = await Task.Run(() => new VeterinarioDAO().ListarVeterinariosFront());
        return Ok(lista);
    }

    [HttpGet("listaVeterinariosBackend")]
    public async Task<ActionResult<List<VeterinarioO>>> ListaVeterinariosBackend()
    {
        var lista = await Task.Run(() => new VeterinarioDAO().ListarVeterinariosBack());
        foreach (var v in lista) v.pwd_usr = null;
        return Ok(lista);
    }

    [HttpPost("nuevoVeterinario")]
    public async Task<ActionResult<string>> NuevoVeterinario(VeterinarioO veterinarioO)
    {
        if (!string.IsNullOrEmpty(veterinarioO.pwd_usr) && !veterinarioO.pwd_usr.StartsWith("$2"))
        {
            veterinarioO.pwd_usr = BCrypt.Net.BCrypt.HashPassword(veterinarioO.pwd_usr, workFactor: 11);
        }
        var mensaje = await Task.Run(() => new VeterinarioDAO().AgregarVeterinario(veterinarioO));
        return Ok(mensaje);
    }

    [HttpGet("buscarVeterinario/{id}")]
    public async Task<ActionResult<Veterinario>> BuscarVeterinarioPorId(long id)
    {
        var veterinario = await Task.Run(() => new VeterinarioDAO().BuscarVeterinarioPorID(id));
        return Ok(veterinario);
    }

    [HttpPut("actualizarVeterinario")]
    public async Task<ActionResult<string>> ActualizarVeterinario(VeterinarioO veterinarioO)
    {
        var mensaje = await Task.Run(() => new VeterinarioDAO().ActualizarVeterinarioPorID(veterinarioO));
        return Ok(mensaje);
    }

    [HttpDelete("eliminarVeterinario/{id}")]
    public async Task<ActionResult> EliminarVeterinario(long id)
    {
        await Task.Run(() => new VeterinarioDAO().EliminarVeterinarioPorID(id));
        return Ok();
    }

    [HttpGet("listarEspecialidad")]
    public async Task<ActionResult<List<Especialidad>>> ListarEspecialidad()
    {
        var lista = await Task.Run(() => new EspecialidadDAO().listarEspecialidad());
        return Ok(lista);
    }

    [HttpGet("listaCitasPorVeterinario/{ide_usr}")]
    public async Task<ActionResult<List<CitaVeterinario>>> ListaCitasPorVeterinario(long ide_usr)
    {
        var lista = await Task.Run(() => new VeterinarioDAO().ListarCitasPorVeterinario(ide_usr));
        return Ok(lista);
    }

    [HttpGet("estadisticasVeterinario/{ide_usr}")]
    public async Task<ActionResult<VeterinarioStats>> ObtenerEstadisticasVeterinario(long ide_usr)
    {
        var stats = await Task.Run(() => new VeterinarioDAO().ObtenerEstadisticasVeterinario(ide_usr));
        return Ok(stats);
    }

    [HttpGet("listaMascotasPorVeterinario/{ide_usr}")]
    public async Task<ActionResult<List<MascotaPorVeterinario>>> ListaMascotasPorVeterinario(long ide_usr)
    {
        var lista = await Task.Run(() => new VeterinarioDAO().ListarMascotasPorVeterinario(ide_usr));
        return Ok(lista);
    }

    [HttpGet("ingresosRealizados/{ide_usr}")]
    public async Task<ActionResult<decimal>> IngresosRealizados(long ide_usr)
    {
        var total = await Task.Run(() => new VeterinarioDAO().ObtenerIngresosRealizados(ide_usr));
        return Ok(total);
    }

    //  Listar mascotas atendidas con historial médico
    [HttpGet("listaMascotasAtendidasConHistorial/{ide_usr}")]
    public async Task<ActionResult<List<MascotaAtendida>>> ListaMascotasAtendidasConHistorial(long ide_usr)
    {
        var lista = await Task.Run(() => new VeterinarioDAO().ListarMascotasAtendidasConHistorial(ide_usr));
        return Ok(lista);
    }
}