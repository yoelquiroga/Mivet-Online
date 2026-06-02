using System.ComponentModel.DataAnnotations;

namespace VeterinariaWebApp.Models.Cita;

public class FutureDateAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is DateTime fecha)
        {
            if (fecha <= DateTime.Now)
            {
                return new ValidationResult(ErrorMessage ?? "La fecha debe ser futura.");
            }
        }
        return ValidationResult.Success;
    }
}