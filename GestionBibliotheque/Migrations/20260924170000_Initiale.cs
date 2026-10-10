using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionBibliotheque.Migrations
{
    /// <inheritdoc />
    public partial class Initiale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Livre",
                columns: table => new
                {
                    LivreID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Auteur = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ISBN = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Categorie = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AnneePublication = table.Column<int>(type: "int", nullable: false),
                    NbExemplaires = table.Column<int>(type: "int", nullable: false),
                    NbDisponibles = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Livre", x => x.LivreID);
                });

            migrationBuilder.CreateTable(
                name: "Membre",
                columns: table => new
                {
                    MembreID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Prenom = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Courriel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateInscription = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Membre", x => x.MembreID);
                });

            migrationBuilder.CreateTable(
                name: "Emprunt",
                columns: table => new
                {
                    EmpruntID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LivreID = table.Column<int>(type: "int", nullable: false),
                    MembreID = table.Column<int>(type: "int", nullable: false),
                    DateEmprunt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateRetourPrevue = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateRetourEffective = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Penalite = table.Column<decimal>(type: "decimal(8,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emprunt", x => x.EmpruntID);
                    table.ForeignKey(
                        name: "FK_Emprunt_Livre_LivreID",
                        column: x => x.LivreID,
                        principalTable: "Livre",
                        principalColumn: "LivreID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Emprunt_Membre_MembreID",
                        column: x => x.MembreID,
                        principalTable: "Membre",
                        principalColumn: "MembreID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Emprunt_LivreID",
                table: "Emprunt",
                column: "LivreID");

            migrationBuilder.CreateIndex(
                name: "IX_Emprunt_MembreID",
                table: "Emprunt",
                column: "MembreID");

            migrationBuilder.CreateIndex(
                name: "IX_Livre_ISBN",
                table: "Livre",
                column: "ISBN",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Emprunt");

            migrationBuilder.DropTable(
                name: "Livre");

            migrationBuilder.DropTable(
                name: "Membre");
        }
    }
}
