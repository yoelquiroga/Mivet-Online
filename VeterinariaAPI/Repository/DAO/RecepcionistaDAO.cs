using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using VeterinariaAPI.Models.Usuario.Recepcionista;
using VeterinariaAPI.Repository.Interfaces;

namespace VeterinariaAPI.Repository.DAO;

public class RecepcionistaDAO : IRecepcionista 
{
    private static readonly string _connectionString = DbConfig.Configuration.GetConnectionString("cn") 
        ?? throw new NullReferenceException();

    public IEnumerable<Recepcionista> ListarRecepcionistasFront()
    {
        var listaRecepcionistas = new List<Recepcionista>();
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_listarRecepcionistasFront", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            listaRecepcionistas.Add(new Recepcionista()
            {
                IdRecepcionista = Convert.ToInt64(dr["ide_rep"]),
                NombreUsuario = dr["nom_usr"].ToString(),
                ApellidoUsuario = dr["ape_usr"].ToString(),
                FechaNacimiento = Convert.ToDateTime(dr["fna_usr"]),
                TipoDocumento = dr["nom_doc"].ToString(),
                NumeroDocumento = dr["num_doc"].ToString(),
                Rol = dr["nom_rol"].ToString(),
                Sueldo = Convert.ToDecimal(dr["sue_rep"])
            });
        }
        return listaRecepcionistas;
    }

    public IEnumerable<RecepcionistaO> ListarRecepcionistasBack()
    {
        var listaRecepcionistas = new List<RecepcionistaO>();
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_listarRecepcionistasBack", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            listaRecepcionistas.Add(new RecepcionistaO()
            {
                ide_usr = Convert.ToInt64(dr["ide_usr"]),
                ide_rep = Convert.ToInt64(dr["ide_rep"]),
                sue_rep = Convert.ToDecimal(dr["sue_rep"]),
                cor_usr = dr["cor_usr"].ToString(),
                pwd_usr = dr["pwd_usr"].ToString(),
                nom_usr = dr["nom_usr"].ToString(),
                ape_usr = dr["ape_usr"].ToString(),
                fna_usr = Convert.ToDateTime(dr["fna_usr"]),
                num_doc = dr["num_doc"].ToString(),
                ide_doc = Convert.ToInt64(dr["ide_doc"]),
                ide_rol = Convert.ToInt64(dr["ide_rol"])
            });
        }
        return listaRecepcionistas;
    }

    public string AgregarRecepcionista(RecepcionistaO recepcionista) 
    {
        string mensaje = "";
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_agregarRecepcionista", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@cor", recepcionista.cor_usr);
        cmd.Parameters.AddWithValue("@pwd", recepcionista.pwd_usr);
        cmd.Parameters.AddWithValue("@nom", recepcionista.nom_usr);
        cmd.Parameters.AddWithValue("@ape", recepcionista.ape_usr);
        cmd.Parameters.AddWithValue("@ndo", recepcionista.num_doc);
        cmd.Parameters.AddWithValue("@fna", recepcionista.fna_usr);
        cmd.Parameters.AddWithValue("@doc", recepcionista.ide_doc);
        cmd.Parameters.AddWithValue("@sue", recepcionista.sue_rep);

        try
        {
            cn.Open();
            cmd.ExecuteNonQuery();
            mensaje = "Recepcionista guardado correctamente";
        }
        catch (SqlException ex)
        {
            Console.WriteLine(ex.Message);
            mensaje = "Error al guardar recepcionista";
        }
        return mensaje;
    }

    public Recepcionista BuscarRecepcionistaPorID(long id)
    {
        Recepcionista recepcionista = new();
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_buscarRecepcionistaPorId", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@id", id);
        cn.Open();
        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            recepcionista = new Recepcionista()
            {
                IdRecepcionista = Convert.ToInt64(dr["ide_rep"]),
                NombreUsuario = dr["nom_usr"].ToString(),
                ApellidoUsuario = dr["ape_usr"].ToString(),
                FechaNacimiento = Convert.ToDateTime(dr["fna_usr"]),
                TipoDocumento = dr["nom_doc"].ToString(),
                NumeroDocumento = dr["num_doc"].ToString(),
                Rol = dr["nom_rol"].ToString(),
                Sueldo = Convert.ToDecimal(dr["sue_rep"])
            };
        }
        return recepcionista;
    }

    public string ActualizarRecepcionistaPorID(RecepcionistaO recepcionista)
    {
        string mensaje = "";
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_actualizarRecepcionista", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@id", recepcionista.ide_rep);
        cmd.Parameters.AddWithValue("@sue", recepcionista.sue_rep);
        cmd.Parameters.AddWithValue("@cor", recepcionista.cor_usr);
        cmd.Parameters.AddWithValue("@pwd", recepcionista.pwd_usr);
        cmd.Parameters.AddWithValue("@nom", recepcionista.nom_usr);
        cmd.Parameters.AddWithValue("@ape", recepcionista.ape_usr);
        cmd.Parameters.AddWithValue("@ndo", recepcionista.num_doc);
        cmd.Parameters.AddWithValue("@fna", recepcionista.fna_usr);
        cmd.Parameters.AddWithValue("@doc", recepcionista.ide_doc);
        try
        {
            cn.Open();
            cmd.ExecuteNonQuery();
            mensaje = "Recepcionista actualizado correctamente";
        }
        catch (Exception ex)
        {
            mensaje = "Error al actualizar recepcionista. Intente nuevamente.";
        }
        return mensaje;
    }

    public string EliminarRecepcionistaPorID(long id)
    {
        string mensaje = "";
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_eliminarRecepcionista", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@id", id);
        try
        {
            cn.Open();
            cmd.ExecuteNonQuery();
            mensaje = "Recepcionista eliminado correctamente";
        }
        catch (Exception ex)
        {
            mensaje = "Error al eliminar recepcionista. Intente nuevamente.";
        }
        return mensaje;
    }
}