using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using VeterinariaAPI.Models.Usuario;
using VeterinariaAPI.Repository.Interfaces;

namespace VeterinariaAPI.Repository.DAO;

public class UserDocDAO : IUserDoc
{
    private static readonly string _connectionString = DbConfig.Configuration.GetConnectionString("cn") 
        ?? throw new NullReferenceException();

    public IEnumerable<UserDoc> ListarTiposDeDocumento()
    {
        var listaDocumentos = new List<UserDoc>();
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_listarDocumentos", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            listaDocumentos.Add(new UserDoc
            {
                ide_doc = Convert.ToInt64(dr[0]),
                nom_doc = dr[1].ToString(),
            });
        }
        return listaDocumentos;
    }
}