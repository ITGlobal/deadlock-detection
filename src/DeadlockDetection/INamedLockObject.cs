using JetBrains.Annotations;

namespace ITGlobal.DeadlockDetection
{
    /// <summary>
    ///     Объект блокировки
    /// </summary>
    [PublicAPI]
    public interface INamedLockObject
    {
        /// <summary>
        ///     Название объекта блокировки
        /// </summary>
        [PublicAPI, NotNull]
        string Name { get; }
    }
}

