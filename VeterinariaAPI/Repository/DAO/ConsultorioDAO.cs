using Microsoft.Data.SqlClient;
using VeterinariaAPI.Models;
using VeterinariaAPI.Repository.Interfaces;

namespace VeterinariaAPI.Repository.DAO;

public class ConsultorioDAO : IConsultorio
{
    private static readonly string _connectionString = DbConfig.Configuration.GetConnectionString("cn") 
        ?? throw new NullReferenceException("Cadena de conexión no encontrada.");

    public List<Consultorio> ListarConsultorios()
    {
        var lista = new List<Consultorio>();
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("SELECT ide_con, nom_con FROM consultorio", cn);
        cn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            lista.Add(new Consultorio
            {
                IdConsultorio = Convert.ToInt32(dr["ide_con"]),
                Nombre = dr["nom_con"].ToString() ?? ""
            });
        }
        return lista;
    }
}
