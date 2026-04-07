using BondAnalyzer.API.Data;
using BondAnalyzer.API.Models;
using BondAnalyzer.API.Models.DTOs;
using BondAnalyzer.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BondAnalyzer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BonosController : ControllerBase
{
    private readonly BondDbContext _context;
    private readonly IBondCalculatorService _calculator;
    private readonly ExportService _exportService;

    public BonosController(BondDbContext context, IBondCalculatorService calculator, ExportService exportService)
    {
        _context = context;
        _calculator = calculator;
        _exportService = exportService;
    }

    // POST /api/bonos — Crear y calcular nuevo escenario
    [HttpPost]
    public async Task<ActionResult<BonoResponseDto>> CrearBono([FromBody] BonoCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var precio = _calculator.CalcularPrecioBono(
            dto.ValorNominal, dto.TasaCupon, dto.Plazo,
            dto.RentabilidadExigida, dto.CuponesPorAnio);

        var bono = new Bono
        {
            Nombre = dto.Nombre,
            ValorNominal = dto.ValorNominal,
            TasaCupon = dto.TasaCupon,
            Plazo = dto.Plazo,
            CuponesPorAnio = dto.CuponesPorAnio,
            RentabilidadExigida = dto.RentabilidadExigida,
            PrecioCalculado = precio
        };

        // Calcular TIR original
        var flujosOriginal = _calculator.GenerarFlujoCajaOriginal(bono);
        bono.TirOriginal = flujosOriginal.Tir;

        // Calcular TIR comprador
        var flujosComprador = _calculator.GenerarFlujoCajaComprador(bono);
        bono.TirComprador = flujosComprador.Tir;

        // Calcular duración
        var duracion = _calculator.CalcularDuracion(bono);
        bono.DuracionMacaulay = duracion.DuracionMacaulay;
        bono.DuracionModificada = duracion.DuracionModificada;

        // Calcular GSP
        var gsp = _calculator.CalcularGSP(bono);
        bono.GspAbsoluto = gsp.GspAbsoluto;
        bono.GspRelativo = gsp.GspRelativo;

        _context.Bonos.Add(bono);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ObtenerBono), new { id = bono.Id }, MapearResponse(bono));
    }

    // GET /api/bonos — Listar todos los escenarios
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BonoResponseDto>>> ListarBonos()
    {
        var bonos = await _context.Bonos
            .OrderByDescending(b => b.FechaCreacion)
            .ToListAsync();

        return Ok(bonos.Select(MapearResponse));
    }

    // GET /api/bonos/{id} — Obtener un escenario específico
    [HttpGet("{id}")]
    public async Task<ActionResult<BonoResponseDto>> ObtenerBono(int id)
    {
        var bono = await _context.Bonos.FindAsync(id);
        if (bono is null) return NotFound($"No se encontró el bono con Id {id}");
        return Ok(MapearResponse(bono));
    }

    // DELETE /api/bonos/{id} — Eliminar un escenario
    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarBono(int id)
    {
        var bono = await _context.Bonos.FindAsync(id);
        if (bono is null) return NotFound($"No se encontró el bono con Id {id}");
        _context.Bonos.Remove(bono);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // GET /api/bonos/{id}/flujo-original
    [HttpGet("{id}/flujo-original")]
    public async Task<ActionResult<FlujoCajaResponseDto>> FlujoOriginal(int id)
    {
        var bono = await _context.Bonos.FindAsync(id);
        if (bono is null) return NotFound();
        return Ok(_calculator.GenerarFlujoCajaOriginal(bono));
    }

    // GET /api/bonos/{id}/flujo-comprador
    [HttpGet("{id}/flujo-comprador")]
    public async Task<ActionResult<FlujoCajaResponseDto>> FlujoComprador(int id)
    {
        var bono = await _context.Bonos.FindAsync(id);
        if (bono is null) return NotFound();
        return Ok(_calculator.GenerarFlujoCajaComprador(bono));
    }

    // GET /api/bonos/{id}/curva-rentabilidad
    [HttpGet("{id}/curva-rentabilidad")]
    public async Task<ActionResult<List<CurvaRentabilidadDto>>> CurvaRentabilidad(int id)
    {
        var bono = await _context.Bonos.FindAsync(id);
        if (bono is null) return NotFound();

        var tasaInicio = bono.TasaCupon - 0.04m;
        if (tasaInicio <= 0) tasaInicio = 0.01m;
        var tasaFin = bono.TasaCupon + 0.06m;

        var curva = _calculator.GenerarCurvaRentabilidadPrecio(bono, tasaInicio, tasaFin, 0.01m);
        return Ok(curva);
    }

    // GET /api/bonos/{id}/gsp
    [HttpGet("{id}/gsp")]
    public async Task<ActionResult<GspDto>> ObtenerGsp(int id)
    {
        var bono = await _context.Bonos.FindAsync(id);
        if (bono is null) return NotFound();
        return Ok(_calculator.CalcularGSP(bono));
    }

    // GET /api/bonos/{id}/duracion
    [HttpGet("{id}/duracion")]
    public async Task<ActionResult<DuracionDto>> ObtenerDuracion(int id)
    {
        var bono = await _context.Bonos.FindAsync(id);
        if (bono is null) return NotFound();
        return Ok(_calculator.CalcularDuracion(bono));
    }

    // POST /api/bonos/comparar — Comparar múltiples escenarios
    [HttpPost("comparar")]
    public async Task<ActionResult<ComparacionDto>> CompararBonos([FromBody] ComparacionRequestDto request)
    {
        if (request.Ids == null || request.Ids.Count < 2)
            return BadRequest("Se requieren al menos 2 IDs para comparar.");

        var bonos = await _context.Bonos
            .Where(b => request.Ids.Contains(b.Id))
            .ToListAsync();

        if (bonos.Count != request.Ids.Count)
            return NotFound("Uno o más bonos no fueron encontrados.");

        var items = bonos.Select(b =>
        {
            var tasaInicio = b.TasaCupon - 0.04m;
            if (tasaInicio <= 0) tasaInicio = 0.01m;
            var curva = _calculator.GenerarCurvaRentabilidadPrecio(b, tasaInicio, b.TasaCupon + 0.06m, 0.01m);

            return new BonoComparacionItemDto
            {
                Id = b.Id,
                Nombre = b.Nombre,
                ValorNominal = b.ValorNominal,
                TasaCupon = b.TasaCupon,
                RentabilidadExigida = b.RentabilidadExigida,
                PrecioCalculado = b.PrecioCalculado,
                TirComprador = b.TirComprador,
                GspAbsoluto = b.GspAbsoluto,
                GspRelativo = b.GspRelativo,
                DuracionMacaulay = b.DuracionMacaulay,
                DuracionModificada = b.DuracionModificada,
                TipoBono = _calculator.DeterminarTipoBono(b.TasaCupon, b.RentabilidadExigida),
                CurvaRentabilidad = curva
            };
        }).ToList();

        return Ok(new ComparacionDto { Bonos = items });
    }

    // GET /api/bonos/{id}/exportar/excel
    [HttpGet("{id}/exportar/excel")]
    public async Task<IActionResult> ExportarExcel(int id)
    {
        var bono = await _context.Bonos.FindAsync(id);
        if (bono is null) return NotFound();

        var bytes = _exportService.ExportarExcel(bono);
        var nombreArchivo = $"Bono_{bono.Nombre.Replace(" ", "_")}_{bono.Id}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
    }

    // GET /api/bonos/{id}/exportar/pdf
    [HttpGet("{id}/exportar/pdf")]
    public async Task<IActionResult> ExportarPdf(int id)
    {
        var bono = await _context.Bonos.FindAsync(id);
        if (bono is null) return NotFound();

        var bytes = _exportService.ExportarPdf(bono);
        var nombreArchivo = $"Bono_{bono.Nombre.Replace(" ", "_")}_{bono.Id}.pdf";
        return File(bytes, "application/pdf", nombreArchivo);
    }

    private BonoResponseDto MapearResponse(Bono bono)
    {
        var cupon = _calculator.CalcularValorCupon(bono.ValorNominal, bono.TasaCupon, bono.CuponesPorAnio);
        return new BonoResponseDto
        {
            Id = bono.Id,
            Nombre = bono.Nombre,
            ValorNominal = bono.ValorNominal,
            TasaCupon = bono.TasaCupon,
            Plazo = bono.Plazo,
            CuponesPorAnio = bono.CuponesPorAnio,
            RentabilidadExigida = bono.RentabilidadExigida,
            ValorCupon = cupon,
            PrecioCalculado = bono.PrecioCalculado,
            TirOriginal = bono.TirOriginal,
            TirComprador = bono.TirComprador,
            DuracionMacaulay = bono.DuracionMacaulay,
            DuracionModificada = bono.DuracionModificada,
            GspAbsoluto = bono.GspAbsoluto,
            GspRelativo = bono.GspRelativo,
            TipoBono = _calculator.DeterminarTipoBono(bono.TasaCupon, bono.RentabilidadExigida),
            FechaCreacion = bono.FechaCreacion
        };
    }
}
