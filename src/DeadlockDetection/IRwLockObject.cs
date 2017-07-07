using JetBrains.Annotations;

namespace ITGlobal.DeadlockDetection
{
    /// <summary>
    ///     Объект RW-блокировки
    /// </summary>
    [PublicAPI]
    public interface IRwLockObject : INamedLockObject
    {
        /// <summary>
        ///     Имеется ли блокировка над объектом на чтение
        /// </summary>
        [PublicAPI]
        bool HasReadLock { get; }

        /// <summary>
        ///     Имеется ли блокировка над объектом на чтение  с возможностью повышения
        /// </summary>
        [PublicAPI]
        bool HasUpgradableReadLock { get; }

        /// <summary>
        ///     Имеется ли блокировка над объектом на запись
        /// </summary>
        [PublicAPI]
        bool HasWriteLock { get; }

        /// <summary>
        ///     Разрешена ли рекурсивная блокировка
        /// </summary>
        [PublicAPI]
        bool IsRecursionSupported { get; }

        /// <summary>
        ///     Захватить блокировку над объектом на чтение
        /// </summary>
        /// <returns>
        ///     Disposable-токен блокировки
        /// </returns>
        [PublicAPI]
        ReadLockDisposableToken ReadLock();

        /// <summary>
        ///     Захватить блокировку над объектом на чтение с возможностью повышения
        /// </summary>
        /// <returns>
        ///     Disposable-токен блокировки
        /// </returns>
        [PublicAPI]
        UpgradableReadLockDisposableToken UpgradableReadLock();

        /// <summary>
        ///     Захватить блокировку над объектом на запись
        /// </summary>
        /// <returns>
        ///     Disposable-токен блокировки
        /// </returns>
        [PublicAPI]
        WriteLockDisposableToken WriteLock();
        
        /// <summary>
        ///     Захватить блокировку над объектом на чтение
        /// </summary>
        [PublicAPI]
        void EnterReadLock();
        
        /// <summary>
        ///     Попытаться захватить блокировку над объектом на чтение с таймаутом
        /// </summary>
        [PublicAPI]
        bool TryEnterReadLock(int timeout);

        /// <summary>
        ///     Захватить блокировку над объектом на чтение с возможностью повышения
        /// </summary>
        [PublicAPI]
        void EnterUpgradeableReadLock();

        /// <summary>
        ///     Попытаться захватить блокировку над объектом на чтение с возможностью повышения с таймаутом
        /// </summary>
        [PublicAPI]
        bool TryEnterUpgradeableReadLock(int timeout);

        /// <summary>
        ///     Захватить блокировку над объектом на запись
        /// </summary>
        [PublicAPI]
        void EnterWriteLock();

        /// <summary>
        ///     Попытаться захватить блокировку над объектом на запись с таймаутом
        /// </summary>
        [PublicAPI]
        bool TryEnterWriteLock(int timeout);

        /// <summary>
        ///     Освободить блокировку над объектом на чтение
        /// </summary>
        [PublicAPI]
        void ExitReadLock();

        /// <summary>
        ///     Освободить блокировку над объектом на чтение с возможностью повышения
        /// </summary>
        [PublicAPI]
        void ExitUpgradeableReadLock();

        /// <summary>
        ///     Освободить блокировку над объектом на запись
        /// </summary>
        [PublicAPI]
        void ExitWriteLock();
    }
}

