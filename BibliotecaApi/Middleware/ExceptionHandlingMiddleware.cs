using System.Text.Json;
using BibliotecaApi.Exceptions;

namespace BibliotecaApi.Middleware;

/// <summary>
/// Ponto único de tradução das exceções de Domínio para HTTP (lado REST).
/// O equivalente do lado gRPC é o Grpc/DomainExceptionInterceptor.
///   NotFoundException      -> 404
///   ConflictException      -> 409
///   RegraNegocioException  -> 400
///   qualquer outra         -> 500
/// </summary>
public class ExceptionHandlingMiddleware
{
    // Mantém acentos legíveis na mensagem ("não" em vez de "não")
    private static readonly JsonSerializerOptions JsonOpcoes = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var status = ex switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                ConflictException => StatusCodes.Status409Conflict,
                RegraNegocioException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            if (status == StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "[Apresentação-REST] Erro não tratado ao processar {Path}", context.Request.Path);
            else
                _logger.LogInformation("[Apresentação-REST] Exceção de domínio traduzida para HTTP {Status}: {Message}", status, ex.Message);

            var corpo = status == StatusCodes.Status500InternalServerError
                ? new { erro = "Ocorreu um erro interno no servidor" }
                : new { erro = ex.Message };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = status;
            await context.Response.WriteAsync(JsonSerializer.Serialize(corpo, JsonOpcoes));
        }
    }
}
