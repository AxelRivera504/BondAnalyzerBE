namespace BondAnalyzer.API.Models.DTOs;

public class BonoResponseDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal ValorNominal { get; set; }
    public decimal TasaCupon { get; set; }
    public decimal TasaCuponPorcentaje => TasaCupon * 100;
    public int Plazo { get; set; }
    public int CuponesPorAnio { get; set; }
    public decimal RentabilidadExigida { get; set; }
    public decimal RentabilidadExigidaPorcentaje => RentabilidadExigida * 100;
    public decimal ValorCupon { get; set; }
    public decimal PrecioCalculado { get; set; }
    public decimal TirOriginal { get; set; }
    public decimal TirOriginalPorcentaje => TirOriginal * 100;
    public decimal TirComprador { get; set; }
    public decimal TirCompradorPorcentaje => TirComprador * 100;
    public decimal DuracionMacaulay { get; set; }
    public decimal DuracionModificada { get; set; }
    public decimal GspAbsoluto { get; set; }
    public decimal GspRelativo { get; set; }
    public decimal GspRelativoPorcentaje => GspRelativo * 100;
    public string TipoBono { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}
