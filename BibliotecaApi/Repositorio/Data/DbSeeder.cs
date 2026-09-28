using BibliotecaApi.Models;

namespace BibliotecaApi.Data;

public static class DbSeeder
{
    /// <summary>
    /// Popula livros de exemplo. "O Hobbit" já nasce com o único exemplar emprestado,
    /// para facilitar demonstrar a regra "livro sem exemplar disponível".
    /// </summary>
    public static void Seed(AppDbContext db)
    {
        if (db.Livros.Any()) return;

        var cleanCode = new Livro { Titulo = "Clean Code", Autor = "Robert C. Martin", Isbn = "9780132350884", TotalExemplares = 3 };
        var ddd = new Livro { Titulo = "Domain-Driven Design", Autor = "Eric Evans", Isbn = "9780321125217", TotalExemplares = 2 };
        var hobbit = new Livro { Titulo = "O Hobbit", Autor = "J. R. R. Tolkien", Isbn = "9788595084742", TotalExemplares = 1 };

        db.Livros.AddRange(cleanCode, ddd, hobbit);

        var agora = DateTime.UtcNow;
        db.Emprestimos.Add(new Emprestimo
        {
            Livro = hobbit,
            NomeLeitor = "Ana Souza",
            DataEmprestimo = agora,
            DataPrevistaDevolucao = agora.AddDays(7)
        });

        db.SaveChanges();
    }
}
