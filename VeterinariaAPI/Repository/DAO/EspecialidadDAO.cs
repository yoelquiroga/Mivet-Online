using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using VeterinariaAPI.Models.Usuario.Veterinario; 
using VeterinariaAPI.Repository.Interfaces;

namespace VeterinariaAPI.Repository.DAO;

public class EspecialidadDAO : IEspecialidad
{
    private static readonly string _connectionString = DbConfig.Configuration.GetConnectionString("cn") 
        ?? throw new NullReferenceException();

    public IEnumerable<Especialidad> listarEspecialidad()
    {
        List<Especialidad> listaEspecialidad = new List<Especialidad>();
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_listarEspecialidad", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            listaEspecialidad.Add(new Especialidad
            {
                ide_esp = Convert.ToInt64(dr[0]),
                nom_esp = dr[1].ToString(),
            });
        }
        return listaEspecialidad;
    }
}