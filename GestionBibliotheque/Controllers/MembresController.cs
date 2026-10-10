using GestionBibliotheque.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionBibliotheque.Controllers
{
    // CRUD des membres : contrôleur généré automatiquement
    // (« Contrôleur MVC avec vues, utilisant Entity Framework »), légèrement adapté.
    public class MembresController : Controller
    {
        private readonly BibliothequeContext _context;

        public MembresController(BibliothequeContext context)
        {
            _context = context;
        }

        // GET: Membres
        public async Task<IActionResult> Index()
        {
            return View(await _context.Membre
                .Include(m => m.Emprunts)
                .OrderBy(m => m.Nom).ThenBy(m => m.Prenom)
                .AsNoTracking()
                .ToListAsync());
        }

        // GET: Membres/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var membre = await _context.Membre
                .Include(m => m.Emprunts!)
                    .ThenInclude(e => e.Livre)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MembreID == id);
            if (membre == null)
            {
                return NotFound();
            }

            return View(membre);
        }

        // GET: Membres/Create
        public IActionResult Create()
        {
            return View(new Membre { DateInscription = DateTime.Today });
        }

        // POST: Membres/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nom,Prenom,Courriel,DateInscription")] Membre membre)
        {
            if (ModelState.IsValid)
            {
                _context.Add(membre);
                await _context.SaveChangesAsync();
                TempData["Succes"] = $"Le membre {membre.NomComplet} a été ajouté.";
                return RedirectToAction(nameof(Index));
            }
            return View(membre);
        }

        // GET: Membres/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var membre = await _context.Membre.FindAsync(id);
            if (membre == null)
            {
                return NotFound();
            }
            return View(membre);
        }

        // POST: Membres/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MembreID,Nom,Prenom,Courriel,DateInscription")] Membre membre)
        {
            if (id != membre.MembreID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(membre);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MembreExists(membre.MembreID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                TempData["Succes"] = $"Le membre {membre.NomComplet} a été modifié.";
                return RedirectToAction(nameof(Index));
            }
            return View(membre);
        }

        // GET: Membres/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var membre = await _context.Membre
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MembreID == id);
            if (membre == null)
            {
                return NotFound();
            }

            return View(membre);
        }

        // POST: Membres/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Règle : on ne supprime pas un membre qui a un historique d'emprunts
            if (await _context.Emprunt.AnyAsync(e => e.MembreID == id))
            {
                TempData["Erreur"] = "Impossible de supprimer ce membre : il possède un historique d'emprunts.";
                return RedirectToAction(nameof(Index));
            }

            var membre = await _context.Membre.FindAsync(id);
            if (membre != null)
            {
                _context.Membre.Remove(membre);
                await _context.SaveChangesAsync();
                TempData["Succes"] = "Le membre a été supprimé.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool MembreExists(int id)
        {
            return _context.Membre.Any(e => e.MembreID == id);
        }
    }
}
