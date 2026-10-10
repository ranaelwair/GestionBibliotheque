using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionBibliotheque.Migrations
{
    /// <summary>
    /// Migration ajoutée à la main (Add-Migration ProceduresStockees) :
    /// crée les procédures stockées utilisées par les contrôleurs.
    /// </summary>
    public partial class ProceduresStockees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---------- sp_Livre_GetAll ----------
            migrationBuilder.Sql(@"CREATE OR ALTER PROCEDURE dbo.sp_Livre_GetAll
    @Recherche NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT LivreID, Titre, Auteur, ISBN, Categorie, AnneePublication, NbExemplaires, NbDisponibles
    FROM dbo.Livre
    WHERE @Recherche IS NULL OR @Recherche = N''
       OR Titre     LIKE N'%' + @Recherche + N'%'
       OR Auteur    LIKE N'%' + @Recherche + N'%'
       OR Categorie LIKE N'%' + @Recherche + N'%'
       OR ISBN      LIKE N'%' + @Recherche + N'%'
    ORDER BY Titre;
END");

            // ---------- sp_Livre_GetById ----------
            migrationBuilder.Sql(@"CREATE OR ALTER PROCEDURE dbo.sp_Livre_GetById
    @LivreID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT LivreID, Titre, Auteur, ISBN, Categorie, AnneePublication, NbExemplaires, NbDisponibles
    FROM dbo.Livre
    WHERE LivreID = @LivreID;
END");

            // ---------- sp_Livre_Insert ----------
            migrationBuilder.Sql(@"CREATE OR ALTER PROCEDURE dbo.sp_Livre_Insert
    @Titre            NVARCHAR(150),
    @Auteur           NVARCHAR(100),
    @ISBN             NVARCHAR(20),
    @Categorie        NVARCHAR(50) = NULL,
    @AnneePublication INT,
    @NbExemplaires    INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Livre WHERE ISBN = @ISBN)
        THROW 50001, N'Un livre avec cet ISBN existe déjà.', 1;

    -- À la création, tous les exemplaires sont disponibles
    INSERT INTO dbo.Livre (Titre, Auteur, ISBN, Categorie, AnneePublication, NbExemplaires, NbDisponibles)
    VALUES (@Titre, @Auteur, @ISBN, @Categorie, @AnneePublication, @NbExemplaires, @NbExemplaires);
END");

            // ---------- sp_Livre_Update ----------
            migrationBuilder.Sql(@"CREATE OR ALTER PROCEDURE dbo.sp_Livre_Update
    @LivreID          INT,
    @Titre            NVARCHAR(150),
    @Auteur           NVARCHAR(100),
    @ISBN             NVARCHAR(20),
    @Categorie        NVARCHAR(50) = NULL,
    @AnneePublication INT,
    @NbExemplaires    INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @EnCours INT;

    IF NOT EXISTS (SELECT 1 FROM dbo.Livre WHERE LivreID = @LivreID)
        THROW 50002, N'Livre introuvable.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Livre WHERE ISBN = @ISBN AND LivreID <> @LivreID)
        THROW 50001, N'Un autre livre possède déjà cet ISBN.', 1;

    SELECT @EnCours = COUNT(*)
    FROM dbo.Emprunt
    WHERE LivreID = @LivreID AND DateRetourEffective IS NULL;

    IF @NbExemplaires < @EnCours
        THROW 50003, N'Le nombre d''exemplaires ne peut pas être inférieur au nombre d''emprunts en cours.', 1;

    -- NbDisponibles est recalculé : exemplaires - emprunts en cours
    UPDATE dbo.Livre
    SET Titre = @Titre,
        Auteur = @Auteur,
        ISBN = @ISBN,
        Categorie = @Categorie,
        AnneePublication = @AnneePublication,
        NbExemplaires = @NbExemplaires,
        NbDisponibles = @NbExemplaires - @EnCours
    WHERE LivreID = @LivreID;
END");

            // ---------- sp_Livre_Delete ----------
            migrationBuilder.Sql(@"CREATE OR ALTER PROCEDURE dbo.sp_Livre_Delete
    @LivreID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF EXISTS (SELECT 1 FROM dbo.Emprunt WHERE LivreID = @LivreID AND DateRetourEffective IS NULL)
        THROW 50004, N'Impossible de supprimer : ce livre a des emprunts en cours.', 1;

    BEGIN TRANSACTION;
        DELETE FROM dbo.Emprunt WHERE LivreID = @LivreID;   -- historique des emprunts terminés
        DELETE FROM dbo.Livre   WHERE LivreID = @LivreID;
    COMMIT TRANSACTION;
END");

            // ---------- sp_EmprunterLivre ----------
            migrationBuilder.Sql(@"CREATE OR ALTER PROCEDURE dbo.sp_EmprunterLivre
    @LivreID    INT,
    @MembreID   INT,
    @DureeJours INT = 14,
    @Message    NVARCHAR(200) OUTPUT,
    @Succes     BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Succes = 0;
    DECLARE @Aujourdhui DATE = CAST(GETDATE() AS DATE);
    DECLARE @DatePrevue DATE = DATEADD(DAY, @DureeJours, @Aujourdhui);

    -- Règle 1 : le livre existe et au moins un exemplaire est disponible
    IF NOT EXISTS (SELECT 1 FROM dbo.Livre WHERE LivreID = @LivreID)
    BEGIN
        SET @Message = N'Livre introuvable.';
        RETURN;
    END

    IF (SELECT NbDisponibles FROM dbo.Livre WHERE LivreID = @LivreID) <= 0
    BEGIN
        SET @Message = N'Aucun exemplaire disponible pour ce livre.';
        RETURN;
    END

    -- Règle 2 : le membre existe
    IF NOT EXISTS (SELECT 1 FROM dbo.Membre WHERE MembreID = @MembreID)
    BEGIN
        SET @Message = N'Membre introuvable.';
        RETURN;
    END

    -- Règle 3 : le membre n'a pas déjà ce livre en main
    IF EXISTS (SELECT 1 FROM dbo.Emprunt
               WHERE MembreID = @MembreID AND LivreID = @LivreID AND DateRetourEffective IS NULL)
    BEGIN
        SET @Message = N'Ce membre a déjà emprunté ce livre.';
        RETURN;
    END

    -- Règle 4 : aucun emprunt en retard
    IF EXISTS (SELECT 1 FROM dbo.Emprunt
               WHERE MembreID = @MembreID AND DateRetourEffective IS NULL
                 AND CAST(DateRetourPrevue AS DATE) < @Aujourdhui)
    BEGIN
        SET @Message = N'Refusé : ce membre a au moins un emprunt en retard.';
        RETURN;
    END

    -- Règle 5 : maximum 3 emprunts en cours
    IF (SELECT COUNT(*) FROM dbo.Emprunt
        WHERE MembreID = @MembreID AND DateRetourEffective IS NULL) >= 3
    BEGIN
        SET @Message = N'Refusé : limite de 3 emprunts en cours atteinte.';
        RETURN;
    END

    -- Tout est valide : on enregistre dans une transaction
    BEGIN TRANSACTION;

        UPDATE dbo.Livre
        SET NbDisponibles = NbDisponibles - 1
        WHERE LivreID = @LivreID AND NbDisponibles > 0;

        IF @@ROWCOUNT = 0   -- un autre utilisateur a pris le dernier exemplaire
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Message = N'Aucun exemplaire disponible pour ce livre.';
            RETURN;
        END

        INSERT INTO dbo.Emprunt (LivreID, MembreID, DateEmprunt, DateRetourPrevue, DateRetourEffective, Penalite)
        VALUES (@LivreID, @MembreID, GETDATE(), @DatePrevue, NULL, 0);

    COMMIT TRANSACTION;

    SET @Succes = 1;
    SET @Message = N'Emprunt enregistré. Retour prévu le ' + CONVERT(NVARCHAR(10), @DatePrevue, 23) + N'.';
END");

            // ---------- sp_RetournerLivre ----------
            migrationBuilder.Sql(@"CREATE OR ALTER PROCEDURE dbo.sp_RetournerLivre
    @EmpruntID    INT,
    @TarifParJour DECIMAL(8,2) = 0.25,
    @Penalite     DECIMAL(8,2) OUTPUT,
    @Message      NVARCHAR(200) OUTPUT,
    @Succes       BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Succes = 0;
    SET @Penalite = 0;

    DECLARE @LivreID INT, @DatePrevue DATE, @DejaRetourne DATETIME2, @JoursRetard INT;

    SELECT @LivreID      = LivreID,
           @DatePrevue   = CAST(DateRetourPrevue AS DATE),
           @DejaRetourne = DateRetourEffective
    FROM dbo.Emprunt
    WHERE EmpruntID = @EmpruntID;

    IF @LivreID IS NULL
    BEGIN
        SET @Message = N'Emprunt introuvable.';
        RETURN;
    END

    IF @DejaRetourne IS NOT NULL
    BEGIN
        SET @Message = N'Ce livre a déjà été retourné.';
        RETURN;
    END

    -- Calcul de la pénalité : tarif x jours de retard, plafonnée à 10,00 $
    SET @JoursRetard = DATEDIFF(DAY, @DatePrevue, CAST(GETDATE() AS DATE));
    IF @JoursRetard < 0 SET @JoursRetard = 0;

    SET @Penalite = @JoursRetard * @TarifParJour;
    IF @Penalite > 10.00 SET @Penalite = 10.00;

    BEGIN TRANSACTION;

        UPDATE dbo.Emprunt
        SET DateRetourEffective = GETDATE(),
            Penalite = @Penalite
        WHERE EmpruntID = @EmpruntID;

        UPDATE dbo.Livre
        SET NbDisponibles = NbDisponibles + 1
        WHERE LivreID = @LivreID;

    COMMIT TRANSACTION;

    SET @Succes = 1;
    IF @JoursRetard > 0
        SET @Message = N'Livre retourné avec ' + CAST(@JoursRetard AS NVARCHAR(10))
                     + N' jour(s) de retard. Pénalité : ' + REPLACE(CONVERT(NVARCHAR(20), @Penalite), '.', ',') + N' $.';
    ELSE
        SET @Message = N'Livre retourné à temps. Aucune pénalité.';
END");

            // ---------- sp_EmpruntsEnRetard ----------
            migrationBuilder.Sql(@"CREATE OR ALTER PROCEDURE dbo.sp_EmpruntsEnRetard
    @TarifParJour DECIMAL(8,2) = 0.25
AS
BEGIN
    SET NOCOUNT ON;

    SELECT e.EmpruntID,
           l.Titre,
           m.Prenom + N' ' + m.Nom AS NomMembre,
           m.Courriel,
           e.DateEmprunt,
           e.DateRetourPrevue,
           j.JoursRetard,
           CAST(CASE WHEN j.JoursRetard * @TarifParJour > 10.00 THEN 10.00
                     ELSE j.JoursRetard * @TarifParJour END AS DECIMAL(8,2)) AS PenaliteEstimee
    FROM dbo.Emprunt e
         INNER JOIN dbo.Livre  l ON l.LivreID  = e.LivreID
         INNER JOIN dbo.Membre m ON m.MembreID = e.MembreID
         CROSS APPLY (SELECT DATEDIFF(DAY, CAST(e.DateRetourPrevue AS DATE), CAST(GETDATE() AS DATE)) AS JoursRetard) j
    WHERE e.DateRetourEffective IS NULL
      AND j.JoursRetard > 0
    ORDER BY j.JoursRetard DESC;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_EmpruntsEnRetard;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_RetournerLivre;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_EmprunterLivre;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_Livre_Delete;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_Livre_Update;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_Livre_Insert;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_Livre_GetById;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_Livre_GetAll;");
        }
    }
}
