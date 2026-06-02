using VeterinariaAPI.Models.Pago;

namespace VeterinariaAPI.Repository.Interfaces;

public interface IPago
{
    IEnumerable<Pago> ListarPagos(int pagina = 1, int tamanoPagina = 50);
    IEnumerable<Pago> ListarPagosPorCliente(long id);
    long AgregarPago(PagoO pago, long token);
    PagoO ObtenerPagoPorId(long id);
    Pago ObtenerPagoPorIdFront(long id);
    string ActualizarPago(PagoO pago);
    string EliminarPago(long id, long userId);
    string ConfirmarPago(long idPago);
    bool? VerificarAutorizacion(long idPago);
}