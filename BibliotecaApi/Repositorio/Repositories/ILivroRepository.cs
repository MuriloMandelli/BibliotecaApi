using BibliotecaApi.Models;

namespace BibliotecaApi.Repositories;

/// <summary>Contrato da camada de Repositório para Livro. Só acesso a dados, nenhuma regra de negócio.</summary>
public interface ILivroRepository
{
    Task<List<Livro>> ListarAsync(string? busca);
    Task<Livro?> ObterPorIdAsync(int id);
    Task<bool> ExisteIsbnAsync(string isbn, int? ignorarId = null);
    void Adicionar(Livro livro);
    void Remover(Livro livro);
    Task SalvarAsync();
}
