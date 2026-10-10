using System.Data;
using GestionBibliotheque.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GestionBibliotheque.Controllers
{
    // Contrôleur des FONCTIONNALITÉS AJOUTÉES (au-delà du CRUD) :
    //   Fonctionnalité 1 : Emprunter un livre                      -> sp_EmprunterLivre
    //   Fonctionnalité 2 : Retourner un livre + calcul de pénalité -> sp_RetournerLivre
    //   Bonus            : Rapport des emprunts en retard          -> sp_EmpruntsEnRetard
    public class EmpruntsController : Controller
    {
        // Règles d'affaires
        private const decimal TarifRetardParJour = 0.25m;   // 0,25 $ par jour de retard (plafond 10 $ dans la SP)

        private readonly BibliothequeContext _context;

        public EmpruntsController(BibliothequeContext context)
        {
            _context = context;
        }

        // GET: Emprunts            (?filtre=encours | retard | tous)
        public async Task<IActionResult> Index(string filtre = "encours")
        {
            ViewData["Filtre"] = filtre;

            var requete = _context.Emprunt
                .Include(e => e.Livre)
                .Include(e => e.Membre)
                .AsNoTracking();

            var aujourdhui = DateTime.Today;
            requete = filtre switch
            {
                "tous"   => requete,
                "retard" => requete.Where(e => e.DateRetourEffective == null && e.DateRetourPrevue < aujourdhui),
                _        => requete.Where(e => e.DateRetourEffective == null)
            };

            var emprunts = await requete
                .OrderBy(e => e.DateRetourEffective != null)   // en cours d'abord
                .ThenBy(e => e.DateRetourPrevue)
                .ToListAsync();

            return View(emprunts);
        }

        // =====================================================================
        // FONCTIONNALITÉ 1 : EMPRUNTER UN LIVRE
        // =====================================================================

        // GET: Emprunts/Emprunter      (?livreId=3 pour présélectionner un livre)
        public async Task<IActionResult> Emprunter(int? livreId)
        {
            await RemplirListesAsync(livreId, null);
            return View(new EmprunterViewModel { LivreID = livreId });
        }

        // POST: Emprunts/Emprunter
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Emprunter(EmprunterViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await RemplirListesAsync(vm.LivreID, vm.MembreID);
                return View(vm);
            }

            // Paramètres de sortie (OUTPUT) de la procédure stockée
            var pMessage = new SqlParameter("@Message", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Output };
            var pSucces = new SqlParameter("@Succes", SqlDbType.Bit) { Direction = ParameterDirection.Output };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_EmprunterLivre @LivreID, @MembreID, @DureeJours, @Message OUTPUT, @Succes OUTPUT",
                new SqlParameter("@LivreID", vm.LivreID!.Value),
                new SqlParameter("@MembreID", vm.MembreID!.Value),
                new SqlParameter("@DureeJours", vm.DureeJours),
                pMessage,
                pSucces);

            bool succes = pSucces.Value is bool b && b;
            string message = pMessage.Value as string ?? "Erreur inconnue.";

            if (!succes)
            {
                // Une règle d'affaires a refusé l'emprunt : on réaffiche le formulaire
                ModelState.AddModelError(string.Empty, message);
                await RemplirListesAsync(vm.LivreID, vm.MembreID);
                return View(vm);
            }

            TempData["Succes"] = message;
            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // FONCTIONNALITÉ 2 : RETOURNER UN LIVRE (avec calcul de la pénalité)
        // =====================================================================

        // POST: Emprunts/Retourner/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Retourner(int id, string? retour)
        {
            var pPenalite = new SqlParameter("@Penalite", SqlDbType.Decimal)
            {
                Precision = 8,
                Scale = 2,
                Direction = ParameterDirection.Output
            };
            var pMessage = new SqlParameter("@Message", SqlDbType.NVarChar, 200) { Direction = ParameterDirection.Output };
            var pSucces = new SqlParameter("@Succes", SqlDbType.Bit) { Direction = ParameterDirection.Output };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC dbo.sp_RetournerLivre @EmpruntID, @TarifParJour, @Penalite OUTPUT, @Message OUTPUT, @Succes OUTPUT",
                new SqlParameter("@EmpruntID", id),
                new SqlParameter("@TarifParJour", TarifRetardParJour),
                pPenalite,
                pMessage,
                pSucces);

            bool succes = pSucces.Value is bool b && b;
            string message = pMessage.Value as string ?? "Erreur inconnue.";

            TempData[succes ? "Succes" : "Erreur"] = message;

            // Revenir sur la page d'où l'on vient (liste des emprunts ou rapport des retards)
            return retour == "retards"
                ? RedirectToAction(nameof(Retards))
                : RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // BONUS : RAPPORT DES EMPRUNTS EN RETARD
        // =====================================================================

        // GET: Emprunts/Retards
        public async Task<IActionResult> Retards()
        {
            // SqlQuery<T> (EF Core 8) : résultat d'une SP vers une classe qui n'est pas une entité
            var retards = await _context.Database
                .SqlQuery<EmpruntRetard>($"EXEC dbo.sp_EmpruntsEnRetard @TarifParJour = {TarifRetardParJour}")
                .ToListAsync();

            ViewData["TotalPenalites"] = retards.Sum(r => r.PenaliteEstimee);
            return View(retards);
        }

        // ---------------------------------------------------------------------
        // Listes déroulantes du formulaire « Emprunter »
        // ---------------------------------------------------------------------
        private async Task RemplirListesAsync(int? livreId, int? membreId)
        {
            var livres = (await _context.Livre
                    .Where(l => l.NbDisponibles > 0)
                    .OrderBy(l => l.Titre)
                    .AsNoTracking()
                    .ToListAsync())
                .Select(l => new { l.LivreID, Libelle = $"{l.Titre} — {l.Auteur} ({l.NbDisponibles} dispo.)" });

            var membres = (await _context.Membre
                    .OrderBy(m => m.Nom).ThenBy(m => m.Prenom)
                    .AsNoTracking()
                    .ToListAsync())
                .Select(m => new { m.MembreID, Libelle = m.NomComplet });

            ViewData["LivreID"] = new SelectList(livres, "LivreID", "Libelle", livreId);
            ViewData["MembreID"] = new SelectList(membres, "MembreID", "Libelle", membreId);
        }
    }
}
