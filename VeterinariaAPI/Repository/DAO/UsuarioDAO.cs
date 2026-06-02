using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using VeterinariaAPI.Models.Usuario;
using VeterinariaAPI.Repository.Interfaces;

namespace VeterinariaAPI.Repository.DAO;

public class UsuarioDAO : IUsuario
{
    private static readonly string _connectionString = DbConfig.Configuration.GetConnectionString("cn") 
        ?? throw new NullReferenceException();

    public string verificarLogin(string uid, string pwd)
    {
        string resultado = "denied";
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            SELECT u.ide_usr, u.pwd_usr, r.nom_rol
            FROM usuario u
            JOIN roles r ON r.ide_rol = u.ide_rol
            WHERE u.cor_usr = @correo", cn);
        cmd.Parameters.AddWithValue("@correo", uid);
        try
        {
            cn.Open();
            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                long ideUsr = dr.GetInt64(0);
                string storedPassword = dr.GetString(1);
                string rol = dr.GetString(2);

                bool isValid = false;
                if (storedPassword.StartsWith("$2"))
                {
                    isValid = BCrypt.Net.BCrypt.Verify(pwd, storedPassword);
                }
                else
                {
                    isValid = (pwd == storedPassword);
                    if (isValid)
                    {
                        MigrarPassword(ideUsr, pwd);
                    }
                }

                if (isValid)
                {
                    resultado = rol;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en login: {ex.Message}");
        }
        return resultado;
    }

    public void MigrarPassword(long ideUsr, string plainPassword)
    {
        try
        {
            string hashed = BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 11);
            using var cn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(
                "UPDATE usuario SET pwd_usr = @pwd WHERE ide_usr = @id", cn);
            cmd.Parameters.AddWithValue("@pwd", hashed);
            cmd.Parameters.AddWithValue("@id", ideUsr);
            cn.Open();
            cmd.ExecuteNonQuery();
            Console.WriteLine($"Usuario {ideUsr} migrado a BCrypt");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"No se pudo migrar contraseña del usuario {ideUsr}: {ex.Message}");
        }
    }

    public string obtenerIdUsuario(string correo)
    {
        string resultado = "denied";
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_obtenerIdUsuario", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@correo", correo);
        try
        {
            cn.Open();
            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                resultado = dr[0].ToString();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener ID de usuario: {ex.Message}");
        }
        return resultado;
    }


    public string ObtenerNombreUsuario(long id)
    {
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_obtenerNombreUsuario", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@id", id);

        try
        {
            cn.Open();
            var nombre = cmd.ExecuteScalar() as string;
            return nombre ?? "Usuario";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener nombre de usuario: {ex.Message}");
            return "Usuario";
        }


    }
}