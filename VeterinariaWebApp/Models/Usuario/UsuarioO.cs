using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace VeterinariaWebApp.Models.Usuario;

public class UsuarioO
{
    [DisplayName("Correo")]
    [Required(ErrorMessage = "El campo correo es requerido")]
    [StringLength(100, ErrorMessage = "Mínimo 8, Máximo 100 caracteres", MinimumLength = 8)]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        ErrorMessage = "El formato del correo electrónico no es válido. Ejemplo: usuario@dominio.com")]
    public string? cor_usr { get; set; }

    [DisplayName("Contraseña")]
    [Required(ErrorMessage = "El campo de contraseña es requerido")]
    [StringLength(150, ErrorMessage = "Mínimo 8, Máximo 150 caracteres", MinimumLength = 8)]
    public string? pwd_usr { get; set; }

    [DisplayName("Nombre")]
    [Required(ErrorMessage = "El campo de Nombre es requerido")]
    [StringLength(100, ErrorMessage = "Mínimo 8, Máximo 100 caracteres", MinimumLength = 8)]
    public string? nom_usr { get; set; }

    [DisplayName("Apellido")]
    [Required(ErrorMessage = "El campo apellido es requerido")]
    [StringLength(150, ErrorMessage = "Mínimo 8, Máximo 150 caracteres", MinimumLength = 8)]
    public string? ape_usr { get; set; }

    [DisplayName("Fecha Nac.")]
    [Required(ErrorMessage = "El campo Fecha es requerido")]
    public DateTime fna_usr { get; set; }

    [DisplayName("Número de Doc.")]
    [StringLength(12, ErrorMessage = "Mínimo 8, Máximo 12 caracteres", MinimumLength = 8)]
    [Required(ErrorMessage = "El Número de Doc. es requerido")]
    public string? num_doc { get; set; }

    [DisplayName("Tipo de Documento")]
    [Required(ErrorMessage = "El Tipo de Documento es requerido")]
    public long ide_doc { get; set; }

    [DisplayName("Rol")]
    public long ide_rol { get; set; }
}