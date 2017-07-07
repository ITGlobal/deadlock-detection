using JetBrains.Annotations;

namespace ITGlobal.DeadlockDetection
{
    /// <summary>
    ///     Объект блокировки
    /// </summary>
    [PublicAPI]
    public interface ILockObject : INamedLockObject
    {
        /// <summary>
        ///     Имеется ли блокировка над объектом
        /// </summary>
        [PublicAPI]
        bool HasLock { get; }

        /// <summary>
        ///     Захватить блокировку
        /// </summary>
        [PublicAPI]
        void Enter();

        /// <summary>
        ///     Попробовать захватить блокировку
        /// </summary>
        [PublicAPI]
        bool TryEnter(int timeout);

        /// <summary>
        ///     Освободить блокировку
        /// </summary>
        [PublicAPI]
        void Exit();

        /// <summary>
        ///     Захватить блокировку (в стиле RAII)
        /// </summary>
        /// <returns>
        ///     Disposable-токен блокировки
        /// </returns>
        [PublicAPI]
        DeadlockMonitorLockToken Lock();

        /// <summary>
        ///     Проверить, имеется ли блокировка над объектом
        /// </summary>
        /// <exception cref="NotSynchonizedException">
        ///     Бросается, если искомой блокировки нет
        /// </exception>
        [PublicAPI]
        void VerifyHasLock();
    }
}

