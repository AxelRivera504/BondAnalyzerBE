using BondAnalyzer.API.Models;
using BondAnalyzer.API.Models.DTOs;

namespace BondAnalyzer.API.Services;

public class BondCalculatorService : IBondCalculatorService
{
    // C = V × i / m
    public decimal CalcularValorCupon(decimal valorNominal, decimal tasaCupon, int cuponesPorAnio)
        => valorNominal * tasaCupon / cuponesPorAnio;

    // P = Σ C/(1+r)^t + V/(1+r)^n
    public decimal CalcularPrecioBono(decimal valorNominal, decimal tasaCupon, int plazo,
        decimal rentabilidadExigida, int cuponesPorAnio)
    {
        var cupon = CalcularValorCupon(valorNominal, tasaCupon, cuponesPorAnio);
        var periodosTotales = plazo * cuponesPorAnio;
        var tasaPeriodo = (double)(rentabilidadExigida / cuponesPorAnio);
        double precio = 0;

        for (int t = 1; t <= periodosTotales; t++)
            precio += (double)cupon / Math.Pow(1 + tasaPeriodo, t);

        precio += (double)valorNominal / Math.Pow(1 + tasaPeriodo, periodosTotales);

        return Math.Round((decimal)precio, 2);
    }

    public FlujoCajaResponseDto GenerarFlujoCajaOriginal(Bono bono)
    {
        var cupon = CalcularValorCupon(bono.ValorNominal, bono.TasaCupon, bono.CuponesPorAnio);
        var periodos = bono.Plazo * bono.CuponesPorAnio;
        var flujos = new List<FlujoCajaDto>();

        // Período 0: inversión inicial al valor nominal
        flujos.Add(new FlujoCajaDto
        {
            Periodo = 0,
            ValorCompra = -bono.ValorNominal,
            Cupon = null,
            ValorRedencion = null,
            FlujoNeto = -bono.ValorNominal
        });

        for (int t = 1; t <= periodos; t++)
        {
            var esUltimo = t == periodos;
            var redencion = esUltimo ? bono.ValorNominal : 0m;
            flujos.Add(new FlujoCajaDto
            {
                Periodo = t,
                ValorCompra = null,
                Cupon = cupon,
                ValorRedencion = esUltimo ? redencion : null,
                FlujoNeto = cupon + redencion
            });
        }

        var flujosDecimal = flujos.Select(f => f.FlujoNeto).ToList();
        var tir = CalcularTIR(flujosDecimal);

        return new FlujoCajaResponseDto
        {
            Flujos = flujos,
            Tir = tir,
            InversionInicial = bono.ValorNominal
        };
    }

    public FlujoCajaResponseDto GenerarFlujoCajaComprador(Bono bono)
    {
        var cupon = CalcularValorCupon(bono.ValorNominal, bono.TasaCupon, bono.CuponesPorAnio);
        var periodos = bono.Plazo * bono.CuponesPorAnio;
        var precio = bono.PrecioCalculado;
        var flujos = new List<FlujoCajaDto>();

        // Período 0: inversión inicial al precio de mercado
        flujos.Add(new FlujoCajaDto
        {
            Periodo = 0,
            ValorCompra = -precio,
            Cupon = null,
            ValorRedencion = null,
            FlujoNeto = -precio
        });

        for (int t = 1; t <= periodos; t++)
        {
            var esUltimo = t == periodos;
            var redencion = esUltimo ? bono.ValorNominal : 0m;
            flujos.Add(new FlujoCajaDto
            {
                Periodo = t,
                ValorCompra = null,
                Cupon = cupon,
                ValorRedencion = esUltimo ? redencion : null,
                FlujoNeto = cupon + redencion
            });
        }

        var flujosDecimal = flujos.Select(f => f.FlujoNeto).ToList();
        var tir = CalcularTIR(flujosDecimal);

        return new FlujoCajaResponseDto
        {
            Flujos = flujos,
            Tir = tir,
            InversionInicial = precio
        };
    }

    // TIR por método de bisección
    public decimal CalcularTIR(List<decimal> flujos)
    {
        const double tolerancia = 1e-10;
        const int maxIteraciones = 10000;

        double tasaBaja = 0.0;
        double tasaAlta = 10.0;  // límite superior 1000%

        double VPN(double tasa)
        {
            double vpn = 0;
            for (int t = 0; t < flujos.Count; t++)
                vpn += (double)flujos[t] / Math.Pow(1 + tasa, t);
            return vpn;
        }

        if (VPN(tasaBaja) < 0) return 0m;

        double tasaMid = 0;
        for (int i = 0; i < maxIteraciones; i++)
        {
            tasaMid = (tasaBaja + tasaAlta) / 2;
            double vpnMid = VPN(tasaMid);

            if (Math.Abs(vpnMid) < tolerancia || (tasaAlta - tasaBaja) / 2 < tolerancia)
                break;

            if (vpnMid > 0)
                tasaBaja = tasaMid;
            else
                tasaAlta = tasaMid;
        }

        return Math.Round((decimal)tasaMid, 6);
    }

    public List<CurvaRentabilidadDto> GenerarCurvaRentabilidadPrecio(Bono bono,
        decimal tasaInicio, decimal tasaFin, decimal paso)
    {
        var curva = new List<CurvaRentabilidadDto>();
        var tasa = tasaInicio;

        while (tasa <= tasaFin + 0.0001m)
        {
            var precio = CalcularPrecioBono(bono.ValorNominal, bono.TasaCupon,
                bono.Plazo, tasa, bono.CuponesPorAnio);

            curva.Add(new CurvaRentabilidadDto
            {
                RentabilidadExigida = Math.Round(tasa, 4),
                PrecioCalculado = precio
            });

            tasa += paso;
        }

        return curva;
    }

    public GspDto CalcularGSP(Bono bono)
    {
        // GSP: variación del precio cuando la rentabilidad sube 1%
        var tasaBase = bono.RentabilidadExigida - 0.01m;
        if (tasaBase <= 0) tasaBase = 0.001m;

        var precioBase = CalcularPrecioBono(bono.ValorNominal, bono.TasaCupon,
            bono.Plazo, tasaBase, bono.CuponesPorAnio);
        var precioAlterado = bono.PrecioCalculado; // precio con la tasa actual (r)

        var gspAbsoluto = precioAlterado - precioBase;
        var gspRelativo = precioBase != 0 ? gspAbsoluto / precioBase : 0m;

        var interpretacion = Math.Abs(gspRelativo * 100) < 3
            ? "Baja sensibilidad al cambio de tasas (riesgo bajo)"
            : Math.Abs(gspRelativo * 100) < 6
                ? "Sensibilidad moderada al cambio de tasas (riesgo moderado)"
                : "Alta sensibilidad al cambio de tasas (riesgo alto)";

        return new GspDto
        {
            RentabilidadBase = tasaBase,
            PrecioBase = precioBase,
            RentabilidadAlterada = bono.RentabilidadExigida,
            PrecioAlterado = precioAlterado,
            GspAbsoluto = Math.Round(gspAbsoluto, 2),
            GspRelativo = Math.Round(gspRelativo, 6),
            Interpretacion = interpretacion
        };
    }

    public DuracionDto CalcularDuracion(Bono bono)
    {
        var cupon = CalcularValorCupon(bono.ValorNominal, bono.TasaCupon, bono.CuponesPorAnio);
        var periodos = bono.Plazo * bono.CuponesPorAnio;
        var tasaPeriodo = (double)(bono.RentabilidadExigida / bono.CuponesPorAnio);
        var precio = (double)bono.PrecioCalculado;

        var detalle = new List<DuracionDetalleDto>();
        double sumaTxVP = 0;

        for (int t = 1; t <= periodos; t++)
        {
            var esUltimo = t == periodos;
            var flujo = esUltimo
                ? cupon + bono.ValorNominal
                : cupon;

            var vp = (double)flujo / Math.Pow(1 + tasaPeriodo, t);
            var tPorVP = t * vp;
            sumaTxVP += tPorVP;

            detalle.Add(new DuracionDetalleDto
            {
                Periodo = t,
                Flujo = flujo,
                ValorPresente = Math.Round((decimal)vp, 2),
                TPorVP = Math.Round((decimal)tPorVP, 2)
            });
        }

        var duracionMacaulay = precio > 0 ? sumaTxVP / precio : 0;
        var duracionModificada = duracionMacaulay / (1 + tasaPeriodo);

        return new DuracionDto
        {
            DuracionMacaulay = Math.Round((decimal)duracionMacaulay, 4),
            DuracionModificada = Math.Round((decimal)duracionModificada, 4),
            PrecioBase = bono.PrecioCalculado,
            RentabilidadExigida = bono.RentabilidadExigida,
            Detalle = detalle,
            InterpretacionMacaulay = $"En promedio, la inversión se recupera en {duracionMacaulay:F2} años, " +
                $"aunque el bono vence en {bono.Plazo} años. Los cupones intermedios reducen el tiempo efectivo de recuperación.",
            InterpretacionModificada = $"Una variación del 1% en la rentabilidad exigida genera una variación " +
                $"aproximada del {duracionModificada:F2}% en el precio del bono. " +
                (duracionModificada > 5
                    ? "Duración alta indica mayor sensibilidad al riesgo de tasas."
                    : "Duración moderada indica sensibilidad controlada al riesgo de tasas.")
        };
    }

    public string DeterminarTipoBono(decimal tasaCupon, decimal rentabilidadExigida)
    {
        if (rentabilidadExigida == tasaCupon) return "A la par";
        if (rentabilidadExigida < tasaCupon) return "Con prima (P > V)";
        return "Con descuento (P < V)";
    }
}
