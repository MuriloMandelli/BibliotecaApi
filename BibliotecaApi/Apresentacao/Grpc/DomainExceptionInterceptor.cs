using Grpc.Core;
using Grpc.Core.Interceptors;
using BibliotecaApi.Exceptions;

namespace BibliotecaApi.Grpc;

/// <summary>
/// Equivalente gRPC do ExceptionHandlingMiddleware do REST: ponto único que traduz
/// as exceções de Domínio para status gRPC. Assim nenhum GrpcService tem try/catch
/// nem decide status sozinho.
///   NotFoundException      -> NotFound
///   ConflictException      -> AlreadyExists
///   RegraNegocioException  -> FailedPrecondition
///   qualquer outra         -> Internal
/// </summary>
public class DomainExceptionInterceptor : Interceptor
{
    private readonly ILogger<DomainExceptionInterceptor> _logger;

    public DomainExceptionInterceptor(ILogger<DomainExceptionInterceptor> logger)
    {
        _logger = logger;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var status = ex switch
            {
                NotFoundException => StatusCode.NotFound,
                ConflictException => StatusCode.AlreadyExists,
                RegraNegocioException => StatusCode.FailedPrecondition,
                _ => StatusCode.Internal
            };

            if (status == StatusCode.Internal)
            {
                _logger.LogError(ex, "[Apresentação-gRPC] Erro não tratado em {Method}", context.Method);
                throw new RpcException(new Status(status, "Ocorreu um erro interno no servidor"));
            }

            _logger.LogInformation("[Apresentação-gRPC] Exceção de domínio traduzida para {Status}: {Message}", status, ex.Message);
            throw new RpcException(new Status(status, ex.Message));
        }
    }
}
