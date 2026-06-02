using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace VeterinariaWebApp.Models.Usuario.Cliente;

public class PerfilClienteViewModel
{
    public long ide_cli { get; set; }
    public long ide_usr { get; set; }

    [DisplayName("Correo electrónico")]
    public string? cor_usr { get; set; } 

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
    public long ide_doc { get; set; }

    // Para mostrar el nombre del tipo de documento
    public string? nom_doc { get; set; }

  
    public string? pwd_usr { get; set; }

    public long ide_rol { get; set; } = 1; // Cliente
}
