using System.ComponentModel.DataAnnotations;

namespace BondAnalyzer.API.Models.DTOs;

public class BonoCreateDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "El valor nominal debe ser mayor a 0")]
    public decimal ValorNominal { get; set; }

    [Required]
    [Range(0.0001, 1.0, ErrorMessage = "La tasa cupón debe estar entre 0% y 100%")]
    public decimal TasaCupon { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "El plazo debe ser al menos 1 año")]
    public int Plazo { get; set; }

    [Range(1, 12, ErrorMessage = "Los cupones por año deben ser 1, 2, 4 o 12")]
    public int CuponesPorAnio { get; set; } = 1;

    [Required]
    [Range(0.0001, 0.9999, ErrorMessage = "La rentabilidad exigida debe estar entre 0% y 100%")]
    public decimal RentabilidadExigida { get; set; }
}
