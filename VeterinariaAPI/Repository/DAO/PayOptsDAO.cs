using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using VeterinariaAPI.Models.Pago;
using VeterinariaAPI.Repository.Interfaces;

namespace VeterinariaAPI.Repository.DAO;

public class PayOptsDAO : IPayOpts
{
    private static readonly string _connectionString = DbConfig.Configuration.GetConnectionString("cn") 
        ?? throw new NullReferenceException();

    public IEnumerable<PayOpts> ListarTiposDePago()
    {
        var lista = new List<PayOpts>();
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_listarPaymentOptions", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cn.Open();
        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            lista.Add(new PayOpts
            {
                ide_pay = Convert.ToInt64(dr[0]),
                nom_pay = dr[1].ToString()
            });
        }
        return lista;
    }
}