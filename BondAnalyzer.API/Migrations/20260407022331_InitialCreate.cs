using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BondAnalyzer.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bonos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ValorNominal = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                    TasaCupon = table.Column<decimal>(type: "DECIMAL(10,6)", nullable: false),
                    Plazo = table.Column<int>(type: "int", nullable: false),
                    CuponesPorAnio = table.Column<int>(type: "int", nullable: false),
                    RentabilidadExigida = table.Column<decimal>(type: "DECIMAL(10,6)", nullable: false),
                    PrecioCalculado = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                    TirOriginal = table.Column<decimal>(type: "DECIMAL(10,6)", nullable: false),
                    TirComprador = table.Column<decimal>(type: "DECIMAL(10,6)", nullable: false),
                    DuracionMacaulay = table.Column<decimal>(type: "DECIMAL(10,4)", nullable: false),
                    DuracionModificada = table.Column<decimal>(type: "DECIMAL(10,4)", nullable: false),
                    GspAbsoluto = table.Column<decimal>(type: "DECIMAL(18,2)", nullable: false),
                    GspRelativo = table.Column<decimal>(type: "DECIMAL(10,6)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "DATETIME2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bonos", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bonos");
        }
    }
}
