using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedidosVendas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarFormaPagtoEConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FormaPagto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    QtdMaximaParcelas = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormaPagto", x => x.Id);
                    table.CheckConstraint("CK_FormaPagto_QtdMinima", "\"QtdMaximaParcelas\" >= 1");
                });

            migrationBuilder.CreateTable(
                name: "TenantFormaPagtoConfig",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdFormaPagto = table.Column<Guid>(type: "uuid", nullable: false),
                    QtdMaximaParcelasOverride = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantFormaPagtoConfig", x => x.Id);
                    table.CheckConstraint("CK_TenantFormaPagtoConfig_QtdMinima", "\"QtdMaximaParcelasOverride\" >= 1");
                    table.ForeignKey(
                        name: "FK_TenantFormaPagtoConfig_FormaPagto_IdFormaPagto",
                        column: x => x.IdFormaPagto,
                        principalTable: "FormaPagto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FormaPagto_Descricao",
                table: "FormaPagto",
                column: "Descricao",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantFormaPagtoConfig_IdFormaPagto",
                table: "TenantFormaPagtoConfig",
                column: "IdFormaPagto");

            migrationBuilder.CreateIndex(
                name: "IX_TenantFormaPagtoConfig_TenantId",
                table: "TenantFormaPagtoConfig",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantFormaPagtoConfig_TenantId_IdFormaPagto",
                table: "TenantFormaPagtoConfig",
                columns: new[] { "TenantId", "IdFormaPagto" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantFormaPagtoConfig");

            migrationBuilder.DropTable(
                name: "FormaPagto");
        }
    }
}
