using Microsoft.AspNetCore.Mvc;
using VeterinariaAPI.Models;
using VeterinariaAPI.Repository.DAO;

namespace VeterinariaAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConsultorioController : ControllerBase
{
    [HttpGet("listar")]
    public ActionResult<List<Consultorio>> ListarConsultorios()
    {
        return Ok(new ConsultorioDAO().ListarConsultorios());
    }
}
