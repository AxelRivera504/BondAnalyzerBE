using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BondAnalyzer.API.Models;

public class Bono
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "DECIMAL(18,2)")]
    public decimal ValorNominal { get; set; }

    [Required]
    [Column(TypeName = "DECIMAL(10,6)")]
    public decimal TasaCupon { get; set; }

    [Required]
    public int Plazo { get; set; }

    public int CuponesPorAnio { get; set; } = 1;

    [Required]
    [Column(TypeName = "DECIMAL(10,6)")]
    public decimal RentabilidadExigida { get; set; }

    [Column(TypeName = "DECIMAL(18,2)")]
    public decimal PrecioCalculado { get; set; }

    [Column(TypeName = "DECIMAL(10,6)")]
    public decimal TirOriginal { get; set; }

    [Column(TypeName = "DECIMAL(10,6)")]
    public decimal TirComprador { get; set; }

    [Column(TypeName = "DECIMAL(10,4)")]
    public decimal DuracionMacaulay { get; set; }

    [Column(TypeName = "DECIMAL(10,4)")]
    public decimal DuracionModificada { get; set; }

    [Column(TypeName = "DECIMAL(18,2)")]
    public decimal GspAbsoluto { get; set; }

    [Column(TypeName = "DECIMAL(10,6)")]
    public decimal GspRelativo { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
