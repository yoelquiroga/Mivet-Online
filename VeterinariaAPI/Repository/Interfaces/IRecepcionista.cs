using VeterinariaAPI.Models.Usuario.Recepcionista;

namespace VeterinariaAPI.Repository.Interfaces;

public interface IRecepcionista
{
    IEnumerable<Recepcionista> ListarRecepcionistasFront();
    IEnumerable<RecepcionistaO> ListarRecepcionistasBack();
    string AgregarRecepcionista(RecepcionistaO recepcionista);
    Recepcionista BuscarRecepcionistaPorID(long id);
    string ActualizarRecepcionistaPorID(RecepcionistaO recepcionista);
    string EliminarRecepcionistaPorID(long id);
}