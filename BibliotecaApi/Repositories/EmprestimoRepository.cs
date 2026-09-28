using Microsoft.EntityFrameworkCore;
using BibliotecaApi.Data;
using BibliotecaApi.Models;

namespace BibliotecaApi.Repositories;

public class EmprestimoRepository : IEmprestimoRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<EmprestimoRepository> _logger;

    public EmprestimoRepository(AppDbContext db, ILogger<EmprestimoRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Emprestimo>> ListarAsync(int? livroId, bool? ativos)
    {
        _logger.LogInformation("[Repositório] SELECT Emprestimos (livroId={LivroId}, ativos={Ativos})", livroId, ativos);

        var query = _db.Emprestimos.Include(e => e.Livro).AsQueryable();

        if (livroId.HasValue)
            query = query.Where(e => e.LivroId == livroId.Value);

        if (ativos.HasValue)
            query = ativos.Value
                ? query.Where(e => e.DataDevolucao == null)
                : query.Where(e => e.DataDevolucao != null);

        return await query.OrderByDescending(e => e.DataEmprestimo).ToListAsync();
    }

    public Task<Emprestimo?> ObterPorIdAsync(int id)
    {
        _logger.LogInformation("[Repositório] SELECT Emprestimo {Id}", id);
        return _db.Emprestimos.Include(e => e.Livro).FirstOrDefaultAsync(e => e.Id == id);
    }

    public Task<int> ContarAtivosDoLeitorAsync(string nomeLeitor)
    {
        _logger.LogInformation("[Repositório] COUNT Emprestimos ativos do leitor '{Leitor}'", nomeLeitor);
        return _db.Emprestimos.CountAsync(e => e.NomeLeitor == nomeLeitor && e.DataDevolucao == null);
    }

    public void Adicionar(Emprestimo emprestimo)
    {
        _logger.LogInformation("[Repositório] INSERT Emprestimo (livroId={LivroId})", emprestimo.LivroId);
        _db.Emprestimos.Add(emprestimo);
    }

    public void Remover(Emprestimo emprestimo)
    {
        _logger.LogInformation("[Repositório] DELETE Emprestimo {Id}", emprestimo.Id);
        _db.Emprestimos.Remove(emprestimo);
    }

    public Task SalvarAsync()
    {
        _logger.LogInformation("[Repositório] SaveChanges (commit no banco)");
        return _db.SaveChangesAsync();
    }
}
