using System.ComponentModel.DataAnnotations;

namespace BibliotecaApi.DTOs;

public record EmprestimoCreateDto(
    int LivroId,
    [Required, StringLength(150)] string NomeLeitor,
    [Range(1, 30)] int PrazoDias);

public record EmprestimoResponseDto(
    int Id,
    int LivroId,
    string LivroTitulo,
    string NomeLeitor,
    DateTime DataEmprestimo,
    DateTime DataPrevistaDevolucao,
    DateTime? DataDevolucao,
    bool Ativo);
