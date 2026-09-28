using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using BibliotecaApi.DTOs;
using BibliotecaApi.Services;

namespace BibliotecaApi.Grpc;

/// <summary>
/// Camada de Apresentação (gRPC) de Livro. Recebe o MESMO ILivroService que o
/// LivrosController: só converte mensagens protobuf em DTOs e vice-versa.
/// A classe base LivrosService.LivrosServiceBase é gerada a partir de Protos/livros.proto.
/// </summary>
public class LivroGrpcService : LivrosService.LivrosServiceBase
{
    private readonly ILivroService _livroService;
    private readonly ILogger<LivroGrpcService> _logger;

    public LivroGrpcService(ILivroService livroService, ILogger<LivroGrpcService> logger)
    {
        _livroService = livroService;
        _logger = logger;
    }

    public override async Task<ListarLivrosResponse> Listar(ListarLivrosRequest request, ServerCallContext context)
    {
        var busca = request.HasBusca ? request.Busca : null;
        _logger.LogInformation("[Apresentação-gRPC] Listar livros (busca={Busca})", busca);

        var response = new ListarLivrosResponse();
        response.Livros.AddRange((await _livroService.ListarAsync(busca)).Select(ToProto));
        return response;
    }

    public override async Task<LivroResponse> ObterPorId(ObterLivroRequest request, ServerCallContext context)
    {
        _logger.LogInformation("[Apresentação-gRPC] ObterPorId livro {Id}", request.Id);
        return ToProto(await _livroService.ObterPorIdAsync(request.Id));
    }

    public override async Task<LivroResponse> Criar(CriarLivroRequest request, ServerCallContext context)
    {
        _logger.LogInformation("[Apresentação-gRPC] Criar livro '{Titulo}'", request.Titulo);
        var dto = new LivroCreateDto(request.Titulo, request.Autor, request.Isbn, request.TotalExemplares);
        return ToProto(await _livroService.CriarAsync(dto));
    }

    public override async Task<LivroResponse> Editar(EditarLivroRequest request, ServerCallContext context)
    {
        _logger.LogInformation("[Apresentação-gRPC] Editar livro {Id}", request.Id);
        var dto = new LivroUpdateDto(request.Titulo, request.Autor, request.Isbn, request.TotalExemplares);
        return ToProto(await _livroService.EditarAsync(request.Id, dto));
    }

    public override async Task<Empty> Excluir(ExcluirLivroRequest request, ServerCallContext context)
    {
        _logger.LogInformation("[Apresentação-gRPC] Excluir livro {Id}", request.Id);
        await _livroService.ExcluirAsync(request.Id);
        return new Empty();
    }

    private static LivroResponse ToProto(LivroResponseDto dto) => new()
    {
        Id = dto.Id,
        Titulo = dto.Titulo,
        Autor = dto.Autor,
        Isbn = dto.Isbn,
        TotalExemplares = dto.TotalExemplares,
        ExemplaresDisponiveis = dto.ExemplaresDisponiveis
    };
}
