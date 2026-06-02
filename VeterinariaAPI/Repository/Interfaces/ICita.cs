using VeterinariaAPI.Models.Cita;

namespace VeterinariaAPI.Repository.Interfaces;

public interface ICita
{

    string AgregarCita(CitaO obj, long clienteId);
    string ModificarCita(CitaO obj, long clienteId);
    string ActualizarEstadoCita(long idCita, string estado, long? clienteId = null);
    CitaO BuscarCita(long id);
    Cita BuscarCitaFront(long id);
    void EliminarCita(long id);

    // Historial Médico
    string AgregarHistorialMedico(HistorialMedicoDTO dto);
    HistorialMedico? ObtenerHistorialPorCita(long idCita);

    // Hold temporal
    long CrearHold(long clienteId, long vetId, DateTime fechaHora);
    long IntentarHold(long clienteId, long vetId, DateTime fechaHora);
    void LiberarHold(long holdId);
    void RenovarHold(long holdId);
    List<SlotSemana> ObtenerSlotsSemana(DateTime fechaInicio, long? vetId, long clienteId);
    void LimpiarHoldsExpirados();
}