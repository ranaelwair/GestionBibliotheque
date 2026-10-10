using System.ComponentModel.DataAnnotations;

namespace GestionBibliotheque.Models.ViewModels
{
    // Données du formulaire « Emprunter un livre »
    public class EmprunterViewModel
    {
        [Required(ErrorMessage = "Choisissez un livre.")]
        [Display(Name = "Livre")]
        public int? LivreID { get; set; }

        [Required(ErrorMessage = "Choisissez un membre.")]
        [Display(Name = "Membre")]
        public int? MembreID { get; set; }

        [Range(1, 30, ErrorMessage = "Entre 1 et 30 jours.")]
        [Display(Name = "Durée (jours)")]
        public int DureeJours { get; set; } = 14;
    }
}
