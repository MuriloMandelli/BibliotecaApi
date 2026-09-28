namespace BibliotecaApi.Exceptions;

/// <summary>Conflito com um dado já existente (ex.: ISBN duplicado). REST: 409 | gRPC: AlreadyExists.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
