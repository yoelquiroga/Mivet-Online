using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace VeterinariaWebApp.Models.Usuario.Cliente;

public class RegistroClienteViewModel
{
    [DisplayName("Correo electrónico")]
    [Required(ErrorMessage = "El correo es requerido")]
    [EmailAddress(ErrorMessage = "Ingrese un correo válido")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
    public string? cor_usr { get; set; }

    [DisplayName("Contraseña")]
    [Required(ErrorMessage = "La contraseña es requerida")]
    [StringLength(150, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 150 caracteres")]
    [DataType(DataType.Password)]
    public string? pwd_usr { get; set; }

    [DisplayName("Confirmar contraseña")]
    [Required(ErrorMessage = "Confirme su contraseña")]
    [Compare("pwd_usr", ErrorMessage = "Las contraseñas no coinciden")]
    [DataType(DataType.Password)]
    public string? ConfirmarPassword { get; set; }

    [DisplayName("Nombres")]
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    public string? nom_usr { get; set; }

    [DisplayName("Apellidos")]
    [Required(ErrorMessage = "El apellido es requerido")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 100 caracteres")]
    public string? ape_usr { get; set; }

    [DisplayName("Fecha de nacimiento")]
    [Required(ErrorMessage = "La fecha de nacimiento es requerida")]
    [DataType(DataType.Date)]
    public DateTime fna_usr { get; set; }

    [DisplayName("Número de documento")]
    [Required(ErrorMessage = "El número de documento es requerido")]
    [StringLength(12, MinimumLength = 8, ErrorMessage = "El documento debe tener entre 8 y 12 caracteres")]
    [RegularExpression(@"^\d+$", ErrorMessage = "Solo se permiten números")]
    public string? num_doc { get; set; }

    [DisplayName("Tipo de documento")]
    [Required(ErrorMessage = "Seleccione un tipo de documento")]
    [Range(1, long.MaxValue, ErrorMessage = "Seleccione un tipo de documento")]
    public long ide_doc { get; set; }
}
