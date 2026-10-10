using System.ComponentModel.DataAnnotations;

namespace GestionBibliotheque.Models.ViewModels
{
    // Une ligne retournée par la procédure stockée dbo.sp_EmpruntsEnRetard.
    // Les noms des propriétés = noms des colonnes du SELECT.
    public class EmpruntRetard
    {
        public int EmpruntID { get; set; }

        public string Titre { get; set; } = string.Empty;

        [Display(Name = "Membre")]
        public string NomMembre { get; set; } = string.Empty;

        public string Courriel { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Emprunté le")]
        public DateTime DateEmprunt { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Retour prévu")]
        public DateTime DateRetourPrevue { get; set; }

        [Display(Name = "Jours de retard")]
        public int JoursRetard { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Pénalité estimée")]
        public decimal PenaliteEstimee { get; set; }
    }
}
