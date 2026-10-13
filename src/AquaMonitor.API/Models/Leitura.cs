using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AquaMonitor.API.Models;

[Table("AM_LEITURAS")]
public class Leitura
{
    [Key]
    [Column("ID")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("SENSOR_ID")]
    public int SensorId { get; set; }

    [Required]
    [Column("CONSUMO_LITROS")]
    public double ConsumoLitros { get; set; }

    [Column("PRESSAO_BAR")]
    public double? PressaoBar { get; set; }

    [Column("TEMPERATURA_CELSIUS")]
    public double? TemperaturaCelsius { get; set; }

    [Column("REGISTRADO_EM")]
    public DateTime RegistradoEm { get; set; } = DateTime.UtcNow;

    [Column("VAZAMENTO_DETECTADO")]
    public bool VazamentoDetectado { get; set; } = false;

    [ForeignKey("SensorId")]
    public Sensor? Sensor { get; set; }
}
