using GestionBibliotheque.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionBibliotheque.Data
{
    // Insère quelques données de test au premier démarrage (si la BD est vide)
    public static class DbInitializer
    {
        public static void Initialize(BibliothequeContext context)
        {
            // Applique les migrations en attente (tables + procédures stockées).
            // Équivaut à « Update-Database » si vous l'avez oublié.
            context.Database.Migrate();

            if (context.Livre.Any())
            {
                return; // la BD contient déjà des données
            }

            var livres = new Livre[]
            {
                new Livre { Titre = "Le Petit Prince", Auteur = "Antoine de Saint-Exupéry", ISBN = "9782070612758", Categorie = "Roman", AnneePublication = 1943, NbExemplaires = 3, NbDisponibles = 2 },
                new Livre { Titre = "L'Étranger", Auteur = "Albert Camus", ISBN = "9782070360024", Categorie = "Roman", AnneePublication = 1942, NbExemplaires = 2, NbDisponibles = 1 },
                new Livre { Titre = "Clean Code", Auteur = "Robert C. Martin", ISBN = "9780132350884", Categorie = "Informatique", AnneePublication = 2008, NbExemplaires = 2, NbDisponibles = 2 },
                new Livre { Titre = "Le Survenant", Auteur = "Germaine Guèvremont", ISBN = "9782762137279", Categorie = "Littérature québécoise", AnneePublication = 1945, NbExemplaires = 1, NbDisponibles = 1 },
                new Livre { Titre = "Bases de données SQL Server", Auteur = "Jérôme Gabillaud", ISBN = "9782409012648", Categorie = "Informatique", AnneePublication = 2018, NbExemplaires = 1, NbDisponibles = 0 }
            };
            context.Livre.AddRange(livres);
            context.SaveChanges();

            var membres = new Membre[]
            {
                new Membre { Nom = "Tremblay", Prenom = "Julie", Courriel = "julie.tremblay@exemple.ca", DateInscription = DateTime.Today.AddYears(-1) },
                new Membre { Nom = "Benali", Prenom = "Karim", Courriel = "karim.benali@exemple.ca", DateInscription = DateTime.Today.AddMonths(-6) },
                new Membre { Nom = "Gagnon", Prenom = "Marc", Courriel = "marc.gagnon@exemple.ca", DateInscription = DateTime.Today.AddMonths(-2) }
            };
            context.Membre.AddRange(membres);
            context.SaveChanges();

            // 3 emprunts en cours (dont 2 en retard) + 1 emprunt déjà retourné.
            // NbDisponibles ci-dessus tient déjà compte des 3 emprunts en cours.
            var emprunts = new Emprunt[]
            {
                // En retard de 10 jours
                new Emprunt { LivreID = livres[0].LivreID, MembreID = membres[0].MembreID, DateEmprunt = DateTime.Today.AddDays(-24), DateRetourPrevue = DateTime.Today.AddDays(-10) },
                // En retard de 3 jours
                new Emprunt { LivreID = livres[1].LivreID, MembreID = membres[1].MembreID, DateEmprunt = DateTime.Today.AddDays(-17), DateRetourPrevue = DateTime.Today.AddDays(-3) },
                // En cours, pas en retard
                new Emprunt { LivreID = livres[4].LivreID, MembreID = membres[2].MembreID, DateEmprunt = DateTime.Today.AddDays(-2), DateRetourPrevue = DateTime.Today.AddDays(12) },
                // Déjà retourné
                new Emprunt { LivreID = livres[2].LivreID, MembreID = membres[0].MembreID, DateEmprunt = DateTime.Today.AddDays(-40), DateRetourPrevue = DateTime.Today.AddDays(-26), DateRetourEffective = DateTime.Today.AddDays(-28), Penalite = 0m }
            };
            context.Emprunt.AddRange(emprunts);
            context.SaveChanges();
        }
    }
}
