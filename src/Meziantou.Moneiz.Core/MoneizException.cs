using System;

namespace Meziantou.Moneiz.Core
{
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
}
