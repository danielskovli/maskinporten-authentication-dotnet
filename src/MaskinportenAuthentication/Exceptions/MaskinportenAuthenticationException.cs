namespace MaskinportenAuthentication.Exceptions;

/// <summary>
/// An exception that indicates a problem with the authentication/authorization call to Maskinporten
/// </summary>
public class MaskinportenAuthenticationException : Exception
{
    public MaskinportenAuthenticationException() { }

    public MaskinportenAuthenticationException(string? message)
        : base(message) { }

    public MaskinportenAuthenticationException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
