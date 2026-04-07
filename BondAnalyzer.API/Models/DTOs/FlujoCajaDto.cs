namespace BondAnalyzer.API.Models.DTOs;

public class FlujoCajaDto
{
    public int Periodo { get; set; }
    public decimal? ValorCompra { get; set; }
    public decimal? Cupon { get; set; }
    public decimal? ValorRedencion { get; set; }
    public decimal FlujoNeto { get; set; }
    public decimal? ValorPresente { get; set; }
    public decimal? TPorVP { get; set; }
}

public class FlujoCajaResponseDto
{
    public List<FlujoCajaDto> Flujos { get; set; } = new();
    public decimal Tir { get; set; }
    public decimal TirPorcentaje => Tir * 100;
    public decimal InversionInicial { get; set; }
}
