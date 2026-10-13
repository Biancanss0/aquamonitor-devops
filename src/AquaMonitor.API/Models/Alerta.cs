using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AquaMonitor.API.Models;

[Table("AM_ALERTAS")]
public class Alerta
{
    [Key]
    [Column("ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("SENSOR_ID")]
    public int SensorId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("TIPO")]
    public string Tipo { get; set; } = string.Empty; // Vazamento, ConsumoExcessivo, PressaoCritica

    [Required]
    [MaxLength(500)]
    [Column("DESCRICAO")]
    public string Descricao { get; set; } = string.Empty;

    [Column("NIVEL")]
    [MaxLength(20)]
    public string Nivel { get; set; } = "MEDIO"; // BAIXO, MEDIO, ALTO, CRITICO

    [Column("RESOLVIDO")]
    public bool Resolvido { get; set; } = false;

    [Column("CRIADO_EM")]
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    [Column("RESOLVIDO_EM")]
    public DateTime? ResolvidoEm { get; set; }

    [ForeignKey("SensorId")]
    public Sensor? Sensor { get; set; }
}
