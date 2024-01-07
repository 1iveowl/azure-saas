namespace Saas.CustomClaimsProvider.Exceptions;

public class ItemNotFoundException : Exception
{
    private ItemNotFoundException() : base()
    {

    }
    public ItemNotFoundException(string? message) : base(message)
    {
    }

    public ItemNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
