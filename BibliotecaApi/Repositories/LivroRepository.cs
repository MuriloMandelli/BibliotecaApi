using Microsoft.EntityFrameworkCore;
using BibliotecaApi.Data;
using BibliotecaApi.Models;

namespace BibliotecaApi.Repositories;

/// <summary>Camada de Repositório: único lugar (junto com EmprestimoRepository) que conhece o EF Core.</summary>
public class LivroRepository : ILivroRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<LivroRepository> _logger;

    public LivroRepository(AppDbContext db, ILogger<LivroRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Livro>> ListarAsync(string? busca)
    {
        _logger.LogInformation("[Repositório] SELECT Livros (busca={Busca})", busca);

        var query = _db.Livros.Include(l => l.Emprestimos).AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(l => l.Titulo.Contains(busca) || l.Autor.Contains(busca));

        return await query.OrderBy(l => l.Titulo).ToListAsync();
    }

    public Task<Livro?> ObterPorIdAsync(int id)
    {
        _logger.LogInformation("[Repositório] SELECT Livro {Id}", id);
        return _db.Livros.Include(l => l.Emprestimos).FirstOrDefaultAsync(l => l.Id == id);
    }

    public Task<bool> ExisteIsbnAsync(string isbn, int? ignorarId = null)
    {
        _logger.LogInformation("[Repositório] SELECT EXISTS Livro com ISBN {Isbn}", isbn);
        return _db.Livros.AnyAsync(l => l.Isbn == isbn && (ignorarId == null || l.Id != ignorarId));
    }

    public void Adicionar(Livro livro)
    {
        _logger.LogInformation("[Repositório] INSERT Livro '{Titulo}'", livro.Titulo);
        _db.Livros.Add(livro);
    }

    public void Remover(Livro livro)
    {
        _logger.LogInformation("[Repositório] DELETE Livro {Id}", livro.Id);
        _db.Livros.Remove(livro);
    }

    public Task SalvarAsync()
    {
        _logger.LogInformation("[Repositório] SaveChanges (commit no banco)");
        return _db.SaveChangesAsync();
    }
}
