namespace BondAnalyzer.API.Models.DTOs;

public class ComparacionRequestDto
{
    public List<int> Ids { get; set; } = new();
}

public class ComparacionDto
{
    public List<BonoComparacionItemDto> Bonos { get; set; } = new();
    public List<CurvaRentabilidadDto>[] CurvasPorBono { get; set; } = Array.Empty<List<CurvaRentabilidadDto>>();
}

public class BonoComparacionItemDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal ValorNominal { get; set; }
    public decimal TasaCupon { get; set; }
    public decimal TasaCuponPorcentaje => TasaCupon * 100;
    public decimal RentabilidadExigida { get; set; }
    public decimal RentabilidadExigidaPorcentaje => RentabilidadExigida * 100;
    public decimal PrecioCalculado { get; set; }
    public decimal TirComprador { get; set; }
    public decimal TirCompradorPorcentaje => TirComprador * 100;
    public decimal GspAbsoluto { get; set; }
    public decimal GspRelativo { get; set; }
    public decimal GspRelativoPorcentaje => GspRelativo * 100;
    public decimal DuracionMacaulay { get; set; }
    public decimal DuracionModificada { get; set; }
    public string TipoBono { get; set; } = string.Empty;
    public List<CurvaRentabilidadDto> CurvaRentabilidad { get; set; } = new();
}
