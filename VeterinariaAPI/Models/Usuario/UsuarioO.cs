namespace VeterinariaAPI.Models.Usuario;

public class UsuarioO
{
    public string? cor_usr { get; set; }
    public string? pwd_usr { get; set; }
    public string? nom_usr { get; set; }
    public string? ape_usr { get; set; }
    public DateTime fna_usr { get; set; }
    public string? num_doc { get; set; }
    public long ide_doc { get; set; }
    public long ide_rol { get; set; }
}