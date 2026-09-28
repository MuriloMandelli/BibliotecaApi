namespace BibliotecaApi.Models;

public class Emprestimo
{
    public int Id { get; set; }
    public string NomeLeitor { get; set; } = string.Empty;
    public DateTime DataEmprestimo { get; set; }
    public DateTime DataPrevistaDevolucao { get; set; }
    public DateTime? DataDevolucao { get; set; }

    public int LivroId { get; set; }
    public Livro? Livro { get; set; }

    public bool Ativo => DataDevolucao is null;
}
