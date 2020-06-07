using System;
using System.Runtime.Serialization;

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

        protected MoneizException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
