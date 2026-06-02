using VeterinariaAPI.Models.Pago;

namespace VeterinariaAPI.Repository.Interfaces;

public interface IPayOpts
{
    IEnumerable<PayOpts> ListarTiposDePago();
}