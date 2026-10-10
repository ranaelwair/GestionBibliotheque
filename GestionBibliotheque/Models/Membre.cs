using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionBibliotheque.Models
{
    public class Membre
    {
        public int MembreID { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(50)]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est obligatoire.")]
        [StringLength(50)]
        [Display(Name = "Prénom")]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le courriel est obligatoire.")]
        [EmailAddress(ErrorMessage = "Courriel invalide.")]
        [StringLength(100)]
        public string Courriel { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Date d'inscription")]
        public DateTime DateInscription { get; set; }

        [NotMapped]
        [Display(Name = "Membre")]
        public string NomComplet => $"{Prenom} {Nom}";

        public ICollection<Emprunt>? Emprunts { get; set; }
    }
}
