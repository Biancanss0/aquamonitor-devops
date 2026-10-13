using System.ComponentModel.DataAnnotations;

namespace AquaMonitor.API.ViewModels;

// ─── Sensor ───────────────────────────────────────────────────────────────────

public class SensorCreateViewModel
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Localização é obrigatória")]
    [MaxLength(200)]
    public string Localizacao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tipo é obrigatório")]
    [RegularExpression("Residencial|Comercial|Industrial", ErrorMessage = "Tipo deve ser Residencial, Comercial ou Industrial")]
    public string Tipo { get; set; } = string.Empty;

    [Range(0.1, 1000000, ErrorMessage = "Limite de consumo deve ser maior que 0")]
    public double LimiteConsumo { get; set; }
}

public class SensorUpdateViewModel : SensorCreateViewModel
{
    public bool Ativo { get; set; }
}

public class SensorResponseViewModel
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Localizacao { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public double LimiteConsumo { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
}

// ─── Leitura ──────────────────────────────────────────────────────────────────

public class LeituraCreateViewModel
{
    [Required(ErrorMessage = "SensorId é obrigatório")]
    public int SensorId { get; set; }

    [Required]
    [Range(0.01, 10000000, ErrorMessage = "Consumo deve ser maior que 0")]
    public double ConsumoLitros { get; set; }

    [Range(0, 100)]
    public double? PressaoBar { get; set; }

    [Range(-50, 150)]
    public double? TemperaturaCelsius { get; set; }
}

public class LeituraResponseViewModel
{
    public int Id { get; set; }
    public int SensorId { get; set; }
    public string SensorNome { get; set; } = string.Empty;
    public double ConsumoLitros { get; set; }
    public double? PressaoBar { get; set; }
    public double? TemperaturaCelsius { get; set; }
    public bool VazamentoDetectado { get; set; }
    public DateTime RegistradoEm { get; set; }
}

// ─── Alerta ───────────────────────────────────────────────────────────────────

public class AlertaResponseViewModel
{
    public int Id { get; set; }
    public int SensorId { get; set; }
    public string SensorNome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Nivel { get; set; } = string.Empty;
    public bool Resolvido { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? ResolvidoEm { get; set; }
}

// ─── Relatório ────────────────────────────────────────────────────────────────

public class RelatorioSensorViewModel
{
    public int SensorId { get; set; }
    public string SensorNome { get; set; } = string.Empty;
    public string Localizacao { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public double LimiteConsumo { get; set; }
    public double ConsumoTotal { get; set; }
    public double ConsumoMedio { get; set; }
    public double ConsumoMaximo { get; set; }
    public int TotalLeituras { get; set; }
    public int TotalAlertas { get; set; }
    public int AlertasNaoResolvidos { get; set; }
    public double PercentualDoLimite { get; set; }
    public string StatusConsumo { get; set; } = string.Empty; // Normal, Atenção, Critico
}

public class RelatorioGeralViewModel
{
    public DateTime PeriodoInicio { get; set; }
    public DateTime PeriodoFim { get; set; }
    public int TotalSensores { get; set; }
    public double ConsumoTotalLitros { get; set; }
    public int TotalAlertas { get; set; }
    public int AlertasNaoResolvidos { get; set; }
    public int VazamentosDetectados { get; set; }
    public List<RelatorioSensorViewModel> DetalhesPorSensor { get; set; } = new();
}

// ─── Auth ─────────────────────────────────────────────────────────────────────

public class LoginViewModel
{
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória")]
    public string Senha { get; set; } = string.Empty;
}

public class RegistroViewModel
{
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "Senha deve ter no mínimo 6 caracteres")]
    public string Senha { get; set; } = string.Empty;
}

public class AuthResponseViewModel
{
    public string Token { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public DateTime Expiracao { get; set; }
}

// ─── Paginação ────────────────────────────────────────────────────────────────

public class PagedResultViewModel<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalRegistros { get; set; }
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
    public int TotalPaginas => (int)Math.Ceiling((double)TotalRegistros / TamanhoPagina);
    public bool TemProxima => Pagina < TotalPaginas;
    public bool TemAnterior => Pagina > 1;
}
