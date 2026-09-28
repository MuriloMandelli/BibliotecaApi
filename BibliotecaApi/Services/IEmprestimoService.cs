using BibliotecaApi.DTOs;

namespace BibliotecaApi.Services;

/// <summary>Contrato da camada de Domínio para Empréstimo (usado por REST e por gRPC).</summary>
public interface IEmprestimoService
{
    Task<IEnumerable<EmprestimoResponseDto>> ListarAsync(int? livroId, bool? ativos);
    Task<EmprestimoResponseDto> ObterPorIdAsync(int id);
    Task<EmprestimoResponseDto> CriarAsync(EmprestimoCreateDto dto);
    Task<EmprestimoResponseDto> DevolverAsync(int id);
    Task ExcluirAsync(int id);
}
