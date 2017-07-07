using System.Threading;

namespace ITGlobal.DeadlockDetection
{
    /// <summary>
    ///     Объект RW-блокировки
    /// </summary>
    internal sealed class NamedReaderWriterLockSlim : ReaderWriterLockSlim, IRwLockObject
    {
        private readonly string name;

        public NamedReaderWriterLockSlim(string name, LockRecursionPolicy recursionPolicy = LockRecursionPolicy.NoRecursion)
            : base(recursionPolicy)
        {
            this.name = name;
        }

        /// <summary>
        ///     Название объекта блокировки
        /// </summary>
        public string Name => name;

        /// <summary>
        ///     Имеется ли блокировка над объектом на чтение
        /// </summary>
        public bool HasReadLock => IsReadLockHeld;

        /// <summary>
        ///     Имеется ли блокировка над объектом на чтение  с возможностью повышения
        /// </summary>
        public bool HasUpgradableReadLock => IsUpgradeableReadLockHeld;

        /// <summary>
        ///     Имеется ли блокировка над объектом на запись
        /// </summary>
        public bool HasWriteLock => IsWriteLockHeld;

        /// <summary>
        ///     Разрешена ли рекурсивная блокировка
        /// </summary>
        public bool IsRecursionSupported => RecursionPolicy == LockRecursionPolicy.SupportsRecursion;

        /// <summary>
        ///     Захватить блокировку над объектом на чтение
        /// </summary>
        /// <returns>
        ///     Disposable-токен блокировки
        /// </returns>
        public ReadLockDisposableToken ReadLock() => DeadlockMonitor.ReadLock(this);

        /// <summary>
        ///     Захватить блокировку над объектом на чтение с возможностью повышения
        /// </summary>
        /// <returns>
        ///     Disposable-токен блокировки
        /// </returns>
        public UpgradableReadLockDisposableToken UpgradableReadLock() => DeadlockMonitor.UpgradableReadLock(this);

        /// <summary>
        ///     Захватить блокировку над объектом на запись
        /// </summary>
        /// <returns>
        ///     Disposable-токен блокировки
        /// </returns>
        public WriteLockDisposableToken WriteLock() => DeadlockMonitor.WriteLock(this);

        /// <summary>
        ///     Захватить блокировку над объектом на чтение
        /// </summary>
        void IRwLockObject.EnterReadLock() => DeadlockMonitor.EnterReadLock(this);

        /// <summary>
        ///     Попытаться захватить блокировку над объектом на чтение с таймаутом
        /// </summary>
        bool IRwLockObject.TryEnterReadLock(int timeout) => DeadlockMonitor.TryEnterReadLock(this, timeout);

        /// <summary>
        ///     Захватить блокировку над объектом на чтение с возможностью повышения
        /// </summary>
        void IRwLockObject.EnterUpgradeableReadLock() => DeadlockMonitor.EnterUpgradeableReadLock(this);
        
        /// <summary>
        ///     Попытаться захватить блокировку над объектом на чтение с возможностью повышения с таймаутом
        /// </summary>
        bool IRwLockObject.TryEnterUpgradeableReadLock(int timeout) => DeadlockMonitor.TryEnterUpgradeableReadLock(this, timeout);

        /// <summary>
        ///     Захватить блокировку над объектом а запись
        /// </summary>
        void IRwLockObject.EnterWriteLock() => DeadlockMonitor.EnterWriteLock(this);

        /// <summary>
        ///     Попытаться захватить блокировку над объектом на запись с таймаутом
        /// </summary>
        bool IRwLockObject.TryEnterWriteLock(int timeout) => DeadlockMonitor.TryEnterWriteLock(this, timeout);

        /// <summary>
        ///     Освободить блокировку над объектом на чтение
        /// </summary>
        void IRwLockObject.ExitReadLock() => DeadlockMonitor.ExitReadLock(this);

        /// <summary>
        ///     Освободить блокировку над объектом на чтение с возможностью повышения
        /// </summary>
        void IRwLockObject.ExitUpgradeableReadLock() => DeadlockMonitor.ExitUpgradeableReadLock(this);

        /// <summary>
        ///     Освободить блокировку над объектом на запись
        /// </summary>
        void IRwLockObject.ExitWriteLock() => DeadlockMonitor.ExitWriteLock(this);
    }
}

