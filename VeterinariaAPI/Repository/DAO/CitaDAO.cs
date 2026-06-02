using VeterinariaAPI.Models.Cita;
using VeterinariaAPI.Repository.Interfaces;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Data.SqlClient;

namespace VeterinariaAPI.Repository.DAO;

public class CitaDAO : ICita
{
    private static readonly string _connectionString = DbConfig.Configuration.GetConnectionString("cn") 
        ?? throw new NullReferenceException("Cadena de conexión no encontrada.");




    public string AgregarCita(CitaO obj, long clienteId)
    {
        using var cn = new SqlConnection(_connectionString);
        cn.Open();
        using var tran = cn.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            // 1. Validar horario laboral 8AM-6PM L-S
            if (obj.CalendarioCita.Hour < 8 || obj.CalendarioCita.Hour >= 18)
                return "Fuera del horario de atención (8:00 AM - 6:00 PM).";
            if (obj.CalendarioCita.DayOfWeek == DayOfWeek.Sunday)
                return "No atendemos los domingos. Elija un día de lunes a sábado.";

            // 2. Validar minutos en intervalos de :00 o :30
            if (obj.CalendarioCita.Minute != 0 && obj.CalendarioCita.Minute != 30)
                return "Las citas solo se pueden agendar en intervalos de 30 minutos (:00 o :30).";

            // 3. Validar consultorio (1-5)
            if (obj.Consultorio < 1 || obj.Consultorio > 5)
                return "Consultorio inválido. Seleccione del 1 al 5.";

            // 4. Validar anticipación mínima de 1 día
            if (obj.CalendarioCita.Date <= DateTime.Today)
                return "Las citas deben agendarse con al menos 1 día de anticipación.";

            // 5. Validar que la mascota existe y está activa
            using var cmdMasExiste = new SqlCommand("SELECT COUNT(1) FROM mascota WHERE ide_mas = @mas AND est_msc = 'A'", cn, tran);
            cmdMasExiste.Parameters.AddWithValue("@mas", obj.IdMascota);
            if ((int)cmdMasExiste.ExecuteScalar() == 0)
                return "La mascota seleccionada no existe o no está activa.";

            // 6. Validar que la mascota pertenece al cliente
            using var cmdMasCli = new SqlCommand(
                "SELECT COUNT(1) FROM mascota m JOIN cliente cl ON cl.ide_cli = m.ide_cli " +
                "WHERE m.ide_mas = @mas AND cl.ide_usr = @usr", cn, tran);
            cmdMasCli.Parameters.AddWithValue("@mas", obj.IdMascota);
            cmdMasCli.Parameters.AddWithValue("@usr", clienteId);
            if ((int)cmdMasCli.ExecuteScalar() == 0)
                return "La mascota no pertenece a su cuenta.";

            // 7. Validar que el veterinario exista
            using var cmdVet = new SqlCommand("SELECT COUNT(1) FROM veterinario WHERE ide_vet = @vet", cn, tran);
            cmdVet.Parameters.AddWithValue("@vet", obj.IdVeterinario);
            if ((int)cmdVet.ExecuteScalar() == 0)
                return "El veterinario seleccionado no existe.";

            // 8. Verificar límite de citas pendientes por cliente (máx 5)
            using var cmdLim = new SqlCommand(
                "SELECT COUNT(1) FROM cita c JOIN mascota m ON m.ide_mas = c.ide_mas " +
                "JOIN cliente cl ON cl.ide_cli = m.ide_cli WHERE cl.ide_usr = @usr AND c.est_cit = 'P'", cn, tran);
            cmdLim.Parameters.AddWithValue("@usr", clienteId);
            if ((int)cmdLim.ExecuteScalar() >= 5)
                return "Ha alcanzado el límite de 5 citas pendientes. Complete o cancele una cita primero.";

            // 9. Validar pago (pertenencia + no usado)
            using var cmdVal = new SqlCommand("sp_validarPagoParaCita", cn, tran);
            cmdVal.CommandType = CommandType.StoredProcedure;
            cmdVal.Parameters.AddWithValue("@ide_pag", obj.IdPago);
            cmdVal.Parameters.AddWithValue("@ide_usr", clienteId);
            var valResult = cmdVal.ExecuteScalar()?.ToString();
            if (valResult == "NO_PERTENECE")
                return "El pago seleccionado no pertenece a su cuenta.";
            if (valResult == "YA_USADO")
                return "Este pago ya fue utilizado en otra cita.";

            // 9b. Validar aut_pag del pago
            using var cmdAut = new SqlCommand("SELECT aut_pag FROM pago WHERE ide_pag = @id", cn, tran);
            cmdAut.Parameters.AddWithValue("@id", obj.IdPago);
            var autVal = cmdAut.ExecuteScalar();
            if (autVal == null || autVal == DBNull.Value || !Convert.ToBoolean(autVal))
                return "El pago no está autorizado. Espere la confirmación del recepcionista.";

            // 10. Validar que la mascota no tenga cita pendiente en el mismo horario (overlap 30 min)
            using var cmdMas = new SqlCommand(
                "SELECT COUNT(1) FROM cita WHERE ide_mas = @mas " +
                "AND NOT (est_cit = 'C' AND cal_cit > DATEADD(HOUR, 24, GETDATE())) AND est_cit != 'A' " +
                "AND cal_cit < @hasta AND DATEADD(MINUTE, 30, cal_cit) > @desde", cn, tran);
            cmdMas.Parameters.AddWithValue("@mas", obj.IdMascota);
            cmdMas.Parameters.AddWithValue("@desde", obj.CalendarioCita);
            cmdMas.Parameters.AddWithValue("@hasta", obj.CalendarioCita.AddMinutes(30));
            if ((int)cmdMas.ExecuteScalar() > 0)
                return "Su mascota ya tiene una cita pendiente en este horario.";

            // 11. Validar que el cliente no tenga otra cita en el mismo horario (otra mascota, overlap 30 min)
            using var cmdCli = new SqlCommand(
                "SELECT COUNT(1) FROM cita c JOIN mascota m ON m.ide_mas = c.ide_mas " +
                "JOIN cliente cl ON cl.ide_cli = m.ide_cli WHERE cl.ide_usr = @usr " +
                "AND NOT (c.est_cit = 'C' AND c.cal_cit > DATEADD(HOUR, 24, GETDATE())) AND c.est_cit != 'A' " +
                "AND c.cal_cit < @hasta AND DATEADD(MINUTE, 30, c.cal_cit) > @desde", cn, tran);
            cmdCli.Parameters.AddWithValue("@usr", clienteId);
            cmdCli.Parameters.AddWithValue("@desde", obj.CalendarioCita);
            cmdCli.Parameters.AddWithValue("@hasta", obj.CalendarioCita.AddMinutes(30));
            if ((int)cmdCli.ExecuteScalar() > 0)
                return "Ya tiene una cita programada en este horario.";

            // 12. Validar que el consultorio corresponde a la especialidad del veterinario
            using var cmdConEsp = new SqlCommand(
                "SELECT COUNT(1) FROM especialidad_consultorio WHERE ide_esp = " +
                "(SELECT ide_esp FROM veterinario WHERE ide_vet = @vet) AND ide_con = @con", cn, tran);
            cmdConEsp.Parameters.AddWithValue("@vet", obj.IdVeterinario);
            cmdConEsp.Parameters.AddWithValue("@con", obj.Consultorio);
            if ((int)cmdConEsp.ExecuteScalar() == 0)
                return "El consultorio seleccionado no corresponde a la especialidad del veterinario.";

            // 13. Disponibilidad del slot (vet + consultorio)
            if (ExisteCitaEnHorarioTransaccion(tran, 0, obj.CalendarioCita, obj.IdVeterinario, obj.Consultorio))
                return "Lo sentimos, ese horario ya está ocupado. Por favor, elija otra fecha u hora.";

            // 14. Validar que no haya hold activo de otro usuario
            using var cmdHold = new SqlCommand(
                "SELECT COUNT(1) FROM hold_cita WHERE ide_vet = @vet " +
                "AND fec_hol = @fec AND hor_hol = @hor " +
                "AND fec_exp > GETDATE() AND ide_cli != @cli", cn, tran);
            cmdHold.Parameters.AddWithValue("@vet", obj.IdVeterinario);
            cmdHold.Parameters.AddWithValue("@fec", obj.CalendarioCita.Date);
            cmdHold.Parameters.AddWithValue("@hor", obj.CalendarioCita.TimeOfDay);
            cmdHold.Parameters.AddWithValue("@cli", clienteId);
            if ((int)cmdHold.ExecuteScalar() > 0)
                return "Este horario está siendo reservado por otro usuario. Intente nuevamente en 4 minutos.";

            // 15. Insertar cita
            using var cmd = new SqlCommand("sp_agregarCita", cn, tran);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@calendario", obj.CalendarioCita);
            cmd.Parameters.AddWithValue("@consultorio", obj.Consultorio);
            cmd.Parameters.AddWithValue("@veterinario", obj.IdVeterinario);
            cmd.Parameters.AddWithValue("@mascota", obj.IdMascota);
            cmd.Parameters.AddWithValue("@pago", obj.IdPago);
            cmd.ExecuteNonQuery();

            tran.Commit();
            return "Cita registrada correctamente";
        }
        catch (Exception ex)
        {
            tran.Rollback();
            return "Error al registrar la cita. Intente nuevamente.";
        }
    }



    public CitaO BuscarCita(long id)
    {
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_buscarCitaPorId", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@idCita", id);

        try
        {
            cn.Open();
            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                return new CitaO
                {
                    IdCita = Convert.ToInt64(dr["ide_cit"]),
                    CalendarioCita = Convert.ToDateTime(dr["cal_cit"]),
                    Consultorio = Convert.ToInt64(dr["con_cit"]),
                    IdVeterinario = Convert.ToInt64(dr["ide_vet"]),
                    IdMascota = Convert.ToInt64(dr["ide_mas"]),
                    IdPago = Convert.ToInt64(dr["ide_pag"]),
                    EstadoCita = dr["est_cit"].ToString()
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al buscar cita por ID: {ex.Message}");
        }
        return null;
    }



    public Cita BuscarCitaFront(long id)
    {
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_buscarCitaPorId", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@idCita", id);

        try
        {
            cn.Open();
            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                return new Cita
                {
                    IdCita = Convert.ToInt64(dr["ide_cit"]),
                    CalendarioCita = Convert.ToDateTime(dr["cal_cit"]),
                    Consultorio = Convert.ToInt64(dr["con_cit"]),
                    NombreVeterinario = dr["NombreVeterinario"].ToString(),
                    NombreMascota = dr["NombreMascota"].ToString(),
                    MontoPago = Convert.ToDecimal(dr["mon_pag"]),
                    EstadoCita = dr["est_cit"].ToString()
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al buscar cita front por ID: {ex.Message}");
        }
        return null;
    }

    public void EliminarCita(long id)
    {
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_eliminarCita", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@idCita", id);

        try
        {
            cn.Open();
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw new Exception("Error al eliminar la cita. Intente nuevamente.");
        }
    }


    //VALIDACION DE DISPONIBILIDAD DE CITA
    private bool ExisteCitaEnHorario(long idCita, DateTime fechaHora, long idVeterinario, long idConsultorio)
    {
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_verificarDisponibilidadCita", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@idCita", idCita);
        cmd.Parameters.AddWithValue("@fechaHora", fechaHora);
        cmd.Parameters.AddWithValue("@idVeterinario", idVeterinario);
        cmd.Parameters.AddWithValue("@idConsultorio", idConsultorio);

        try
        {
            cn.Open();
            var result = cmd.ExecuteScalar();
            return result != null && Convert.ToInt64(result) > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al verificar disponibilidad: {ex.Message}");
            return false;
        }
    }

    private bool ExisteCitaEnHorarioTransaccion(SqlTransaction tran, long idCita, DateTime fechaHora, long idVeterinario, long idConsultorio)
    {
        using var cmd = new SqlCommand("sp_verificarDisponibilidadCita", tran.Connection, tran);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@idCita", idCita);
        cmd.Parameters.AddWithValue("@fechaHora", fechaHora);
        cmd.Parameters.AddWithValue("@idVeterinario", idVeterinario);
        cmd.Parameters.AddWithValue("@idConsultorio", idConsultorio);

        try
        {
            var result = cmd.ExecuteScalar();
            return result != null && Convert.ToInt64(result) > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al verificar disponibilidad: {ex.Message}");
            return false;
        }
    }






    //LISTADO DE CITAS PENDIENTES, VENCIDAS, ATENDIDAS, CANCELADAS

    public List<Cita> ListarCitasPendientes()
    {
        var lista = new List<Cita>();
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_listarCitasPendientes", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        try
        {
            cn.Open();
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new Cita
                {
                    IdCita = Convert.ToInt64(dr["ide_cit"]),
                    CalendarioCita = Convert.ToDateTime(dr["cal_cit"]),
                    Consultorio = Convert.ToInt64(dr["con_cit"]),
                    NombreVeterinario = dr["NombreVeterinario"].ToString(),
                    NombreMascota = dr["NombreMascota"].ToString(),
                    MontoPago = Convert.ToDecimal(dr["mon_pag"]),
                    EstadoCita = dr["est_cit"].ToString()
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al listar citas pendientes: " + ex.Message);
        }
        return lista;
    }

    //--------------------------------------------------
    public List<Cita> ListarCitasVencidas()
    {
        var lista = new List<Cita>();
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_listarCitasVencidas", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        try
        {
            cn.Open();
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new Cita
                {
                    IdCita = Convert.ToInt64(dr["ide_cit"]),
                    CalendarioCita = Convert.ToDateTime(dr["cal_cit"]),
                    Consultorio = Convert.ToInt64(dr["con_cit"]),
                    NombreVeterinario = dr["NombreVeterinario"].ToString(),
                    NombreMascota = dr["NombreMascota"].ToString(),
                    MontoPago = Convert.ToDecimal(dr["mon_pag"]),
                    EstadoCita = dr["est_cit"].ToString()
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al listar citas vencidas: " + ex.Message);
        }
        return lista;
    }

    public (string mensaje, string emailCliente, string nombreCliente, string nombreMascota, string fechaCita) MarcarInasistencia(long idCita)
    {
        using var cn = new SqlConnection(_connectionString);
        cn.Open();
        using var tran = cn.BeginTransaction();
        try
        {
            using var cmdUpd = new SqlCommand(
                "UPDATE cita SET est_cit = 'C' WHERE ide_cit = @id AND est_cit = 'P'", cn, tran);
            cmdUpd.Parameters.AddWithValue("@id", idCita);
            var rows = cmdUpd.ExecuteNonQuery();
            if (rows == 0)
            {
                tran.Rollback();
                return ("La cita no se encuentra en estado Pendiente.", "", "", "", "");
            }

            using var cmdSel = new SqlCommand(@"
                SELECT u.cor_usr, u.nom_usr, u.ape_usr, m.nom_mas, c.cal_cit
                FROM cita c
                INNER JOIN mascota m ON c.ide_mas = m.ide_mas
                INNER JOIN cliente cl ON m.ide_cli = cl.ide_cli
                INNER JOIN usuario u ON cl.ide_usr = u.ide_usr
                WHERE c.ide_cit = @id", cn, tran);
            cmdSel.Parameters.AddWithValue("@id", idCita);
            using var dr = cmdSel.ExecuteReader();
            string email = "", nombre = "", mascota = "", fechaStr = "";
            if (dr.Read())
            {
                email = dr["cor_usr"]?.ToString() ?? "";
                nombre = dr["nom_usr"] + " " + dr["ape_usr"];
                mascota = dr["nom_mas"]?.ToString() ?? "";
                var fecha = (DateTime)dr["cal_cit"];
                fechaStr = fecha.ToString("dd/MM/yyyy 'a las' hh:mm tt");
            }
            dr.Close();

            tran.Commit();
            return ("ok", email, nombre, mascota, fechaStr);
        }
        catch (Exception ex)
        {
            tran.Rollback();
            return ("Error al cancelar la cita. Intente nuevamente.", "", "", "", "");
        }
    }

    //------------------------------------

    public List<Cita> ListarCitasAtendidas()
    {
        var lista = new List<Cita>();
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_listarCitasAtendidas", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        try
        {
            cn.Open();
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new Cita
                {
                    IdCita = Convert.ToInt64(dr["ide_cit"]),
                    CalendarioCita = Convert.ToDateTime(dr["cal_cit"]),
                    Consultorio = Convert.ToInt64(dr["con_cit"]),
                    NombreVeterinario = dr["NombreVeterinario"].ToString(),
                    NombreMascota = dr["NombreMascota"].ToString(),
                    MontoPago = Convert.ToDecimal(dr["mon_pag"]),
                    EstadoCita = dr["est_cit"].ToString()
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al listar citas atendidas: " + ex.Message);
        }
        return lista;
    }

    //Listado de citas Canceladas

    public List<Cita> ListarCitasCanceladas()
    {
        var lista = new List<Cita>();
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_listarCitasCanceladas", cn);
        cmd.CommandType = CommandType.StoredProcedure;

        try
        {
            cn.Open();
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new Cita
                {
                    IdCita = Convert.ToInt64(dr["ide_cit"]),
                    CalendarioCita = Convert.ToDateTime(dr["cal_cit"]),
                    Consultorio = Convert.ToInt64(dr["con_cit"]),
                    NombreVeterinario = dr["NombreVeterinario"].ToString(),
                    NombreMascota = dr["NombreMascota"].ToString(),
                    MontoPago = Convert.ToDecimal(dr["mon_pag"]),
                    EstadoCita = dr["est_cit"].ToString()
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al listar citas canceladas: " + ex.Message);
        }
        return lista;
    }



    // LISTAR CITAS POR ESTADO 

    public List<Cita> ListarCitasPorEstado(string estado)
    {
        var lista = new List<Cita>();
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_listarCitasPorEstado", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@estado", estado);

        try
        {
            cn.Open();
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new Cita
                {
                    IdCita = Convert.ToInt64(dr["ide_cit"]),
                    CalendarioCita = Convert.ToDateTime(dr["cal_cit"]),
                    Consultorio = Convert.ToInt64(dr["con_cit"]),
                    NombreVeterinario = dr["NombreVeterinario"].ToString(),
                    NombreMascota = dr["NombreMascota"].ToString(),
                    MontoPago = Convert.ToDecimal(dr["mon_pag"]),
                    EstadoCita = dr["est_cit"].ToString()
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al listar citas por estado: {ex.Message}");
        }
        return lista;
    }






    public string ModificarCita(CitaO obj, long clienteId)
    {
        using var cn = new SqlConnection(_connectionString);
        cn.Open();
        using var tran = cn.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            // 1. Validar horario laboral
            if (obj.CalendarioCita.Hour < 8 || obj.CalendarioCita.Hour >= 18)
                return "Fuera del horario de atención (8:00 AM - 6:00 PM).";
            if (obj.CalendarioCita.DayOfWeek == DayOfWeek.Sunday)
                return "No atendemos los domingos.";

            // 2. Validar minutos en intervalos de :00 o :30
            if (obj.CalendarioCita.Minute != 0 && obj.CalendarioCita.Minute != 30)
                return "Las citas solo se pueden agendar en intervalos de 30 minutos (:00 o :30).";

            // 3. Validar consultorio
            if (obj.Consultorio < 1 || obj.Consultorio > 5)
                return "Consultorio inválido. Seleccione del 1 al 5.";

            // 4. Validar que la cita original tenga más de 12 horas de anticipación
            using var cmdOrig = new SqlCommand("SELECT cal_cit FROM cita WHERE ide_cit = @id", cn, tran);
            cmdOrig.Parameters.AddWithValue("@id", obj.IdCita);
            var fechaOriginal = (DateTime)cmdOrig.ExecuteScalar();
            if (fechaOriginal <= DateTime.Now.AddHours(12))
                return "No puede editar una cita cuando faltan menos de 12 horas para la misma.";

            // 5. Validar anticipación mínima de 24 horas para la nueva fecha
            if (obj.CalendarioCita <= DateTime.Now.AddHours(24))
                return "Las citas deben modificarse con al menos 24 horas de anticipación.";

            // 6. Validar que la mascota existe y está activa
            using var cmdMasExiste = new SqlCommand("SELECT COUNT(1) FROM mascota WHERE ide_mas = @mas AND est_msc = 'A'", cn, tran);
            cmdMasExiste.Parameters.AddWithValue("@mas", obj.IdMascota);
            if ((int)cmdMasExiste.ExecuteScalar() == 0)
                return "La mascota seleccionada no existe o no está activa.";

            // 7. Validar que el veterinario exista
            using var cmdVet = new SqlCommand("SELECT COUNT(1) FROM veterinario WHERE ide_vet = @vet", cn, tran);
            cmdVet.Parameters.AddWithValue("@vet", obj.IdVeterinario);
            if ((int)cmdVet.ExecuteScalar() == 0)
                return "El veterinario seleccionado no existe.";

            // 8. Si clienteId > 0, validar pertenencia + pago + solapamiento
            if (clienteId > 0)
            {
                // 8a. Validar que la mascota pertenece al cliente
                using var cmdMasCli = new SqlCommand(
                    "SELECT COUNT(1) FROM mascota m JOIN cliente cl ON cl.ide_cli = m.ide_cli " +
                    "WHERE m.ide_mas = @mas AND cl.ide_usr = @usr", cn, tran);
                cmdMasCli.Parameters.AddWithValue("@mas", obj.IdMascota);
                cmdMasCli.Parameters.AddWithValue("@usr", clienteId);
                if ((int)cmdMasCli.ExecuteScalar() == 0)
                    return "La mascota no pertenece a su cuenta.";

                // 8b. Validar pago (pertenencia)
                using var cmdVal = new SqlCommand("sp_validarPagoParaCita", cn, tran);
                cmdVal.CommandType = CommandType.StoredProcedure;
                cmdVal.Parameters.AddWithValue("@ide_pag", obj.IdPago);
                cmdVal.Parameters.AddWithValue("@ide_usr", clienteId);
                var valResult = cmdVal.ExecuteScalar()?.ToString();
                if (valResult == "NO_PERTENECE")
                    return "El pago seleccionado no pertenece a su cuenta.";

                // 8c. Validar que la mascota no tenga otra cita pendiente (overlap 30 min)
                using var cmdMas = new SqlCommand(
                    "SELECT COUNT(1) FROM cita WHERE ide_cit != @idCita AND ide_mas = @mas " +
                    "AND NOT (est_cit = 'C' AND cal_cit > DATEADD(HOUR, 24, GETDATE())) AND est_cit != 'A' " +
                    "AND cal_cit < @hasta AND DATEADD(MINUTE, 30, cal_cit) > @desde", cn, tran);
                cmdMas.Parameters.AddWithValue("@idCita", obj.IdCita);
                cmdMas.Parameters.AddWithValue("@mas", obj.IdMascota);
                cmdMas.Parameters.AddWithValue("@desde", obj.CalendarioCita);
                cmdMas.Parameters.AddWithValue("@hasta", obj.CalendarioCita.AddMinutes(30));
                if ((int)cmdMas.ExecuteScalar() > 0)
                    return "Su mascota ya tiene una cita pendiente en este horario.";

                // 8d. Validar que el cliente no tenga otra cita en el mismo horario
                using var cmdCli = new SqlCommand(
                    "SELECT COUNT(1) FROM cita c JOIN mascota m ON m.ide_mas = c.ide_mas " +
                    "JOIN cliente cl ON cl.ide_cli = m.ide_cli WHERE c.ide_cit != @idCita AND cl.ide_usr = @usr " +
                    "AND NOT (c.est_cit = 'C' AND c.cal_cit > DATEADD(HOUR, 24, GETDATE())) AND c.est_cit != 'A' " +
                    "AND c.cal_cit < @hasta AND DATEADD(MINUTE, 30, c.cal_cit) > @desde", cn, tran);
                cmdCli.Parameters.AddWithValue("@idCita", obj.IdCita);
                cmdCli.Parameters.AddWithValue("@usr", clienteId);
                cmdCli.Parameters.AddWithValue("@desde", obj.CalendarioCita);
                cmdCli.Parameters.AddWithValue("@hasta", obj.CalendarioCita.AddMinutes(30));
                if ((int)cmdCli.ExecuteScalar() > 0)
                    return "Ya tiene una cita programada en este horario.";
            }

            // 9. Validar que el consultorio corresponde a la especialidad del veterinario
            using var cmdConEsp = new SqlCommand(
                "SELECT COUNT(1) FROM especialidad_consultorio WHERE ide_esp = " +
                "(SELECT ide_esp FROM veterinario WHERE ide_vet = @vet) AND ide_con = @con", cn, tran);
            cmdConEsp.Parameters.AddWithValue("@vet", obj.IdVeterinario);
            cmdConEsp.Parameters.AddWithValue("@con", obj.Consultorio);
            if ((int)cmdConEsp.ExecuteScalar() == 0)
                return "El consultorio seleccionado no corresponde a la especialidad del veterinario.";

            // 10. Validar disponibilidad del slot (vet + consultorio)
            if (ExisteCitaEnHorarioTransaccion(tran, obj.IdCita, obj.CalendarioCita, obj.IdVeterinario, obj.Consultorio))
                return "Lo sentimos, ese horario ya está ocupado. Por favor, elija otra fecha u hora.";

            // 11. Validar que no haya hold activo de otro usuario
            using var cmdHold = new SqlCommand(
                "SELECT COUNT(1) FROM hold_cita WHERE ide_vet = @vet " +
                "AND fec_hol = @fec AND hor_hol = @hor " +
                "AND fec_exp > GETDATE() AND ide_cli != @cli", cn, tran);
            cmdHold.Parameters.AddWithValue("@vet", obj.IdVeterinario);
            cmdHold.Parameters.AddWithValue("@fec", obj.CalendarioCita.Date);
            cmdHold.Parameters.AddWithValue("@hor", obj.CalendarioCita.TimeOfDay);
            cmdHold.Parameters.AddWithValue("@cli", clienteId > 0 ? clienteId : -1);
            if ((int)cmdHold.ExecuteScalar() > 0)
                return "Este horario está siendo reservado por otro usuario. Intente nuevamente en 4 minutos.";

            // 12. Actualizar cita
            using var cmd = new SqlCommand("sp_actualizarCita", cn, tran);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@idCita", obj.IdCita);
            cmd.Parameters.AddWithValue("@calendario", obj.CalendarioCita);
            cmd.Parameters.AddWithValue("@consultorio", obj.Consultorio);
            cmd.Parameters.AddWithValue("@veterinario", obj.IdVeterinario);
            cmd.Parameters.AddWithValue("@mascota", obj.IdMascota);
            cmd.Parameters.AddWithValue("@pago", obj.IdPago);
            cmd.ExecuteNonQuery();

            tran.Commit();
            return "Cita actualizada correctamente";
        }
        catch (Exception ex)
        {
            tran.Rollback();
            return "Error al actualizar la cita. Intente nuevamente.";
        }
    }




    // Actualizar estado de la cita con validación opcional de ownership
    public string ActualizarEstadoCita(long idCita, string estado, long? clienteId = null)
    {
        using var cn = new SqlConnection(_connectionString);
        cn.Open();

        var transicionesValidas = new Dictionary<string, string[]>
        {
            { "P", new[] { "E", "C" } },
            { "E", new[] { "A", "P" } },
            { "A", Array.Empty<string>() },
            { "C", Array.Empty<string>() }
        };

        using var tran = cn.BeginTransaction();
        try
        {
            using var cmdCurrent = new SqlCommand(
                "SELECT est_cit FROM cita WITH (UPDLOCK, ROWLOCK) WHERE ide_cit = @idCita", cn, tran);
            cmdCurrent.Parameters.AddWithValue("@idCita", idCita);
            var currentEstado = cmdCurrent.ExecuteScalar()?.ToString();
            if (currentEstado == null)
                return "La cita no existe.";

            if (!transicionesValidas.TryGetValue(currentEstado, out var allowed) || !allowed.Contains(estado))
                return $"No se puede cambiar de '{currentEstado}' a '{estado}'. Transición no permitida.";

            if (clienteId.HasValue)
            {
                using var cmdCheck = new SqlCommand(
                    "SELECT COUNT(1) FROM cita c JOIN mascota m ON m.ide_mas = c.ide_mas " +
                    "JOIN cliente cl ON cl.ide_cli = m.ide_cli WHERE c.ide_cit = @idCita AND cl.ide_usr = @clienteId", cn, tran);
                cmdCheck.Parameters.AddWithValue("@idCita", idCita);
                cmdCheck.Parameters.AddWithValue("@clienteId", clienteId.Value);
                if ((int)cmdCheck.ExecuteScalar() == 0)
                    return "No tienes permiso para modificar esta cita.";
            }

            using var cmd = new SqlCommand("sp_actualizarEstadoCita", cn, tran);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@idCita", idCita);
            cmd.Parameters.AddWithValue("@estado", estado);
            cmd.ExecuteNonQuery();

            tran.Commit();
        }
        catch (Exception ex)
        {
            tran.Rollback();
            return "Error al actualizar el estado. Intente nuevamente.";
        }

        return estado switch
        {
            "P" => "Cita marcada como Pendiente",
            "E" => "Cita en proceso de atención",
            "A" => "Cita atendida correctamente",
            "C" => "Cita cancelada correctamente",
            _ => "Estado actualizado"
        };
    }

    // ==================== HISTORIAL MÉDICO ====================

    // Agregar o actualizar historial médico
    public string AgregarHistorialMedico(HistorialMedicoDTO dto)
    {
        string mensaje = "";
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_agregarHistorialMedico", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@ide_cit", dto.IdCita);
        cmd.Parameters.AddWithValue("@sintomas", (object?)dto.Sintomas ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@diagnostico", dto.Diagnostico);
        cmd.Parameters.AddWithValue("@tratamiento", dto.Tratamiento);
        cmd.Parameters.AddWithValue("@medicamentos", (object?)dto.Medicamentos ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@observaciones", (object?)dto.Observaciones ?? DBNull.Value);
        try
        {
            cn.Open();
            cmd.ExecuteNonQuery();
            mensaje = "Historial médico guardado correctamente";
        }
        catch (Exception ex)
        {
            mensaje = "Error al guardar historial médico. Intente nuevamente.";
        }
        return mensaje;
    }

    // Obtener historial médico por ID de cita
    public HistorialMedico? ObtenerHistorialPorCita(long idCita)
    {
        HistorialMedico? historial = null;
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_obtenerHistorialPorCita", cn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@ide_cit", idCita);

        cn.Open();
        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            historial = new HistorialMedico
            {
                ide_his = Convert.ToInt64(dr["ide_his"]),
                ide_cit = Convert.ToInt64(dr["ide_cit"]),
                sintomas = dr["sintomas"] == DBNull.Value ? null : dr["sintomas"].ToString(),
                diagnostico = dr["diagnostico"].ToString() ?? "",
                tratamiento = dr["tratamiento"].ToString() ?? "",
                medicamentos = dr["medicamentos"] == DBNull.Value ? null : dr["medicamentos"].ToString(),
                observaciones = dr["observaciones"] == DBNull.Value ? null : dr["observaciones"].ToString(),
                fecha_atencion = Convert.ToDateTime(dr["fecha_atencion"])
            };
        }
        return historial;
    }

    // ==================== HOLD TEMPORAL ====================

    public long CrearHold(long clienteId, long vetId, DateTime fechaHora)
    {
        using var cn = new SqlConnection(_connectionString);
        cn.Open();

        // Liberar holds previos de este cliente
        using var cmdDel = new SqlCommand(
            "DELETE FROM hold_cita WHERE ide_cli = @cli", cn);
        cmdDel.Parameters.AddWithValue("@cli", clienteId);
        cmdDel.ExecuteNonQuery();

        // Crear nuevo hold con 4 min de expiración
        using var cmdIns = new SqlCommand(
            "INSERT INTO hold_cita (ide_cli, ide_vet, fec_hol, hor_hol, fec_exp) " +
            "OUTPUT INSERTED.ide_hol " +
            "VALUES (@cli, @vet, @fec, @hor, DATEADD(MINUTE, 4, GETDATE()))", cn);
        cmdIns.Parameters.AddWithValue("@cli", clienteId);
        cmdIns.Parameters.AddWithValue("@vet", vetId);
        cmdIns.Parameters.AddWithValue("@fec", fechaHora.Date);
        cmdIns.Parameters.AddWithValue("@hor", fechaHora.TimeOfDay);
        return (long)cmdIns.ExecuteScalar();
    }

    public long IntentarHold(long clienteId, long vetId, DateTime fechaHora)
    {
        using var cn = new SqlConnection(_connectionString);
        cn.Open();
        using var tran = cn.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            using var cmdCita = new SqlCommand(
                "SELECT COUNT(1) FROM cita WHERE ide_vet = @vet AND cal_cit = @fecHora " +
                "AND NOT (est_cit = 'C' AND cal_cit > DATEADD(HOUR, 24, GETDATE()))", cn, tran);
            cmdCita.Parameters.AddWithValue("@vet", vetId);
            cmdCita.Parameters.AddWithValue("@fecHora", fechaHora);
            if ((int)cmdCita.ExecuteScalar() > 0)
                return -1;

            using var cmdHold = new SqlCommand(
                "SELECT COUNT(1) FROM hold_cita WHERE ide_vet = @vet " +
                "AND fec_hol = @fec AND hor_hol = @hor " +
                "AND fec_exp > GETDATE() AND ide_cli != @cli", cn, tran);
            cmdHold.Parameters.AddWithValue("@vet", vetId);
            cmdHold.Parameters.AddWithValue("@fec", fechaHora.Date);
            cmdHold.Parameters.AddWithValue("@hor", fechaHora.TimeOfDay);
            cmdHold.Parameters.AddWithValue("@cli", clienteId);
            if ((int)cmdHold.ExecuteScalar() > 0)
                return -2;

            using var cmdDel = new SqlCommand("DELETE FROM hold_cita WHERE ide_cli = @cli", cn, tran);
            cmdDel.Parameters.AddWithValue("@cli", clienteId);
            cmdDel.ExecuteNonQuery();

            using var cmdIns = new SqlCommand(
                "INSERT INTO hold_cita (ide_cli, ide_vet, fec_hol, hor_hol, fec_exp) " +
                "OUTPUT INSERTED.ide_hol " +
                "VALUES (@cli, @vet, @fec, @hor, DATEADD(MINUTE, 4, GETDATE()))", cn, tran);
            cmdIns.Parameters.AddWithValue("@cli", clienteId);
            cmdIns.Parameters.AddWithValue("@vet", vetId);
            cmdIns.Parameters.AddWithValue("@fec", fechaHora.Date);
            cmdIns.Parameters.AddWithValue("@hor", fechaHora.TimeOfDay);
            var newId = (long)cmdIns.ExecuteScalar();

            tran.Commit();
            return newId;
        }
        catch
        {
            tran.Rollback();
            return -3;
        }
    }

    public void LiberarHold(long holdId)
    {
        if (holdId <= 0) return;
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("DELETE FROM hold_cita WHERE ide_hol = @id", cn);
        cmd.Parameters.AddWithValue("@id", holdId);
        cn.Open();
        cmd.ExecuteNonQuery();
    }

    public void RenovarHold(long holdId)
    {
        if (holdId <= 0) return;
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(
            "UPDATE hold_cita SET fec_exp = DATEADD(MINUTE, 4, GETDATE()) WHERE ide_hol = @id", cn);
        cmd.Parameters.AddWithValue("@id", holdId);
        cn.Open();
        cmd.ExecuteNonQuery();
    }

    public List<SlotSemana> ObtenerSlotsSemana(DateTime fechaInicio, long? vetId, long clienteId)
    {
        var desde = fechaInicio.Date;
        var hasta = desde.AddDays(7);

        var lista = new List<SlotSemana>();
        using var cn = new SqlConnection(_connectionString);
        cn.Open();

        var sql = @"
            SELECT cal_cit AS FechaHora, c.ide_vet, 'ocupado' AS Estado,
                   u.nom_usr + ' ' + u.ape_usr AS NombreVeterinario, c.con_cit AS Consultorio,
                   NULL AS IdHold,
                   c.est_cit AS EstadoCitaDB,
                   cl.ide_usr AS IdCliente
            FROM cita c
            JOIN veterinario v ON v.ide_vet = c.ide_vet
            JOIN usuario u ON u.ide_usr = v.ide_usr
            JOIN mascota m ON m.ide_mas = c.ide_mas
            JOIN cliente cl ON cl.ide_cli = m.ide_cli
            WHERE c.cal_cit >= @desde AND c.cal_cit < @hasta
                AND NOT (c.est_cit = 'C' AND c.cal_cit > DATEADD(HOUR, 24, GETDATE()))
                AND (@vetId IS NULL OR c.ide_vet = @vetId)

            UNION ALL

            SELECT CAST(h.fec_hol AS DATETIME) + CAST(h.hor_hol AS DATETIME),
                   h.ide_vet,
                   CASE WHEN h.ide_cli = @cliId THEN 'hold_propio' ELSE 'hold_otro' END,
                   u.nom_usr + ' ' + u.ape_usr,
                   ISNULL((SELECT TOP 1 ec.ide_con FROM especialidad_consultorio ec
                           JOIN veterinario v2 ON v2.ide_esp = ec.ide_esp
                           WHERE v2.ide_vet = h.ide_vet), 0),
                   h.ide_hol,
                   NULL AS EstadoCitaDB,
                   h.ide_cli AS IdCliente
            FROM hold_cita h
            JOIN veterinario v ON v.ide_vet = h.ide_vet
            JOIN usuario u ON u.ide_usr = v.ide_usr
            WHERE CAST(h.fec_hol AS DATETIME) + CAST(h.hor_hol AS DATETIME) >= @desde
                AND CAST(h.fec_hol AS DATETIME) + CAST(h.hor_hol AS DATETIME) < @hasta
                AND h.fec_exp > GETDATE()
                AND (@vetId IS NULL OR h.ide_vet = @vetId)";

        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@desde", desde);
        cmd.Parameters.AddWithValue("@hasta", hasta);
        cmd.Parameters.AddWithValue("@vetId", vetId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@cliId", clienteId);

        using var dr = cmd.ExecuteReader();
        while (dr.Read())
        {
            lista.Add(new SlotSemana
            {
                FechaHora = Convert.ToDateTime(dr["FechaHora"]),
                IdVeterinario = Convert.ToInt64(dr["ide_vet"]),
                Estado = dr["Estado"].ToString(),
                NombreVeterinario = dr["NombreVeterinario"].ToString(),
                Consultorio = Convert.ToInt32(dr["Consultorio"]),
                IdHold = dr["IdHold"] == DBNull.Value ? null : Convert.ToInt64(dr["IdHold"]),
                EstadoCitaDB = dr["EstadoCitaDB"] == DBNull.Value ? null : dr["EstadoCitaDB"].ToString(),
                IdCliente = dr["IdCliente"] == DBNull.Value ? null : Convert.ToInt64(dr["IdCliente"])
            });
        }

        return lista;
    }

    public object? BuscarCitaConDetalle(long id)
    {
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(@"
            SELECT c.ide_cit, c.cal_cit, c.con_cit, c.ide_vet, c.est_cit,
                   u.nom_usr + ' ' + u.ape_usr AS NombreVeterinario,
                   m.nom_mas AS NombreMascota,
                   p.mon_pag AS MontoPago
            FROM cita c
            INNER JOIN veterinario v ON v.ide_vet = c.ide_vet
            INNER JOIN usuario u ON u.ide_usr = v.ide_usr
            INNER JOIN mascota m ON m.ide_mas = c.ide_mas
            INNER JOIN pago p ON p.ide_pag = c.ide_pag
            WHERE c.ide_cit = @id", cn);
        cmd.Parameters.AddWithValue("@id", id);
        cn.Open();
        using var dr = cmd.ExecuteReader();
        if (dr.Read())
        {
            return new
            {
                IdCita = Convert.ToInt64(dr["ide_cit"]),
                CalendarioCita = Convert.ToDateTime(dr["cal_cit"]),
                Consultorio = Convert.ToInt64(dr["con_cit"]),
                IdVeterinario = Convert.ToInt64(dr["ide_vet"]),
                NombreVeterinario = dr["NombreVeterinario"].ToString(),
                NombreMascota = dr["NombreMascota"].ToString(),
                MontoPago = Convert.ToDecimal(dr["MontoPago"]),
                EstadoCita = dr["est_cit"].ToString()
            };
        }
        return null;
    }

    public void LimpiarHoldsExpirados()
    {
        using var cn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("DELETE FROM hold_cita WHERE fec_exp <= GETDATE()", cn);
        cn.Open();
        cmd.ExecuteNonQuery();
    }

    public string ReagendarCita(long idCitaCancelada, DateTime nuevaFecha, int nuevoConsultorio, long nuevoVeterinarioId)
    {
        using var cn = new SqlConnection(_connectionString);
        cn.Open();
        using var tran = cn.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            // 1. Obtener la cita cancelada + su pago + fecha original
            long idePag = 0, ideMas = 0;
            DateTime? fechaOriginal = null;
            string? estCit = null;
            using var cmdSel = new SqlCommand(
                "SELECT est_cit, ide_pag, ide_mas, cal_cit FROM cita WHERE ide_cit = @id", cn, tran);
            cmdSel.Parameters.AddWithValue("@id", idCitaCancelada);
            using var dr = cmdSel.ExecuteReader();
            if (dr.Read())
            {
                estCit = dr["est_cit"].ToString();
                idePag = Convert.ToInt64(dr["ide_pag"]);
                ideMas = Convert.ToInt64(dr["ide_mas"]);
                fechaOriginal = Convert.ToDateTime(dr["cal_cit"]);
            }
            dr.Close();

            if (estCit != "C")
                return "La cita original no está cancelada.";
            if (idePag == 0)
                return "La cita no tiene un pago asociado.";

            // 2. Validar que la fecha sea la misma que la original
            if (nuevaFecha.Date != fechaOriginal?.Date)
                return "Solo se puede reagendar en la misma fecha de la cita original.";

            // 3. Validar hora exacta: solo 17:00 o 17:30
            if (nuevaFecha.Hour != 17 || (nuevaFecha.Minute != 0 && nuevaFecha.Minute != 30))
                return "Solo se puede reagendar a las 5:00 PM o 5:30 PM.";
            if (nuevoConsultorio < 1 || nuevoConsultorio > 5)
                return "Consultorio inválido (1-5).";

            // 4. Validar que el veterinario exista
            using var cmdVet = new SqlCommand("SELECT COUNT(1) FROM veterinario WHERE ide_vet = @vet", cn, tran);
            cmdVet.Parameters.AddWithValue("@vet", nuevoVeterinarioId);
            if ((int)cmdVet.ExecuteScalar() == 0)
                return "El veterinario seleccionado no existe.";

            // 5. Validar slot disponible (excluir la cita que estamos actualizando)
            if (ExisteCitaEnHorarioTransaccion(tran, idCitaCancelada, nuevaFecha, nuevoVeterinarioId, nuevoConsultorio))
                return "El horario seleccionado ya está ocupado.";

            // 5. Actualizar la cita cancelada con los nuevos datos (misma mascota, mismo pago)
            using var cmdUpd = new SqlCommand(@"
                UPDATE cita
                SET cal_cit = @cal, con_cit = @con, ide_vet = @vet, est_cit = 'P'
                WHERE ide_cit = @id;", cn, tran);
            cmdUpd.Parameters.AddWithValue("@cal", nuevaFecha);
            cmdUpd.Parameters.AddWithValue("@con", nuevoConsultorio);
            cmdUpd.Parameters.AddWithValue("@vet", nuevoVeterinarioId);
            cmdUpd.Parameters.AddWithValue("@id", idCitaCancelada);
            cmdUpd.ExecuteNonQuery();

            tran.Commit();
            return $"ok:{idCitaCancelada}";
        }
        catch (Exception ex)
        {
            tran.Rollback();
            return "Error al reagendar. Intente nuevamente.";
        }
    }
}