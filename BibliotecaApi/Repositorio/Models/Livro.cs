namespace BibliotecaApi.Models;

public class Livro
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public int TotalExemplares { get; set; }

    public ICollection<Emprestimo> Emprestimos { get; set; } = new List<Emprestimo>();

    public int EmprestimosAtivos => Emprestimos.Count(e => e.Ativo);
    public int ExemplaresDisponiveis => TotalExemplares - EmprestimosAtivos;
}
