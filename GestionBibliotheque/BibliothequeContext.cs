using GestionBibliotheque.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionBibliotheque
{
    public class BibliothequeContext(DbContextOptions<BibliothequeContext> options) : DbContext(options)
    {
        public DbSet<Livre> Livre { get; set; } = default!;
        public DbSet<Membre> Membre { get; set; } = default!;
        public DbSet<Emprunt> Emprunt { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Un ISBN ne peut exister qu'une seule fois
            modelBuilder.Entity<Livre>()
                .HasIndex(l => l.ISBN)
                .IsUnique();

            // Pas de suppression en cascade : on ne perd pas l'historique des emprunts
            modelBuilder.Entity<Emprunt>()
                .HasOne(e => e.Livre)
                .WithMany(l => l.Emprunts)
                .HasForeignKey(e => e.LivreID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Emprunt>()
                .HasOne(e => e.Membre)
                .WithMany(m => m.Emprunts)
                .HasForeignKey(e => e.MembreID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
