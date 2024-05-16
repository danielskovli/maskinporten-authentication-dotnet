namespace MaskinportenAuthentication.Exceptions;

public class MaskinportenConfigurationException : Exception
{
    public MaskinportenConfigurationException() { }

    public MaskinportenConfigurationException(string? message)
        : base(message) { }

    public MaskinportenConfigurationException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
