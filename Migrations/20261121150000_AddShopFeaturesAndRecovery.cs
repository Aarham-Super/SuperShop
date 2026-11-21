using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using SuperShop.Data;

#nullable disable
namespace SuperShop.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261121150000_AddShopFeaturesAndRecovery")]
    public partial class AddShopFeaturesAndRecovery : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "PromotionCode", table: "DemoOrders", type: "varchar(32)", maxLength: 32, nullable: true).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<decimal>(name: "DiscountAmount", table: "DemoOrders", type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m);

            migrationBuilder.CreateTable(name: "PasswordResetCodes", columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                CodeHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                Salt = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UsedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                FailedAttempts = table.Column<int>(type: "int", nullable: false)
            }, constraints: table =>
            {
                table.PrimaryKey("PK_PasswordResetCodes", x => x.Id);
                table.ForeignKey("FK_PasswordResetCodes_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
            }).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.CreateIndex(name: "IX_PasswordResetCodes_UserId_CreatedAt", table: "PasswordResetCodes", columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateTable(name: "Promotions", columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                Description = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                DiscountPercent = table.Column<int>(type: "int", nullable: false),
                StartsAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                EndsAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                MaxUses = table.Column<int>(type: "int", nullable: true),
                Uses = table.Column<int>(type: "int", nullable: false)
            }, constraints: table => table.PrimaryKey("PK_Promotions", x => x.Id)).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.CreateIndex(name: "IX_Promotions_Code", table: "Promotions", column: "Code", unique: true);

            migrationBuilder.CreateTable(name: "WishlistItems", columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                ProductId = table.Column<int>(type: "int", nullable: false),
                AddedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            }, constraints: table =>
            {
                table.PrimaryKey("PK_WishlistItems", x => x.Id);
                table.ForeignKey("FK_WishlistItems_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_WishlistItems_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Cascade);
            }).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.CreateIndex(name: "IX_WishlistItems_ProductId", table: "WishlistItems", column: "ProductId");
            migrationBuilder.CreateIndex(name: "IX_WishlistItems_UserId_ProductId", table: "WishlistItems", columns: new[] { "UserId", "ProductId" }, unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("PasswordResetCodes");
            migrationBuilder.DropTable("Promotions");
            migrationBuilder.DropTable("WishlistItems");
            migrationBuilder.DropColumn("PromotionCode", "DemoOrders");
            migrationBuilder.DropColumn("DiscountAmount", "DemoOrders");
        }
    }
}
