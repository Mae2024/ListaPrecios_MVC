using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ListaPrecios_MVC.Migrations
{
    /// <inheritdoc />
    public partial class Vendedores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VendedorId",
                table: "Pedidos",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendedorId",
                table: "Clientes",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_VendedorId",
                table: "Pedidos",
                column: "VendedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_VendedorId",
                table: "Clientes",
                column: "VendedorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_AspNetUsers_VendedorId",
                table: "Clientes",
                column: "VendedorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_AspNetUsers_VendedorId",
                table: "Pedidos",
                column: "VendedorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_AspNetUsers_VendedorId",
                table: "Clientes");

            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_AspNetUsers_VendedorId",
                table: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_VendedorId",
                table: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_VendedorId",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "VendedorId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "VendedorId",
                table: "Clientes");
        }
    }
}
