using System;
using System.Threading;
using JetBrains.Annotations;

namespace ITGlobal.DeadlockDetection
{
    /// <summary>
    ///     Disposable-токен блокировки на запись
    /// </summary>
    [PublicAPI]
    public struct WriteLockDisposableToken : IDisposable
    {
        readonly ReaderWriterLockSlim rwls;

        internal WriteLockDisposableToken(ReaderWriterLockSlim rwls)
        {
            this.rwls = rwls;
        }

        public void Dispose()
        {
            DeadlockMonitor.ExitWriteLock(rwls);
        }
    }
}