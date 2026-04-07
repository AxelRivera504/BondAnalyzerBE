using BondAnalyzer.API.Models;
using BondAnalyzer.API.Models.DTOs;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BondAnalyzer.API.Services;

public class ExportService
{
    private readonly IBondCalculatorService _calculator;

    public ExportService(IBondCalculatorService calculator)
    {
        _calculator = calculator;
    }

    public byte[] ExportarExcel(Bono bono)
    {
        var cupon = _calculator.CalcularValorCupon(bono.ValorNominal, bono.TasaCupon, bono.CuponesPorAnio);
        var flujosOriginal = _calculator.GenerarFlujoCajaOriginal(bono);
        var flujosComprador = _calculator.GenerarFlujoCajaComprador(bono);
        var curva = _calculator.GenerarCurvaRentabilidadPrecio(bono,
            bono.TasaCupon - 0.04m, bono.TasaCupon + 0.06m, 0.01m);
        var gsp = _calculator.CalcularGSP(bono);
        var duracion = _calculator.CalcularDuracion(bono);

        using var wb = new XLWorkbook();

        // Hoja 1: Resumen
        var wsResumen = wb.Worksheets.Add("Resumen");
        AgregarEncabezadoHoja(wsResumen, $"Análisis de Bono: {bono.Nombre}");

        var filaResumen = 3;
        AgregarFilaDatos(wsResumen, filaResumen++, "Nombre del Bono", bono.Nombre);
        AgregarFilaDatos(wsResumen, filaResumen++, "Valor Nominal (V)", FormatearLempiras(bono.ValorNominal));
        AgregarFilaDatos(wsResumen, filaResumen++, "Tasa Cupón (i)", $"{bono.TasaCupon * 100:F2}%");
        AgregarFilaDatos(wsResumen, filaResumen++, "Plazo (n)", $"{bono.Plazo} años");
        AgregarFilaDatos(wsResumen, filaResumen++, "Cupones por año (m)", bono.CuponesPorAnio.ToString());
        AgregarFilaDatos(wsResumen, filaResumen++, "Valor del Cupón (C)", FormatearLempiras(cupon));
        AgregarFilaDatos(wsResumen, filaResumen++, "Rentabilidad Exigida (r)", $"{bono.RentabilidadExigida * 100:F2}%");
        filaResumen++;
        AgregarFilaDatos(wsResumen, filaResumen++, "Precio del Bono (P)", FormatearLempiras(bono.PrecioCalculado));
        AgregarFilaDatos(wsResumen, filaResumen++, "Tipo de Bono", _calculator.DeterminarTipoBono(bono.TasaCupon, bono.RentabilidadExigida));
        AgregarFilaDatos(wsResumen, filaResumen++, "TIR Bono Original", $"{bono.TirOriginal * 100:F4}%");
        AgregarFilaDatos(wsResumen, filaResumen++, "TIR Comprador", $"{bono.TirComprador * 100:F4}%");
        AgregarFilaDatos(wsResumen, filaResumen++, "Duración Macaulay", $"{bono.DuracionMacaulay:F4} años");
        AgregarFilaDatos(wsResumen, filaResumen++, "Duración Modificada", $"{bono.DuracionModificada:F4}");
        AgregarFilaDatos(wsResumen, filaResumen++, "GSP Absoluto", FormatearLempiras(bono.GspAbsoluto));
        AgregarFilaDatos(wsResumen, filaResumen++, "GSP Relativo", $"{bono.GspRelativo * 100:F4}%");
        AgregarFilaDatos(wsResumen, filaResumen++, "Fecha de Análisis", bono.FechaCreacion.ToString("dd/MM/yyyy HH:mm"));

        wsResumen.Columns().AdjustToContents();

        // Hoja 2: Flujo de Caja Original
        var wsFlujoOriginal = wb.Worksheets.Add("Flujo Caja Original");
        AgregarEncabezadoHoja(wsFlujoOriginal, "Flujo de Caja Original del Bono");
        AgregarTablaFlujoCaja(wsFlujoOriginal, flujosOriginal, 3);
        wsFlujoOriginal.Columns().AdjustToContents();

        // Hoja 3: Flujo de Caja Comprador
        var wsFlujoComprador = wb.Worksheets.Add("Flujo Caja Comprador");
        AgregarEncabezadoHoja(wsFlujoComprador, "Flujo de Caja del Comprador (Mercado Secundario)");
        AgregarTablaFlujoCaja(wsFlujoComprador, flujosComprador, 3);
        wsFlujoComprador.Columns().AdjustToContents();

        // Hoja 4: Curva Rentabilidad-Precio
        var wsCurva = wb.Worksheets.Add("Curva Rentabilidad-Precio");
        AgregarEncabezadoHoja(wsCurva, "Curva de Rentabilidad vs. Precio");
        var fila = 3;
        wsCurva.Cell(fila, 1).Value = "Rentabilidad Exigida";
        wsCurva.Cell(fila, 2).Value = "Precio del Bono";
        EstilarEncabezadoColumnas(wsCurva, fila, 2);
        fila++;
        foreach (var punto in curva)
        {
            wsCurva.Cell(fila, 1).Value = $"{punto.RentabilidadPorcentaje:F2}%";
            wsCurva.Cell(fila, 2).Value = punto.PrecioCalculado;
            wsCurva.Cell(fila, 2).Style.NumberFormat.Format = "#,##0.00";
            fila++;
        }
        wsCurva.Columns().AdjustToContents();

        // Hoja 5: GSP y Duración
        var wsGspDur = wb.Worksheets.Add("GSP y Duración");
        AgregarEncabezadoHoja(wsGspDur, "Grado de Sensibilidad Puntual y Duración");
        var filaGD = 3;
        wsGspDur.Cell(filaGD++, 1).Value = "--- GRADO DE SENSIBILIDAD PUNTUAL (GSP) ---";
        wsGspDur.Cell(filaGD - 1, 1).Style.Font.Bold = true;
        AgregarFilaDatos(wsGspDur, filaGD++, "Rentabilidad Base (r-1%)", $"{gsp.RentabilidadBasePorcentaje:F2}%");
        AgregarFilaDatos(wsGspDur, filaGD++, "Precio Base", FormatearLempiras(gsp.PrecioBase));
        AgregarFilaDatos(wsGspDur, filaGD++, "Rentabilidad Alterada (r)", $"{gsp.RentabilidadAlteradaPorcentaje:F2}%");
        AgregarFilaDatos(wsGspDur, filaGD++, "Precio Alterado", FormatearLempiras(gsp.PrecioAlterado));
        AgregarFilaDatos(wsGspDur, filaGD++, "GSP Absoluto (ΔY)", FormatearLempiras(gsp.GspAbsoluto));
        AgregarFilaDatos(wsGspDur, filaGD++, "GSP Relativo (ΔY/Y)", $"{gsp.GspRelativoPorcentaje:F4}%");
        AgregarFilaDatos(wsGspDur, filaGD++, "Interpretación", gsp.Interpretacion);
        filaGD++;
        wsGspDur.Cell(filaGD++, 1).Value = "--- DURACIÓN DEL BONO ---";
        wsGspDur.Cell(filaGD - 1, 1).Style.Font.Bold = true;
        AgregarFilaDatos(wsGspDur, filaGD++, "Duración de Macaulay", $"{duracion.DuracionMacaulay:F4} años");
        AgregarFilaDatos(wsGspDur, filaGD++, "Duración Modificada", $"{duracion.DuracionModificada:F4}");
        filaGD++;
        wsGspDur.Cell(filaGD, 1).Value = "Periodo";
        wsGspDur.Cell(filaGD, 2).Value = "Flujo";
        wsGspDur.Cell(filaGD, 3).Value = "Valor Presente";
        wsGspDur.Cell(filaGD, 4).Value = "t × VP";
        EstilarEncabezadoColumnas(wsGspDur, filaGD, 4);
        filaGD++;
        foreach (var det in duracion.Detalle)
        {
            wsGspDur.Cell(filaGD, 1).Value = det.Periodo;
            wsGspDur.Cell(filaGD, 2).Value = det.Flujo;
            wsGspDur.Cell(filaGD, 3).Value = det.ValorPresente;
            wsGspDur.Cell(filaGD, 4).Value = det.TPorVP;
            for (int c = 2; c <= 4; c++)
                wsGspDur.Cell(filaGD, c).Style.NumberFormat.Format = "#,##0.00";
            filaGD++;
        }
        wsGspDur.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportarPdf(Bono bono)
    {
        var cupon = _calculator.CalcularValorCupon(bono.ValorNominal, bono.TasaCupon, bono.CuponesPorAnio);
        var flujosOriginal = _calculator.GenerarFlujoCajaOriginal(bono);
        var flujosComprador = _calculator.GenerarFlujoCajaComprador(bono);
        var curva = _calculator.GenerarCurvaRentabilidadPrecio(bono,
            bono.TasaCupon - 0.04m, bono.TasaCupon + 0.06m, 0.01m);
        var gsp = _calculator.CalcularGSP(bono);
        var duracion = _calculator.CalcularDuracion(bono);
        var tipoBono = _calculator.DeterminarTipoBono(bono.TasaCupon, bono.RentabilidadExigida);

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(ComposeHeader);

                page.Content().Element(content =>
                {
                    content.Column(col =>
                    {
                        col.Spacing(12);

                        // Título principal
                        col.Item().Text($"Análisis de Bono: {bono.Nombre}")
                            .Bold().FontSize(16).FontColor(Colors.Blue.Darken2);

                        col.Item().Text($"Fecha: {bono.FechaCreacion:dd/MM/yyyy HH:mm}")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);

                        // Resumen datos del bono
                        col.Item().Text("1. Datos del Bono").Bold().FontSize(13);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });
                            AgregarFilaPdf(table, "Valor Nominal (V)", FormatearLempiras(bono.ValorNominal));
                            AgregarFilaPdf(table, "Tasa Cupón (i)", $"{bono.TasaCupon * 100:F2}%");
                            AgregarFilaPdf(table, "Plazo (n)", $"{bono.Plazo} años");
                            AgregarFilaPdf(table, "Cupones por año (m)", bono.CuponesPorAnio.ToString());
                            AgregarFilaPdf(table, "Valor del Cupón (C)", FormatearLempiras(cupon));
                            AgregarFilaPdf(table, "Rentabilidad Exigida (r)", $"{bono.RentabilidadExigida * 100:F2}%");
                            AgregarFilaPdf(table, "Precio del Bono (P)", FormatearLempiras(bono.PrecioCalculado), bold: true);
                            AgregarFilaPdf(table, "Tipo de Bono", tipoBono, bold: true);
                        });

                        // TIR
                        col.Item().Text("2. Tasa Interna de Retorno (TIR)").Bold().FontSize(13);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(2); });
                            AgregarFilaPdf(table, "TIR Bono Original", $"{bono.TirOriginal * 100:F4}%");
                            AgregarFilaPdf(table, "TIR Comprador", $"{bono.TirComprador * 100:F4}%", bold: true);
                        });

                        // Flujo Original
                        col.Item().Text("3. Flujo de Caja Original del Bono").Bold().FontSize(13);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(55);
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });
                            AgregarEncabezadoPdf(table, "Período", "Valor Compra", "Cupón", "Redención", "Flujo Neto");
                            foreach (var f in flujosOriginal.Flujos)
                            {
                                AgregarFilaFlujoPdf(table, f.Periodo.ToString(),
                                    f.ValorCompra.HasValue ? FormatearLempiras(f.ValorCompra.Value) : "-",
                                    f.Cupon.HasValue ? FormatearLempiras(f.Cupon.Value) : "-",
                                    f.ValorRedencion.HasValue ? FormatearLempiras(f.ValorRedencion.Value) : "-",
                                    FormatearLempiras(f.FlujoNeto));
                            }
                        });

                        // Flujo Comprador
                        col.Item().Text("4. Flujo de Caja del Comprador").Bold().FontSize(13);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(55);
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });
                            AgregarEncabezadoPdf(table, "Período", "Valor Compra", "Cupón", "Redención", "Flujo Neto");
                            foreach (var f in flujosComprador.Flujos)
                            {
                                AgregarFilaFlujoPdf(table, f.Periodo.ToString(),
                                    f.ValorCompra.HasValue ? FormatearLempiras(f.ValorCompra.Value) : "-",
                                    f.Cupon.HasValue ? FormatearLempiras(f.Cupon.Value) : "-",
                                    f.ValorRedencion.HasValue ? FormatearLempiras(f.ValorRedencion.Value) : "-",
                                    FormatearLempiras(f.FlujoNeto));
                            }
                        });

                        // Curva Rentabilidad-Precio
                        col.Item().Text("5. Curva Rentabilidad-Precio").Bold().FontSize(13);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                            AgregarEncabezadoPdf(table, "Rentabilidad Exigida", "Precio del Bono");
                            foreach (var punto in curva)
                            {
                                AgregarFilaFlujoPdf(table,
                                    $"{punto.RentabilidadPorcentaje:F2}%",
                                    FormatearLempiras(punto.PrecioCalculado));
                            }
                        });

                        // GSP
                        col.Item().Text("6. Grado de Sensibilidad Puntual (GSP)").Bold().FontSize(13);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(2); });
                            AgregarFilaPdf(table, "Rentabilidad Base (r-1%)", $"{gsp.RentabilidadBasePorcentaje:F2}%");
                            AgregarFilaPdf(table, "Precio Base", FormatearLempiras(gsp.PrecioBase));
                            AgregarFilaPdf(table, "Rentabilidad Alterada (r)", $"{gsp.RentabilidadAlteradaPorcentaje:F2}%");
                            AgregarFilaPdf(table, "Precio Alterado", FormatearLempiras(gsp.PrecioAlterado));
                            AgregarFilaPdf(table, "GSP Absoluto (ΔY)", FormatearLempiras(gsp.GspAbsoluto), bold: true);
                            AgregarFilaPdf(table, "GSP Relativo (%)", $"{gsp.GspRelativoPorcentaje:F4}%", bold: true);
                            AgregarFilaPdf(table, "Interpretación", gsp.Interpretacion);
                        });

                        // Duración
                        col.Item().Text("7. Análisis de Duración").Bold().FontSize(13);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(2); });
                            AgregarFilaPdf(table, "Duración de Macaulay", $"{duracion.DuracionMacaulay:F4} años", bold: true);
                            AgregarFilaPdf(table, "Duración Modificada", $"{duracion.DuracionModificada:F4}", bold: true);
                        });
                        col.Item().Text(duracion.InterpretacionMacaulay).FontSize(9).FontColor(Colors.Grey.Darken2);
                        col.Item().Text(duracion.InterpretacionModificada).FontSize(9).FontColor(Colors.Grey.Darken2);
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Smart Finance — UNAH Campus Cortés | IS-820 Finanzas Administrativas | Pág. ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("Herramienta de Análisis de Rentabilidad y Riesgo de Bonos")
                    .Bold().FontSize(12).FontColor(Colors.Blue.Darken3);
                col.Item().Text("Universidad Nacional Autónoma de Honduras (UNAH) — Campus Cortés")
                    .FontSize(9).FontColor(Colors.Grey.Darken1);
                col.Item().Text("IS-820 Finanzas Administrativas | Grupo: Smart Finance")
                    .FontSize(9).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private static void AgregarEncabezadoHoja(IXLWorksheet ws, string titulo)
    {
        ws.Cell(1, 1).Value = titulo;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.DarkBlue;
        ws.Cell(2, 1).Value = "Smart Finance | UNAH Campus Cortés | IS-820";
        ws.Cell(2, 1).Style.Font.Italic = true;
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
    }

    private static void AgregarFilaDatos(IXLWorksheet ws, int fila, string etiqueta, string valor)
    {
        ws.Cell(fila, 1).Value = etiqueta;
        ws.Cell(fila, 1).Style.Font.Bold = true;
        ws.Cell(fila, 2).Value = valor;
    }

    private static void EstilarEncabezadoColumnas(IXLWorksheet ws, int fila, int columnas)
    {
        for (int c = 1; c <= columnas; c++)
        {
            ws.Cell(fila, c).Style.Font.Bold = true;
            ws.Cell(fila, c).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }
    }

    private static void AgregarTablaFlujoCaja(IXLWorksheet ws, FlujoCajaResponseDto flujos, int filaInicio)
    {
        ws.Cell(filaInicio, 1).Value = "Período";
        ws.Cell(filaInicio, 2).Value = "Valor Compra";
        ws.Cell(filaInicio, 3).Value = "Cupón";
        ws.Cell(filaInicio, 4).Value = "Valor Redención";
        ws.Cell(filaInicio, 5).Value = "Flujo Neto";
        EstilarEncabezadoColumnas(ws, filaInicio, 5);

        var fila = filaInicio + 1;
        foreach (var f in flujos.Flujos)
        {
            ws.Cell(fila, 1).Value = f.Periodo;
            if (f.ValorCompra.HasValue) { ws.Cell(fila, 2).Value = (double)f.ValorCompra.Value; ws.Cell(fila, 2).Style.NumberFormat.Format = "#,##0.00"; }
            else ws.Cell(fila, 2).Value = "—";
            if (f.Cupon.HasValue) { ws.Cell(fila, 3).Value = (double)f.Cupon.Value; ws.Cell(fila, 3).Style.NumberFormat.Format = "#,##0.00"; }
            else ws.Cell(fila, 3).Value = "—";
            if (f.ValorRedencion.HasValue) { ws.Cell(fila, 4).Value = (double)f.ValorRedencion.Value; ws.Cell(fila, 4).Style.NumberFormat.Format = "#,##0.00"; }
            else ws.Cell(fila, 4).Value = "—";
            ws.Cell(fila, 5).Value = (double)f.FlujoNeto;
            ws.Cell(fila, 5).Style.NumberFormat.Format = "#,##0.00";
            fila++;
        }

        fila++;
        ws.Cell(fila, 1).Value = "TIR";
        ws.Cell(fila, 1).Style.Font.Bold = true;
        ws.Cell(fila, 2).Value = $"{flujos.TirPorcentaje:F4}%";
        ws.Cell(fila, 2).Style.Font.Bold = true;
    }

    private static void AgregarFilaPdf(TableDescriptor table, string etiqueta, string valor, bool bold = false)
    {
        table.Cell().Padding(4).Text(etiqueta).Bold();
        if (bold)
            table.Cell().Padding(4).Text(valor).Bold().FontColor(Colors.Blue.Darken2);
        else
            table.Cell().Padding(4).Text(valor);
    }

    private static void AgregarEncabezadoPdf(TableDescriptor table, params string[] columnas)
    {
        foreach (var col in columnas)
            table.Cell().Background(Colors.Blue.Lighten3).Padding(4).Text(col).Bold().FontSize(9);
    }

    private static void AgregarFilaFlujoPdf(TableDescriptor table, params string[] valores)
    {
        foreach (var val in valores)
            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(val).FontSize(9);
    }

    private static string FormatearLempiras(decimal valor)
        => $"L. {valor:N2}";
}
