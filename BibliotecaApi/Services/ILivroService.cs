using BibliotecaApi.DTOs;

namespace BibliotecaApi.Services;

/// <summary>
/// Contrato da camada de Domínio para Livro. É ESTA interface que os Controllers REST
/// e os serviços gRPC recebem por injeção de dependência: os dois lados reaproveitam
/// exatamente as mesmas regras de negócio.
/// </summary>
public interface ILivroService
{
    Task<IEnumerable<LivroResponseDto>> ListarAsync(string? busca);
    Task<LivroResponseDto> ObterPorIdAsync(int id);
    Task<LivroResponseDto> CriarAsync(LivroCreateDto dto);
    Task<LivroResponseDto> EditarAsync(int id, LivroUpdateDto dto);
    Task ExcluirAsync(int id);
}
