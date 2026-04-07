using BondAnalyzer.API.Models;
using BondAnalyzer.API.Models.DTOs;

namespace BondAnalyzer.API.Services;

public interface IBondCalculatorService
{
    decimal CalcularValorCupon(decimal valorNominal, decimal tasaCupon, int cuponesPorAnio);
    decimal CalcularPrecioBono(decimal valorNominal, decimal tasaCupon, int plazo, decimal rentabilidadExigida, int cuponesPorAnio);
    FlujoCajaResponseDto GenerarFlujoCajaOriginal(Bono bono);
    FlujoCajaResponseDto GenerarFlujoCajaComprador(Bono bono);
    decimal CalcularTIR(List<decimal> flujos);
    List<CurvaRentabilidadDto> GenerarCurvaRentabilidadPrecio(Bono bono, decimal tasaInicio, decimal tasaFin, decimal paso);
    GspDto CalcularGSP(Bono bono);
    DuracionDto CalcularDuracion(Bono bono);
    string DeterminarTipoBono(decimal tasaCupon, decimal rentabilidadExigida);
}
