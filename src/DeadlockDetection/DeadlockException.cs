using System;
using JetBrains.Annotations;

namespace ITGlobal.DeadlockDetection
{
    /// <summary>
    ///     Исключение, возникающиее при дедлоке
    /// </summary>
    [PublicAPI]
    public class DeadlockException : Exception
    {
        public DeadlockException()
        {
        }

        public DeadlockException(string message)
            : base(message)
        {
        }

        public DeadlockException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}

