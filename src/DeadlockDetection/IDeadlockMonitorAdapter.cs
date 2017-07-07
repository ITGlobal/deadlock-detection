using System.Threading;

namespace ITGlobal.DeadlockDetection
{
    /// <summary>
    ///     Адаптер монитора
    /// </summary>
    internal interface IDeadlockMonitorAdapter
    {
        string GetDiagnostics(bool enableStackTrace);

        void EnterReadLock(ReaderWriterLockSlim obj);
        bool TryEnterReadLock(ReaderWriterLockSlim obj, int timeout);
        void ExitReadLock(ReaderWriterLockSlim obj);

        void EnterUpgradeableReadLock(ReaderWriterLockSlim obj);
        bool TryEnterUpgradeableReadLock(ReaderWriterLockSlim obj, int timeout);
        void ExitUpgradeableReadLock(ReaderWriterLockSlim obj);

        void EnterWriteLock(ReaderWriterLockSlim obj);
        bool TryEnterWriteLock(ReaderWriterLockSlim obj, int timeout);
        void ExitWriteLock(ReaderWriterLockSlim obj);

        bool TryEnter(object obj, int timeout);
        void Enter(object obj);
        void Exit(object obj);
        bool HasLock(object obj);
    }
}