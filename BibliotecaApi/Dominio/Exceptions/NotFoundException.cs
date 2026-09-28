namespace BibliotecaApi.Exceptions;

/// <summary>Recurso não encontrado. REST: 404 | gRPC: NotFound.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
