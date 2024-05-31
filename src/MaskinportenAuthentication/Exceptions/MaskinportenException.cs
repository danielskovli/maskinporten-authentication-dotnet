namespace MaskinportenAuthentication.Exceptions;

/// <summary>
/// Generic Maskinporten related exception. Something bad happened, and it was related to Maskinporten.
/// </summary>
public class MaskinportenException : Exception
{
    public MaskinportenException() { }

    public MaskinportenException(string? message)
        : base(message) { }

    public MaskinportenException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
