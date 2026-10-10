using System.ComponentModel.DataAnnotations;

namespace GestionBibliotheque.Models
{
    public class Livre
    {
        public int LivreID { get; set; }

        [Required(ErrorMessage = "Le titre est obligatoire.")]
        [StringLength(150)]
        public string Titre { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'auteur est obligatoire.")]
        [StringLength(100)]
        public string Auteur { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'ISBN est obligatoire.")]
        [StringLength(20)]
        public string ISBN { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Catégorie")]
        public string? Categorie { get; set; }

        [Range(1450, 2100, ErrorMessage = "Année invalide.")]
        [Display(Name = "Année de publication")]
        public int AnneePublication { get; set; }

        [Range(1, 100, ErrorMessage = "Entre 1 et 100 exemplaires.")]
        [Display(Name = "Nb exemplaires")]
        public int NbExemplaires { get; set; }

        // Calculé par les procédures stockées (jamais saisi par l'utilisateur)
        [Display(Name = "Disponibles")]
        public int NbDisponibles { get; set; }

        public ICollection<Emprunt>? Emprunts { get; set; }
    }
}
