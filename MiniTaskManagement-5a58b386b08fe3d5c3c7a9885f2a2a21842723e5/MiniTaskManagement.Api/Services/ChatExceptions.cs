namespace MiniTaskManagement.Api.Services;

public class ChatForbiddenException : Exception
{
    public ChatForbiddenException(string message) : base(message)
    {
    }
}

public class ChatNotFoundException : Exception
{
    public ChatNotFoundException(string message) : base(message)
    {
    }
}
