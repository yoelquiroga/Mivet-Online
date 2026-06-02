using System.Data;
using System.Data.SqlClient;
using VeterinariaAPI.Models.Usuario.Cliente;
using VeterinariaAPI.Models.Cita;
using VeterinariaAPI.Models.Mascota;
using VeterinariaAPI.Repository.Interfaces;
using Microsoft.Data.SqlClient;

namespace VeterinariaAPI.Repository.DAO;

public class ClienteDAO : ICliente
{
    private static readonly string _connectionString = DbConfig.Configuration.GetConnectionString("cn") 
        ?? throw new NullReferenceException();

    public string GuardarClienteO(ClienteO cliente)
    {
        string mensaje = "";
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_agregarCliente", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@cor", cliente.cor_usr);
        cmd.Parameters.AddWithValue("@pwd", cliente.pwd_usr);
        cmd.Parameters.AddWithValue("@nom", cliente.nom_usr);
        cmd.Parameters.AddWithValue("@ape", cliente.ape_usr);
        cmd.Parameters.AddWithValue("@ndo", cliente.num_doc);
        cmd.Parameters.AddWithValue("@fna", cliente.fna_usr);
        cmd.Parameters.AddWithValue("@doc", cliente.ide_doc);
        try
        {
            cn.Open();
            cmd.ExecuteNonQuery();
            mensaje = "Cliente guardado correctamente";
        }
        catch (SqlException ex)
        {
            Console.WriteLine(ex.Message);
            mensaje = "Error al guardar cliente";
        }
        return mensaje;
    }

    public IEnumerable<Cliente> ListarClientes()
    {
        var listaClientes = new List<Cliente>();
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_listarClientesFront", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            listaClientes.Add(new Cliente()
            {
                IdCliente = Convert.ToInt64(dr[0]),
                NombreUsuario = dr[1].ToString(),
                ApellidoUsuario = dr[2].ToString(),
                FechaNacimiento = Convert.ToDateTime(dr[3]),
                TipoDocumento = dr[4].ToString(),
                NumeroDocumento = dr[5].ToString(),
                Rol = dr[6].ToString(),
            });
        }
        return listaClientes;
    }

    public IEnumerable<ClienteO> ListarClientesO()
    {
        var listaClientes = new List<ClienteO>();
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_listarClientesBack", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            listaClientes.Add(new ClienteO()
            {
                ide_usr = Convert.ToInt64(dr[0]),
                ide_cli = Convert.ToInt64(dr[1]),
                cor_usr = dr[2].ToString(),
                pwd_usr = dr[3].ToString(),
                nom_usr = dr[4].ToString(),
                ape_usr = dr[5].ToString(),
                fna_usr = Convert.ToDateTime(dr[6]),
                num_doc = dr[7].ToString(),
                ide_doc = Convert.ToInt64(dr[8]),
                ide_rol = Convert.ToInt64(dr[9])
            });
        }
        return listaClientes;
    }

    public Cliente BuscarClientePorID(long id)
    {
        Cliente cliente = new();
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_buscarCliente", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@id", id);
        cn.Open();
        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            cliente = new Cliente()
            {
                IdCliente = Convert.ToInt64(dr[0]),
                NombreUsuario = dr[1].ToString(),
                ApellidoUsuario = dr[2].ToString(),
                FechaNacimiento = Convert.ToDateTime(dr[3]),
                TipoDocumento = dr[4].ToString(),
                NumeroDocumento = dr[5].ToString(),
                Rol = dr[6].ToString(),
            };
        }
        return cliente;
    }

    public string ActualizarCliente(ClienteO c)
    {
        string mensaje = "";
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_actualizarCliente", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@id", c.ide_cli);
        cmd.Parameters.AddWithValue("@cor", c.cor_usr);
        cmd.Parameters.AddWithValue("@pwd", c.pwd_usr);
        cmd.Parameters.AddWithValue("@nom", c.nom_usr);
        cmd.Parameters.AddWithValue("@ape", c.ape_usr);
        cmd.Parameters.AddWithValue("@ndo", c.num_doc);
        cmd.Parameters.AddWithValue("@fna", c.fna_usr);
        cmd.Parameters.AddWithValue("@doc", c.ide_doc);
        try
        {
            cn.Open();
            cmd.ExecuteNonQuery();
            mensaje = "Cliente actualizado correctamente";
        }
        catch (Exception ex)
        {
            mensaje = "Error al actualizar cliente. Intente nuevamente.";
        }
        return mensaje;
    }

    public string EliminarCliente(long id)
    {
        string mensaje = "";
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_eliminarCliente", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@id", id);
        try
        {
            cn.Open();
            cmd.ExecuteNonQuery();
            mensaje = "Cliente eliminado correctamente";
        }
        catch (Exception ex)
        {
            mensaje = "Error al eliminar cliente. Intente nuevamente.";
        }
        return mensaje;
    }

    public IEnumerable<CitaCliente> ListarCitasPorCliente(long ide_usr)
    {
        var listaCitaCliente = new List<CitaCliente>();
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_listarCitasPorClienteConHistorial", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@ide_usr", ide_usr);
        cn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            listaCitaCliente.Add(new CitaCliente()
            {
                ide_cit = Convert.ToInt64(dr["ide_cit"]),
                cal_cit = Convert.ToDateTime(dr["cal_cit"]),
                con_cit = Convert.ToInt32(dr["con_cit"]),
                veterinario = dr["veterinario"].ToString(),
                especialidad = dr["especialidad"].ToString(),
                mascota = dr["mascota"].ToString(),
                especie = dr["especie"].ToString(),
                raza = dr["raza"].ToString(),
                mon_pag = Convert.ToDecimal(dr["mon_pag"]),
                metodo_pago = dr["metodo_pago"].ToString(),
                est_cit = dr["est_cit"].ToString() ?? "P",
                // Historial médico (puede ser NULL)
                sintomas = dr["sintomas"] == DBNull.Value ? null : dr["sintomas"].ToString(),
                diagnostico = dr["diagnostico"] == DBNull.Value ? null : dr["diagnostico"].ToString(),
                tratamiento = dr["tratamiento"] == DBNull.Value ? null : dr["tratamiento"].ToString(),
                medicamentos = dr["medicamentos"] == DBNull.Value ? null : dr["medicamentos"].ToString(),
                observaciones = dr["observaciones"] == DBNull.Value ? null : dr["observaciones"].ToString(),
                fecha_atencion = dr["fecha_atencion"] == DBNull.Value ? null : Convert.ToDateTime(dr["fecha_atencion"])
            });
        }
        return listaCitaCliente;
    }

    // NUEVOS MÉTODOS PARA MASCOTAS 

    public string AgregarMascota(Mascota mascota, long id_usuario)
    {
        string mensaje = "";
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_agregarMascota", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@nombre", mascota.Nombre);
        cmd.Parameters.AddWithValue("@especie", mascota.Especie);
        cmd.Parameters.AddWithValue("@raza", mascota.Raza);
        cmd.Parameters.AddWithValue("@fecha_nac", mascota.FechaNacimiento);
        cmd.Parameters.AddWithValue("@id_usuario", id_usuario); // Pasar el ID del usuario directamente
        try
        {
            cn.Open();
            cmd.ExecuteNonQuery();
            mensaje = "Mascota registrada correctamente";
        }
        catch (Exception ex)
        {
            mensaje = "Error al registrar mascota. Intente nuevamente.";
        }
        return mensaje;
    }

    public IEnumerable<Mascota> ListarMascotasPorCliente(long id_usuario)
    {
        var listaMascotas = new List<Mascota>();
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_listarMascotasPorCliente", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@id_usuario", id_usuario);
        cn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            listaMascotas.Add(new Mascota()
            {
                IdMascota = Convert.ToInt64(dr["ide_mas"]),
                Nombre = dr["Nombre"].ToString(),
                Especie = dr["Especie"].ToString(),
                Raza = dr["Raza"].ToString(),
                FechaNacimiento = Convert.ToDateTime(dr["FechaNacimiento"]),
              
            
            });
        }
        return listaMascotas;
    }



    public string ActualizarMascota(Mascota mascota)
    {
        string mensaje = "";
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_actualizarMascota", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@id_mascota", mascota.IdMascota);
        cmd.Parameters.AddWithValue("@nombre", mascota.Nombre);
        cmd.Parameters.AddWithValue("@especie", mascota.Especie);
        cmd.Parameters.AddWithValue("@raza", mascota.Raza);
        cmd.Parameters.AddWithValue("@fecha_nac", mascota.FechaNacimiento);
        try
        {
            cn.Open();
            int filasAfectadas = cmd.ExecuteNonQuery();
            mensaje = filasAfectadas > 0
                ? "Mascota actualizada correctamente"
                : "No se encontró la mascota para actualizar";
        }
        catch (Exception ex)
        {
            mensaje = "Error al actualizar la mascota. Intente nuevamente.";
        }
        return mensaje;
    }

    public string EliminarMascota(long id_mascota, bool confirmar = false)
    {
        string mensaje = "";
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_eliminarMascota", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@id_mascota", id_mascota);
        cmd.Parameters.AddWithValue("@confirmar", confirmar);
        cn.Open();
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            mensaje = reader["Mensaje"].ToString();
        }
        return mensaje;
    }





    public MascotaConCliente ObtenerMascotaConCliente(long idMascota)
    {
        MascotaConCliente resultado = null;
        using var cn = new SqlConnection(_connectionString);
        string query = @"
        SELECT m.ide_mas, c.ide_usr 
        FROM mascota m
        JOIN cliente c ON m.ide_cli = c.ide_cli
        WHERE m.ide_mas = @idMascota";

        using var cmd = new SqlCommand(query, cn);
        cmd.Parameters.AddWithValue("@idMascota", idMascota);
        cn.Open();
        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            resultado = new MascotaConCliente
            {
                IdMascota = Convert.ToInt64(dr["ide_mas"]),
                IdUsuario = Convert.ToInt64(dr["ide_usr"])
            };
        }
        return resultado;
    }



}