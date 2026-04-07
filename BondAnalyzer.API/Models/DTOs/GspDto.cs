namespace BondAnalyzer.API.Models.DTOs;

public class GspDto
{
    public decimal RentabilidadBase { get; set; }
    public decimal RentabilidadBasePorcentaje => RentabilidadBase * 100;
    public decimal PrecioBase { get; set; }
    public decimal RentabilidadAlterada { get; set; }
    public decimal RentabilidadAlteradaPorcentaje => RentabilidadAlterada * 100;
    public decimal PrecioAlterado { get; set; }
    public decimal GspAbsoluto { get; set; }
    public decimal GspRelativo { get; set; }
    public decimal GspRelativoPorcentaje => GspRelativo * 100;
    public string Interpretacion { get; set; } = string.Empty;
}
