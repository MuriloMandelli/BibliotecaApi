using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using BibliotecaApi.DTOs;
using BibliotecaApi.Services;

namespace BibliotecaApi.Grpc;

/// <summary>
/// Camada de Apresentação (gRPC) de Empréstimo. Reaproveita o MESMO IEmprestimoService
/// do EmprestimosController: a regra "livro precisa ter exemplar disponível" não é
/// duplicada aqui.
/// </summary>
public class EmprestimoGrpcService : EmprestimosService.EmprestimosServiceBase
{
    private readonly IEmprestimoService _emprestimoService;
    private readonly ILogger<EmprestimoGrpcService> _logger;

    public EmprestimoGrpcService(IEmprestimoService emprestimoService, ILogger<EmprestimoGrpcService> logger)
    {
        _emprestimoService = emprestimoService;
        _logger = logger;
    }

    public override async Task<ListarEmprestimosResponse> Listar(ListarEmprestimosRequest request, ServerCallContext context)
    {
        int? livroId = request.HasLivroId ? request.LivroId : null;
        bool? ativos = request.HasAtivos ? request.Ativos : null;
        _logger.LogInformation("[Apresentação-gRPC] Listar empréstimos (livroId={LivroId}, ativos={Ativos})", livroId, ativos);

        var response = new ListarEmprestimosResponse();
        response.Emprestimos.AddRange((await _emprestimoService.ListarAsync(livroId, ativos)).Select(ToProto));
        return response;
    }

    public override async Task<EmprestimoResponse> ObterPorId(ObterEmprestimoRequest request, ServerCallContext context)
    {
        _logger.LogInformation("[Apresentação-gRPC] ObterPorId empréstimo {Id}", request.Id);
        return ToProto(await _emprestimoService.ObterPorIdAsync(request.Id));
    }

    public override async Task<EmprestimoResponse> Criar(CriarEmprestimoRequest request, ServerCallContext context)
    {
        _logger.LogInformation("[Apresentação-gRPC] Criar empréstimo (livroId={LivroId})", request.LivroId);
        var dto = new EmprestimoCreateDto(request.LivroId, request.NomeLeitor, request.PrazoDias);
        return ToProto(await _emprestimoService.CriarAsync(dto));
    }

    public override async Task<EmprestimoResponse> Devolver(DevolverEmprestimoRequest request, ServerCallContext context)
    {
        _logger.LogInformation("[Apresentação-gRPC] Devolver empréstimo {Id}", request.Id);
        return ToProto(await _emprestimoService.DevolverAsync(request.Id));
    }

    public override async Task<Empty> Excluir(ExcluirEmprestimoRequest request, ServerCallContext context)
    {
        _logger.LogInformation("[Apresentação-gRPC] Excluir empréstimo {Id}", request.Id);
        await _emprestimoService.ExcluirAsync(request.Id);
        return new Empty();
    }

    private static EmprestimoResponse ToProto(EmprestimoResponseDto dto) => new()
    {
        Id = dto.Id,
        LivroId = dto.LivroId,
        LivroTitulo = dto.LivroTitulo,
        NomeLeitor = dto.NomeLeitor,
        DataEmprestimo = Timestamp.FromDateTime(dto.DataEmprestimo),
        DataPrevistaDevolucao = Timestamp.FromDateTime(dto.DataPrevistaDevolucao),
        DataDevolucao = dto.DataDevolucao is null ? null : Timestamp.FromDateTime(dto.DataDevolucao.Value),
        Ativo = dto.Ativo
    };
}
