using System;
using System.Threading;
using JetBrains.Annotations;

namespace ITGlobal.DeadlockDetection
{
    /// <summary>
    ///     Disposable-токен блокировки на чтение с возможностью повышения до блокировки на запись
    /// </summary>
    [PublicAPI]
    public struct UpgradableReadLockDisposableToken : IDisposable
    {
        readonly ReaderWriterLockSlim rwls;

        internal UpgradableReadLockDisposableToken(ReaderWriterLockSlim rwls)
        {
            this.rwls = rwls;
        }

        public void Dispose()
        {
            DeadlockMonitor.ExitUpgradeableReadLock(rwls);
        }
    }
}

