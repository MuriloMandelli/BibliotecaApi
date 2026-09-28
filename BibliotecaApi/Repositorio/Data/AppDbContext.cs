using Microsoft.EntityFrameworkCore;
using BibliotecaApi.Models;

namespace BibliotecaApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Livro> Livros => Set<Livro>();
    public DbSet<Emprestimo> Emprestimos => Set<Emprestimo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Livro>()
            .HasIndex(l => l.Isbn)
            .IsUnique();

        modelBuilder.Entity<Livro>().Ignore(l => l.EmprestimosAtivos);
        modelBuilder.Entity<Livro>().Ignore(l => l.ExemplaresDisponiveis);
        modelBuilder.Entity<Emprestimo>().Ignore(e => e.Ativo);

        // Excluir um livro leva junto o histórico de empréstimos já devolvidos.
        // Livro com empréstimo ATIVO nunca chega aqui: o Domínio barra antes.
        modelBuilder.Entity<Emprestimo>()
            .HasOne(e => e.Livro)
            .WithMany(l => l.Emprestimos)
            .HasForeignKey(e => e.LivroId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
