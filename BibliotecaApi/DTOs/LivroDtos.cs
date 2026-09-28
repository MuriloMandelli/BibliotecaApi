using System.ComponentModel.DataAnnotations;

namespace BibliotecaApi.DTOs;

public record LivroCreateDto(
    [Required, StringLength(200)] string Titulo,
    [Required, StringLength(150)] string Autor,
    [Required, StringLength(13, MinimumLength = 10)] string Isbn,
    [Range(1, 1000)] int TotalExemplares);

public record LivroUpdateDto(
    [Required, StringLength(200)] string Titulo,
    [Required, StringLength(150)] string Autor,
    [Required, StringLength(13, MinimumLength = 10)] string Isbn,
    [Range(1, 1000)] int TotalExemplares);

public record LivroResponseDto(
    int Id,
    string Titulo,
    string Autor,
    string Isbn,
    int TotalExemplares,
    int ExemplaresDisponiveis);
