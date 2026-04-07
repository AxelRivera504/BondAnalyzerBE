namespace BondAnalyzer.API.Models.DTOs;

public class DuracionDto
{
    public decimal DuracionMacaulay { get; set; }
    public decimal DuracionModificada { get; set; }
    public decimal PrecioBase { get; set; }
    public decimal RentabilidadExigida { get; set; }
    public decimal RentabilidadExigidaPorcentaje => RentabilidadExigida * 100;
    public List<DuracionDetalleDto> Detalle { get; set; } = new();
    public string InterpretacionMacaulay { get; set; } = string.Empty;
    public string InterpretacionModificada { get; set; } = string.Empty;
}

public class DuracionDetalleDto
{
    public int Periodo { get; set; }
    public decimal Flujo { get; set; }
    public decimal ValorPresente { get; set; }
    public decimal TPorVP { get; set; }
}
