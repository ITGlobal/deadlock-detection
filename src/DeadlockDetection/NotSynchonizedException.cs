using System;
using JetBrains.Annotations;

namespace ITGlobal.DeadlockDetection
{
    /// <summary>
    ///     Исключение, возникающиее, если у потока нет блокировки по указанному объекту
    /// </summary>
    [PublicAPI]
    public class NotSynchonizedException : Exception
    {
        public NotSynchonizedException()
        {
        }

        public NotSynchonizedException(string message)
            : base(message)
        {
        }

        public NotSynchonizedException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}

