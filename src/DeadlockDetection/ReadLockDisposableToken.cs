using System;
using System.Threading;
using JetBrains.Annotations;

namespace ITGlobal.DeadlockDetection
{
    /// <summary>
    ///     Disposable-токен блокировки на чтение
    /// </summary>
    [PublicAPI]
    public struct ReadLockDisposableToken : IDisposable
    {
        readonly ReaderWriterLockSlim rwls;

        internal ReadLockDisposableToken(ReaderWriterLockSlim rwls)
        {
            this.rwls = rwls;
        }

        public void Dispose()
        {
            DeadlockMonitor.ExitReadLock(rwls);
        }
    }
}

