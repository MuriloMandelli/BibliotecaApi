namespace BibliotecaApi.Exceptions;

/// <summary>Violação de regra de negócio do Domínio. REST: 400 | gRPC: FailedPrecondition.</summary>
public class RegraNegocioException : Exception
{
    public RegraNegocioException(string message) : base(message) { }
}
