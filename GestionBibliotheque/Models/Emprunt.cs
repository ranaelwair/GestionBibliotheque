using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionBibliotheque.Models
{
    public class Emprunt
    {
        public int EmpruntID { get; set; }

        [Display(Name = "Livre")]
        public int LivreID { get; set; }

        [Display(Name = "Membre")]
        public int MembreID { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Emprunté le")]
        public DateTime DateEmprunt { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Retour prévu")]
        public DateTime DateRetourPrevue { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Retourné le")]
        public DateTime? DateRetourEffective { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        [DataType(DataType.Currency)]
        [Display(Name = "Pénalité")]
        public decimal Penalite { get; set; }

        [NotMapped]
        public bool EstEnCours => DateRetourEffective == null;

        [NotMapped]
        public bool EstEnRetard => EstEnCours && DateRetourPrevue.Date < DateTime.Today;

        public Livre? Livre { get; set; }

        public Membre? Membre { get; set; }
    }
}
