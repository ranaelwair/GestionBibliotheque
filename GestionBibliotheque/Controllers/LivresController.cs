using GestionBibliotheque.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GestionBibliotheque.Controllers
{
    // CRUD des livres : TOUTES les opérations passent par des procédures stockées
    //  - lecture   : FromSqlInterpolated("EXEC ...")         -> retourne des entités Livre
    //  - écriture  : Database.ExecuteSqlInterpolatedAsync()  -> INSERT / UPDATE / DELETE
    public class LivresController : Controller
    {
        private readonly BibliothequeContext _context;

        public LivresController(BibliothequeContext context)
        {
            _context = context;
        }

        // Méthode utilitaire : un livre par son ID via sp_Livre_GetById
        private async Task<Livre?> GetLivreAsync(int id)
        {
            var resultat = await _context.Livre
                .FromSqlInterpolated($"EXEC dbo.sp_Livre_GetById @LivreID = {id}")
                .AsNoTracking()
                .ToListAsync();

            return resultat.FirstOrDefault();
        }

        // GET: Livres            (+ recherche : /Livres?recherche=camus)
        public async Task<IActionResult> Index(string? recherche)
        {
            ViewData["Recherche"] = recherche;

            var livres = await _context.Livre
                .FromSqlInterpolated($"EXEC dbo.sp_Livre_GetAll @Recherche = {recherche}")
                .AsNoTracking()
                .ToListAsync();

            return View(livres);
        }

        // GET: Livres/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var livre = await GetLivreAsync(id.Value);
            if (livre == null)
            {
                return NotFound();
            }

            // Historique des emprunts du livre (requête LINQ classique EF)
            ViewBag.Emprunts = await _context.Emprunt
                .Include(e => e.Membre)
                .Where(e => e.LivreID == id)
                .OrderByDescending(e => e.DateEmprunt)
                .AsNoTracking()
                .ToListAsync();

            return View(livre);
        }

        // GET: Livres/Create
        public IActionResult Create()
        {
            return View(new Livre { AnneePublication = DateTime.Today.Year, NbExemplaires = 1 });
        }

        // POST: Livres/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Titre,Auteur,ISBN,Categorie,AnneePublication,NbExemplaires")] Livre livre)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync($@"EXEC dbo.sp_Livre_Insert
                        @Titre = {livre.Titre}, @Auteur = {livre.Auteur}, @ISBN = {livre.ISBN},
                        @Categorie = {livre.Categorie}, @AnneePublication = {livre.AnneePublication},
                        @NbExemplaires = {livre.NbExemplaires}");

                    TempData["Succes"] = $"Le livre « {livre.Titre} » a été ajouté.";
                    return RedirectToAction(nameof(Index));
                }
                catch (SqlException ex)
                {
                    // Message levé par THROW dans la procédure (ex. ISBN en double)
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }
            return View(livre);
        }

        // GET: Livres/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var livre = await GetLivreAsync(id.Value);
            if (livre == null)
            {
                return NotFound();
            }
            return View(livre);
        }

        // POST: Livres/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("LivreID,Titre,Auteur,ISBN,Categorie,AnneePublication,NbExemplaires")] Livre livre)
        {
            if (id != livre.LivreID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync($@"EXEC dbo.sp_Livre_Update
                        @LivreID = {livre.LivreID}, @Titre = {livre.Titre}, @Auteur = {livre.Auteur},
                        @ISBN = {livre.ISBN}, @Categorie = {livre.Categorie},
                        @AnneePublication = {livre.AnneePublication}, @NbExemplaires = {livre.NbExemplaires}");

                    TempData["Succes"] = $"Le livre « {livre.Titre} » a été modifié.";
                    return RedirectToAction(nameof(Index));
                }
                catch (SqlException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }
            return View(livre);
        }

        // GET: Livres/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var livre = await GetLivreAsync(id.Value);
            if (livre == null)
            {
                return NotFound();
            }

            return View(livre);
        }

        // POST: Livres/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($"EXEC dbo.sp_Livre_Delete @LivreID = {id}");
                TempData["Succes"] = "Le livre a été supprimé.";
            }
            catch (SqlException ex)
            {
                // ex. « Impossible de supprimer : ce livre a des emprunts en cours. »
                TempData["Erreur"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
