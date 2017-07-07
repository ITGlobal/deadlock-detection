namespace ITGlobal.DeadlockDetection
{
    /// <summary>
    ///     Объект блокировки
    /// </summary>
    internal sealed class NamedLockCookie : ILockObject
    {
        private readonly string name;

        public NamedLockCookie(string name)
        {
            this.name = name;
        }

        /// <summary>
        ///     Название объекта блокировки
        /// </summary>
        public string Name => name;

        /// <summary>
        ///     Имеется ли блокировка над объектом
        /// </summary>
        public bool HasLock => DeadlockMonitor.HasLock(this);

        /// <summary>
        ///     Захватить блокировку
        /// </summary>
        public void Enter() => DeadlockMonitor.Enter(this);

        /// <summary>
        ///     Попробовать захватить блокировку
        /// </summary>
        public bool TryEnter(int timeout) => DeadlockMonitor.TryEnter(this, timeout);

        /// <summary>
        ///     Освободить блокировку
        /// </summary>
        public void Exit() => DeadlockMonitor.Exit(this);

        /// <summary>
        ///     Захватить блокировку (в стиле RAII)
        /// </summary>
        /// <returns>
        ///     Disposable-токен блокировки
        /// </returns>
        public DeadlockMonitorLockToken Lock() => DeadlockMonitor.Lock(this);

        /// <summary>
        ///     Проверить, имеется ли блокировка над объектом
        /// </summary>
        /// <exception cref="NotSynchonizedException">
        ///     Бросается, если искомой блокировки нет
        /// </exception>
        public void VerifyHasLock() => DeadlockMonitor.VerifyHasLock(this);
    }
}

