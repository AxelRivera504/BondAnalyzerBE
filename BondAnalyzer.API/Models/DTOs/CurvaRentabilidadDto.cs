namespace BondAnalyzer.API.Models.DTOs;

public class CurvaRentabilidadDto
{
    public decimal RentabilidadExigida { get; set; }
    public decimal RentabilidadPorcentaje => RentabilidadExigida * 100;
    public decimal PrecioCalculado { get; set; }
}
