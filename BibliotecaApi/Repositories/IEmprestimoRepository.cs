using BibliotecaApi.Models;

namespace BibliotecaApi.Repositories;

/// <summary>Contrato da camada de Repositório para Empréstimo.</summary>
public interface IEmprestimoRepository
{
    Task<List<Emprestimo>> ListarAsync(int? livroId, bool? ativos);
    Task<Emprestimo?> ObterPorIdAsync(int id);
    Task<int> ContarAtivosDoLeitorAsync(string nomeLeitor);
    void Adicionar(Emprestimo emprestimo);
    void Remover(Emprestimo emprestimo);
    Task SalvarAsync();
}
