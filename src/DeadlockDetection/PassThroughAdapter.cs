using System.Threading;

namespace ITGlobal.DeadlockDetection
{
    /// <summary>
    ///     Адаптер монитора-заглушка
    /// </summary>
    internal sealed class PassThroughAdapter : IDeadlockMonitorAdapter
    {
        public static readonly IDeadlockMonitorAdapter Instance = new PassThroughAdapter();

        private PassThroughAdapter() { }

        string IDeadlockMonitorAdapter.GetDiagnostics(bool enableStackTrace)
            => string.Empty;

        void IDeadlockMonitorAdapter.EnterReadLock(ReaderWriterLockSlim obj)
            => obj.EnterReadLock();
        bool IDeadlockMonitorAdapter.TryEnterReadLock(ReaderWriterLockSlim obj, int timeout)
            => obj.TryEnterReadLock(timeout);
        void IDeadlockMonitorAdapter.ExitReadLock(ReaderWriterLockSlim obj)
            => obj.ExitReadLock();

        void IDeadlockMonitorAdapter.EnterUpgradeableReadLock(ReaderWriterLockSlim obj)
            => obj.EnterUpgradeableReadLock();
        bool IDeadlockMonitorAdapter.TryEnterUpgradeableReadLock(ReaderWriterLockSlim obj, int timeout)
            => obj.TryEnterUpgradeableReadLock(timeout);
        void IDeadlockMonitorAdapter.ExitUpgradeableReadLock(ReaderWriterLockSlim obj)
            => obj.ExitUpgradeableReadLock();

        void IDeadlockMonitorAdapter.EnterWriteLock(ReaderWriterLockSlim obj)
            => obj.EnterWriteLock();
        bool IDeadlockMonitorAdapter.TryEnterWriteLock(ReaderWriterLockSlim obj, int timeout)
            => obj.TryEnterWriteLock(timeout);
        void IDeadlockMonitorAdapter.ExitWriteLock(ReaderWriterLockSlim obj)
            => obj.ExitWriteLock();

        void IDeadlockMonitorAdapter.Enter(object obj)
            => Monitor.Enter(obj);

        bool IDeadlockMonitorAdapter.TryEnter(object obj, int timeout)
            => Monitor.TryEnter(obj, timeout);

        void IDeadlockMonitorAdapter.Exit(object obj)
            => Monitor.Exit(obj);

        bool IDeadlockMonitorAdapter.HasLock(object obj)
            => Monitor.IsEntered(obj);
    }
}