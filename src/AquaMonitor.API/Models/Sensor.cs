using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AquaMonitor.API.Models;

[Table("AM_SENSORES")]
public class Sensor
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
    [MaxLength(200)]
    [Column("LOCALIZACAO")]
    public string Localizacao { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("TIPO")]
    public string Tipo { get; set; } = string.Empty; // Residencial, Comercial, Industrial

    [Column("LIMITE_CONSUMO")]
    public double LimiteConsumo { get; set; } // em litros

    [Column("ATIVO")]
    public bool Ativo { get; set; } = true;

    [Column("CRIADO_EM")]
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public ICollection<Leitura> Leituras { get; set; } = new List<Leitura>();
    public ICollection<Alerta> Alertas { get; set; } = new List<Alerta>();
}
