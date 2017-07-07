using System;
using System.Threading;

namespace ITGlobal.DeadlockDetection
{
    /// <summary>
    ///     Адаптер монитора c детектированием дедлоков.
    ///     Общий принцип: сначала ожидание блокировки в течении <see cref="DeadlockMonitor.OptimisticTimeout"/> мс,
    ///     затем, в случае неудачи, проверка на дедлок с последующим ожиданием (на этот раз уже бесконечным)
    /// </summary>
    internal sealed class TrackingAdapter : IDeadlockMonitorAdapter
    {
        #region singletone

        public static readonly IDeadlockMonitorAdapter Instance = new TrackingAdapter();

        private TrackingAdapter() { }

        #endregion

        #region IDeadlockMonitorAdapter

        string IDeadlockMonitorAdapter.GetDiagnostics(bool enableStackTrace) 
            => DeadlockDetectionEngine.GetDiagnostics(enableStackTrace);

        void IDeadlockMonitorAdapter.EnterReadLock(ReaderWriterLockSlim obj)
            => EnterLock(obj, LockAccessLevel.Read, _ => _.TryEnterReadLock(DeadlockMonitor.OptimisticTimeout), _ => _.EnterReadLock());
        bool IDeadlockMonitorAdapter.TryEnterReadLock(ReaderWriterLockSlim obj, int timeout)
            => TryEnterLock(obj, LockAccessLevel.Read, _ => _.TryEnterReadLock(timeout));
        void IDeadlockMonitorAdapter.ExitReadLock(ReaderWriterLockSlim obj)
        {
            // Помечаем данный поток как не имеющий блокировку объекта obj с уровнем ReadLock
            DeadlockDetectionEngine.ReleaseOwner(obj, LockAccessLevel.Read);

            obj.ExitReadLock();
        }

        void IDeadlockMonitorAdapter.EnterUpgradeableReadLock(ReaderWriterLockSlim obj)
            => EnterLock(
                obj,
                LockAccessLevel.UpgradeableRead,
                _ => _.TryEnterUpgradeableReadLock(DeadlockMonitor.OptimisticTimeout),
                _ => _.EnterUpgradeableReadLock()
            );

        bool IDeadlockMonitorAdapter.TryEnterUpgradeableReadLock(ReaderWriterLockSlim obj, int timeout) =>
            TryEnterLock(
                obj,
                LockAccessLevel.UpgradeableRead,
                _ => _.TryEnterUpgradeableReadLock(timeout)
            );

        void IDeadlockMonitorAdapter.ExitUpgradeableReadLock(ReaderWriterLockSlim obj)
        {
            // Помечаем данный поток как не имеющий блокировку объекта obj с уровнем UpgradeableReadLock
            DeadlockDetectionEngine.ReleaseOwner(obj, LockAccessLevel.UpgradeableRead);

            obj.ExitUpgradeableReadLock();
        }

        void IDeadlockMonitorAdapter.EnterWriteLock(ReaderWriterLockSlim obj)
            => EnterLock(
                obj,
                LockAccessLevel.Write,
                _ => _.TryEnterWriteLock(DeadlockMonitor.OptimisticTimeout),
                _ => _.EnterWriteLock()
            );

        bool IDeadlockMonitorAdapter.TryEnterWriteLock(ReaderWriterLockSlim obj, int timeout)
            => TryEnterLock(
                obj,
                LockAccessLevel.Write,
                _ => _.TryEnterWriteLock(timeout)
            );

        void IDeadlockMonitorAdapter.ExitWriteLock(ReaderWriterLockSlim obj)
        {
            // Помечаем данный поток как не имеющий блокировку объекта obj с уровнем WriteLock
            DeadlockDetectionEngine.ReleaseOwner(obj, LockAccessLevel.Write);

            obj.ExitWriteLock();
        }

        void IDeadlockMonitorAdapter.Enter(object obj)
            => EnterLock(
                obj,
                LockAccessLevel.Write,
                _ => Monitor.TryEnter(_, DeadlockMonitor.OptimisticTimeout),
                Monitor.Enter
            );

        bool IDeadlockMonitorAdapter.TryEnter(object obj, int timeout)
            => TryEnterLock(
                obj,
                LockAccessLevel.Write,
                _ => Monitor.TryEnter(_, timeout)
            );

        void IDeadlockMonitorAdapter.Exit(object obj)
        {
            // Помечаем данный поток как не имеющий блокировку объекта obj с уровнем WriteLock
            DeadlockDetectionEngine.ReleaseOwner(obj, LockAccessLevel.Write);

            Monitor.Exit(obj);
        }

        bool IDeadlockMonitorAdapter.HasLock(object obj)
            => Monitor.IsEntered(obj);

        #endregion

        #region helpers

        private static void EnterLock<T>(
            T lockObject,
            LockAccessLevel level,
            Func<T, bool> optimisticWait,
            Action<T> wait)
        {
            // Сначала дожидаемся в пределах оптимистичного таймаута
            DeadlockDetectionEngine.RegisterWaiting(lockObject, level);
            if (!optimisticWait(lockObject))
            {
                // Если блокировка не былаа захвачена в течении оптимистичного таймаута,
                // то проверяем, нет ли дедлока
                DeadlockDetectionEngine.VerifyDeadlock(lockObject, level);

                // Дожидаемся захвата блокировки
                wait(lockObject);
            }

            // Помечаем данный поток как имеющий блокировку объекта obj с уровнем level
            DeadlockDetectionEngine.RegisterOwner(lockObject, level);
        }

        private static bool TryEnterLock<T>(
            T lockObject,
            LockAccessLevel level,
            Func<T, bool> wait)
        {
            DeadlockDetectionEngine.RegisterWaiting(lockObject, level);
            if (!wait(lockObject))
            {
                DeadlockDetectionEngine.RemoveWaiting(lockObject, level);
                return false;
            }

            // Помечаем данный поток как имеющий блокировку объекта obj с уровнем level
            DeadlockDetectionEngine.RegisterOwner(lockObject, level);

            return true;
        }

        #endregion
    }
}