namespace MaskinportenAuthentication.Exceptions;

public class MaskinportenAuthenticationException : Exception
{
    public MaskinportenAuthenticationException() { }

    public MaskinportenAuthenticationException(string? message)
        : base(message) { }

    public MaskinportenAuthenticationException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
