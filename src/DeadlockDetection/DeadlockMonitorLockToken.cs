using System;
using JetBrains.Annotations;

namespace ITGlobal.DeadlockDetection
{
    /// <summary>
    ///     Disposable-токен блокировки монитора
    /// </summary>
    [PublicAPI]
    public struct DeadlockMonitorLockToken : IDisposable
    {
        private readonly object lockObject;

        internal DeadlockMonitorLockToken(object lockObject)
            : this()
        {
            this.lockObject = lockObject;
        }

        public void Dispose()
        {
            if (lockObject != null)
            {
                // Такое бывает под тестами
                DeadlockMonitor.Exit(lockObject);
            }
        }
    }
}

