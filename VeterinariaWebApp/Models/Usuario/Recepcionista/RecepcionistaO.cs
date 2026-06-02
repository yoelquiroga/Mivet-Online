using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace VeterinariaWebApp.Models.Usuario.Recepcionista;

public class RecepcionistaO : UsuarioO
{
    [DisplayName("ID")]
    public long ide_rep { get; set; }

    [DisplayName("Sueldo")]
    [Required(ErrorMessage = "El Sueldo es requerido")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El sueldo debe ser mayor a 0")]
    [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Solo se permiten números con hasta 2 decimales")]
    public decimal sue_rep { get; set; }

    [DisplayName("ID Usr.")]
    public long ide_usr { get; set; }
}