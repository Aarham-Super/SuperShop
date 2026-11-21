using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using SuperShop.Data;

#nullable disable
namespace SuperShop.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261019120000_AddSizesAndDemoOrderHistory")]
    public partial class AddSizesAndDemoOrderHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "UserId", table: "DemoOrders", type: "varchar(450)", maxLength: 450, nullable: true).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.CreateIndex(name: "IX_DemoOrders_UserId", table: "DemoOrders", column: "UserId");
            migrationBuilder.CreateTable(name: "ProductSizes", columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                ProductId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false).Annotation("MySql:CharSet", "utf8mb4")
            }, constraints: table =>
            {
                table.PrimaryKey("PK_ProductSizes", x => x.Id);
                table.ForeignKey("FK_ProductSizes_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Cascade);
            }).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.CreateIndex(name: "IX_ProductSizes_ProductId", table: "ProductSizes", column: "ProductId");
            migrationBuilder.CreateTable(name: "DemoOrderItems", columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                DemoOrderId = table.Column<int>(type: "int", nullable: false),
                ProductName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                Size = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                Quantity = table.Column<int>(type: "int", nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
            }, constraints: table =>
            {
                table.PrimaryKey("PK_DemoOrderItems", x => x.Id);
                table.ForeignKey("FK_DemoOrderItems_DemoOrders_DemoOrderId", x => x.DemoOrderId, "DemoOrders", "Id", onDelete: ReferentialAction.Cascade);
            }).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.CreateIndex(name: "IX_DemoOrderItems_DemoOrderId", table: "DemoOrderItems", column: "DemoOrderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DemoOrderItems");
            migrationBuilder.DropTable(name: "ProductSizes");
            migrationBuilder.DropIndex(name: "IX_DemoOrders_UserId", table: "DemoOrders");
            migrationBuilder.DropColumn(name: "UserId", table: "DemoOrders");
        }
    }
}
