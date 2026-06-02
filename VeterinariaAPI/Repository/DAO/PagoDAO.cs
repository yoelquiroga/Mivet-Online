using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using VeterinariaAPI.Models.Pago;
using VeterinariaAPI.Repository.Interfaces;

namespace VeterinariaAPI.Repository.DAO;

public class PagoDAO : IPago
{
    private static readonly string _connectionString = DbConfig.Configuration.GetConnectionString("cn") 
        ?? throw new NullReferenceException();

    private void CargarAutPag(SqlConnection cn, List<Pago> pagos)
    {
        if (pagos.Count == 0) return;
        var ids = pagos.Select(p => p.IdPago).ToList();
        var @params = ids.Select((_, i) => "@p" + i);
        var cmd = new SqlCommand("SELECT ide_pag, aut_pag FROM pago WHERE ide_pag IN (" + string.Join(",", @params) + ")", cn);
        for (int i = 0; i < ids.Count; i++)
            cmd.Parameters.AddWithValue("@p" + i, ids[i]);
        using var dr = cmd.ExecuteReader();
        var dict = new Dictionary<long, bool?>();
        while (dr.Read())
        {
            dict[Convert.ToInt64(dr[0])] = dr[1] == DBNull.Value ? null : (bool?)Convert.ToBoolean(dr[1]);
        }
        foreach (var p in pagos)
        {
            if (dict.TryGetValue(p.IdPago, out var ap))
                p.AutPag = ap;
        }
    }

    public IEnumerable<Pago> ListarPagos(int pagina = 1, int tamanoPagina = 50)
    {
        List<Pago> pagos = new List<Pago>();
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_listarPagos", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@Pagina", pagina);
        cmd.Parameters.AddWithValue("@TamanoPagina", tamanoPagina);
        cn.Open();
        using (var dr = cmd.ExecuteReader())
        {
            while (dr.Read())
            {
                pagos.Add(new Pago()
                {
                    IdPago = Convert.ToInt64(dr[0]),
                    HoraPago = Convert.ToDateTime(dr[1]),
                    MontoPago = Convert.ToDecimal(dr[2]),
                    TipoPago = dr[3].ToString(),
                    CorreoCliente = dr[4].ToString(), 
                    NombreCliente = dr[5].ToString()  
                });
            }
        }
        CargarAutPag(cn, pagos);
        return pagos;
    }



    // Listar PAGOS PENDIENTES para Recep
    public IEnumerable<Pago> ListarPagosPendientes(int pagina = 1, int tamanoPagina = 50)
    {
        List<Pago> pagos = new List<Pago>();
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_listarPagosPendientes", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@Pagina", pagina);
        cmd.Parameters.AddWithValue("@TamanoPagina", tamanoPagina);
        cn.Open();
        using (var dr = cmd.ExecuteReader())
        {
            while (dr.Read())
            {
                pagos.Add(new Pago()
                {
                    IdPago = Convert.ToInt64(dr["ide_pag"]),
                    HoraPago = Convert.ToDateTime(dr["hor_pag"]),
                    MontoPago = Convert.ToDecimal(dr["mon_pag"]),
                    TipoPago = dr["nom_pay"].ToString(),
                    CorreoCliente = dr["cor_usr"].ToString(),
                    NombreCliente = dr["nombre_completo"].ToString(),
                    EstadoPago = "Pendiente" 
                });
            }
        }
        CargarAutPag(cn, pagos);
        return pagos;
    }

    //  Listar PAGOS REALIZADOS Recep
    public IEnumerable<Pago> ListarPagosRealizados(int pagina = 1, int tamanoPagina = 50)
    {
        List<Pago> pagos = new List<Pago>();
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_listarPagosRealizados", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@Pagina", pagina);
        cmd.Parameters.AddWithValue("@TamanoPagina", tamanoPagina);
        cn.Open();
        using (var dr = cmd.ExecuteReader())
        {
            while (dr.Read())
            {
                pagos.Add(new Pago()
                {
                    IdPago = Convert.ToInt64(dr["ide_pag"]),
                    HoraPago = Convert.ToDateTime(dr["hor_pag"]),
                    MontoPago = Convert.ToDecimal(dr["mon_pag"]),
                    TipoPago = dr["nom_pay"].ToString(),
                    CorreoCliente = dr["cor_usr"].ToString(),
                    NombreCliente = dr["nombre_completo"].ToString(),
                    EstadoPago = "Realizado" 
                });
            }
        }
        CargarAutPag(cn, pagos);
        return pagos;
    }



    public IEnumerable<Pago> ListarPagosPorCliente(long id)
    {
        List<Pago> pagos = new List<Pago>();
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_listarPagosPorCliente", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@ide_usr", id);
        cn.Open();
        using (var dr = cmd.ExecuteReader())
        {
            while (dr.Read())
            {
                pagos.Add(new Pago()
                {
                    IdPago = Convert.ToInt64(dr[0]),
                    HoraPago = Convert.ToDateTime(dr[1]),
                    MontoPago = Convert.ToDecimal(dr[2]),
                    TipoPago = dr[3].ToString(),
                    CorreoCliente = dr[4].ToString(),
                    NombreCliente = dr[5].ToString(),
                  
                    EstadoPago = dr["EstadoPago"].ToString()
                });
            }
        }
        CargarAutPag(cn, pagos);
        return pagos;
    }

    public long AgregarPago(PagoO pago, long token)
    {
        long idGenerado = 0;
        using var cn = new SqlConnection(_connectionString);
        cn.Open();
        using var tran = cn.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            using var cmdCount = new SqlCommand(
                "SELECT COUNT(*) FROM pago p JOIN cliente c ON c.ide_cli = p.ide_cli " +
                "WHERE c.ide_usr = @usr AND NOT EXISTS (SELECT 1 FROM cita WHERE ide_pag = p.ide_pag)", cn, tran);
            cmdCount.Parameters.AddWithValue("@usr", token);
            var count = (int)cmdCount.ExecuteScalar();
            if (count >= 3)
            {
                tran.Rollback();
                throw new InvalidOperationException("Ha alcanzado el límite de 3 pagos pendientes. Complete o cancele una cita primero.");
            }

            using var cmd = new SqlCommand("sp_agregarPago", cn, tran);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@hora", pago.HoraPago);
            cmd.Parameters.AddWithValue("@monto", pago.MontoPago);
            cmd.Parameters.AddWithValue("@tipopago", pago.TipoPago);
            cmd.Parameters.AddWithValue("@usuario", token);
            var result = cmd.ExecuteScalar();
            if (result != null) idGenerado = Convert.ToInt64(result);

            if (idGenerado > 0)
            {
                if (pago.AutPag == false)
                {
                    using var cmdAut = new SqlCommand("UPDATE pago SET aut_pag = NULL WHERE ide_pag = @id", cn, tran);
                    cmdAut.Parameters.AddWithValue("@id", idGenerado);
                    cmdAut.ExecuteNonQuery();
                }
                else
                {
                    using var cmdAut = new SqlCommand("UPDATE pago SET aut_pag = 1 WHERE ide_pag = @id", cn, tran);
                    cmdAut.Parameters.AddWithValue("@id", idGenerado);
                    cmdAut.ExecuteNonQuery();
                }
            }

            tran.Commit();
        }
        catch
        {
            tran.Rollback();
            throw;
        }
        return idGenerado;
    }

    public PagoO ObtenerPagoPorId(long id)
    {
        PagoO pago = null;
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_obtenerPagoPorId", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@id", id);
        cn.Open();
        using (var dr = cmd.ExecuteReader())
        {
            if (dr.Read())
            {
                pago = new PagoO()
                {
                    IdPago = Convert.ToInt64(dr[0]),
                    HoraPago = Convert.ToDateTime(dr[1]),
                    MontoPago = Convert.ToDecimal(dr[2]),
                    TipoPago = Convert.ToInt64(dr[3]),
                    IdCliente = Convert.ToInt64(dr[4]) 
                };
            }
        }
        return pago;
    }

    public Pago ObtenerPagoPorIdFront(long id)
    {
        Pago pago = null;
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_obtenerPagoPorIdFront", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@id", id);
        cn.Open();
        using (var dr = cmd.ExecuteReader())
        {
            if (dr.Read())
            {
                pago = new Pago()
                {
                    IdPago = Convert.ToInt64(dr[0]),
                    HoraPago = Convert.ToDateTime(dr[1]),
                    MontoPago = Convert.ToDecimal(dr[2]),
                    TipoPago = dr[3].ToString(),
                    NombreCliente = dr[4].ToString(),
                    CorreoCliente = dr[5].ToString(),
                    EstadoPago = dr["EstadoPago"].ToString()
                };
            }
        }
        if (pago != null)
        {
            using var cmdAut = new SqlCommand("SELECT aut_pag FROM pago WHERE ide_pag = @id", cn);
            cmdAut.Parameters.AddWithValue("@id", id);
            var autVal = cmdAut.ExecuteScalar();
            pago.AutPag = autVal == DBNull.Value ? null : (bool?)Convert.ToBoolean(autVal);
        }
        return pago;
    }

    public bool? VerificarAutorizacion(long idPago)
    {
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("SELECT aut_pag FROM pago WHERE ide_pag = @id", cn);
        cmd.Parameters.AddWithValue("@id", idPago);
        cn.Open();
        var val = cmd.ExecuteScalar();
        return val == null || val == DBNull.Value ? null : (bool?)Convert.ToBoolean(val);
    }

    public string ConfirmarPago(long idPago)
    {
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("UPDATE pago SET aut_pag = 1 WHERE ide_pag = @id", cn);
        cmd.Parameters.AddWithValue("@id", idPago);
        cn.Open();
        cmd.ExecuteNonQuery();
        return "Pago confirmado correctamente";
    }

    public string ActualizarPago(PagoO pago)
    {
        string respuesta = "";
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_actualizarPago", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@cliente", pago.IdCliente); 
        cmd.Parameters.AddWithValue("@ide_pag", pago.IdPago);
        cmd.Parameters.AddWithValue("@hor_pag", pago.HoraPago);
        cmd.Parameters.AddWithValue("@mon_pag", pago.MontoPago);
        cmd.Parameters.AddWithValue("@ide_pay", pago.TipoPago); 
        cn.Open();
        cmd.ExecuteNonQuery();
        respuesta = "Pago actualizado correctamente";
        return respuesta;
    }


    public string EliminarPago(long id, long userId)
    {
        string respuesta = "";
        using var cn = new SqlConnection(_connectionString);
        cn.Open();
        using var tran = cn.BeginTransaction();
        try
        {
            using var cmdOwner = new SqlCommand(
                "SELECT COUNT(1) FROM pago p JOIN cliente c ON c.ide_cli = p.ide_cli " +
                "WHERE p.ide_pag = @id AND c.ide_usr = @uid", cn, tran);
            cmdOwner.Parameters.AddWithValue("@id", id);
            cmdOwner.Parameters.AddWithValue("@uid", userId);
            var owner = (int)cmdOwner.ExecuteScalar();
            if (owner == 0)
            {
                tran.Rollback();
                return "No puede eliminar un pago que no le pertenece.";
            }

            using var cmd = new SqlCommand("sp_eliminarPago", cn, tran);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            tran.Commit();
            respuesta = "Pago eliminado correctamente";
        }
        catch (Exception ex)
        {
            tran.Rollback();
            respuesta = "Error al eliminar el pago. Intente nuevamente.";
        }
        return respuesta;
    }

}
