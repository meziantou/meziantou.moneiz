namespace Meziantou.Moneiz.Database;

public class MoneizException : Exception
{
    public MoneizException()
    {
    }

    public MoneizException(string message) : base(message)
    {
    }

    public MoneizException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
