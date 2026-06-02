using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace VeterinariaWebApp.Models.Mascota;

public class Mascota
{
    [DisplayName("ID")]
    public long IdMascota { get; set; }

    [DisplayName("Nombre")]
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 50 caracteres")]
    public string? Nombre { get; set; }

    [DisplayName("Especie")]
    [Required(ErrorMessage = "La especie es requerida")]
    [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
    public string? Especie { get; set; }

    [DisplayName("Raza")]
    [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
    public string? Raza { get; set; }

    [DisplayName("Fecha de Nacimiento")]
    [Required(ErrorMessage = "La fecha de nacimiento es requerida")]
    [DataType(DataType.Date)]
    public DateTime FechaNacimiento { get; set; }





    // Propiedad calculada para la edad
    public string Edad
    {
        get
        {
            var hoy = DateTime.Today;
            var años = hoy.Year - FechaNacimiento.Year;
            var meses = hoy.Month - FechaNacimiento.Month;
            if (hoy.Day < FechaNacimiento.Day) meses--;
            if (meses < 0) { años--; meses += 12; }
            if (años > 0) return años == 1 ? "1 año" : $"{años} años";
            else if (meses > 0) return meses == 1 ? "1 mes" : $"{meses} meses";
            else return "Recién nacido";
        }
    }
}