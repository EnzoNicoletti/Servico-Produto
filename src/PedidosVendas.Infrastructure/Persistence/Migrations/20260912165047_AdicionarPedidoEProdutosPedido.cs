using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PedidosVendas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarPedidoEProdutosPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pedido",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataAbertura = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IdCliente = table.Column<Guid>(type: "uuid", nullable: true),
                    IdCupom = table.Column<Guid>(type: "uuid", nullable: true),
                    IdUnidade = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DataFechamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedido", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProdutosPedido",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdPedido = table.Column<Guid>(type: "uuid", nullable: false),
                    IdProduto = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutosPedido", x => x.Id);
                    table.CheckConstraint("CK_ProdutosPedido_QuantidadePositiva", "\"Quantidade\" > 0");
                    table.ForeignKey(
                        name: "FK_ProdutosPedido_Pedido_IdPedido",
                        column: x => x.IdPedido,
                        principalTable: "Pedido",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_TenantId_IdCliente_Status",
                table: "Pedido",
                columns: new[] { "TenantId", "IdCliente", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosPedido_IdPedido_IdProduto",
                table: "ProdutosPedido",
                columns: new[] { "IdPedido", "IdProduto" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosPedido_IdProduto",
                table: "ProdutosPedido",
                column: "IdProduto");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProdutosPedido");

            migrationBuilder.DropTable(
                name: "Pedido");
        }
    }
}
