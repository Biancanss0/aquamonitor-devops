using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AquaMonitor.API.Models;

[Table("AM_USUARIOS")]
public class Usuario
{
    [Key]
    [Column("ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("NOME")]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    [Column("EMAIL")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Column("SENHA_HASH")]
    public string SenhaHash { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("PERFIL")]
    public string Perfil { get; set; } = "Operador"; // Admin, Operador

    [Column("CRIADO_EM")]
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
