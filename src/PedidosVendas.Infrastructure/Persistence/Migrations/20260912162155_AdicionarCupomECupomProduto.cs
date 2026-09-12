using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedidosVendas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCupomECupomProduto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cupom",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ValorDesconto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PercDesconto = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ValorMinimoCompra = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IdCategoria = table.Column<Guid>(type: "uuid", nullable: true),
                    DataValidade = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    IsCupomProduto = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cupom", x => x.Id);
                    table.CheckConstraint("CK_Cupom_DescontoExclusivo", "(\"ValorDesconto\" > 0 AND \"PercDesconto\" = 0) OR (\"PercDesconto\" > 0 AND \"ValorDesconto\" = 0)");
                    table.CheckConstraint("CK_Cupom_QuantidadeNaoNegativa", "\"Quantidade\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "CupomProduto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdCupom = table.Column<Guid>(type: "uuid", nullable: false),
                    IdProduto = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CupomProduto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CupomProduto_Cupom_IdCupom",
                        column: x => x.IdCupom,
                        principalTable: "Cupom",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cupom_TenantId",
                table: "Cupom",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Cupom_TenantId_DataValidade",
                table: "Cupom",
                columns: new[] { "TenantId", "DataValidade" });

            migrationBuilder.CreateIndex(
                name: "IX_CupomProduto_IdCupom_IdProduto",
                table: "CupomProduto",
                columns: new[] { "IdCupom", "IdProduto" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CupomProduto_IdProduto",
                table: "CupomProduto",
                column: "IdProduto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CupomProduto");

            migrationBuilder.DropTable(
                name: "Cupom");
        }
    }
}
