using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using JetBrains.Annotations;

namespace ITGlobal.DeadlockDetection
{
    /// <summary>
    ///     Монитор с отслеживанием дедлоков
    /// </summary>
    [PublicAPI]
    public static class DeadlockMonitor
    {
        #region fields

        /// <summary>
        ///     Интервал оптимистической блокировки, в миллисекундах
        /// </summary>
        internal const int OptimisticTimeout = 250;

        private static IDeadlockMonitorAdapter _adapter =
#if DEBUG
             TrackingAdapter.Instance;
#else
             PassThroughAdapter.Instance;
#endif

        #endregion

        #region public API

        #region diagnostics

        /// <summary>
        ///     Включить детектирование дедлоков
        /// </summary>
        [PublicAPI]
        public static void Enable()
        {
            _adapter = TrackingAdapter.Instance;
        }

        /// <summary>
        ///     Отключить детектирование дедлоков
        /// </summary>
        [PublicAPI]
        public static void Disable()
        {
            _adapter = PassThroughAdapter.Instance;
        }

        /// <summary>
        ///     Получить диагностическую информацию о состоянии блокировок
        /// </summary>
        /// <param name="enableStackTrace">
        ///     Выводить ли стек потоков
        /// </param>
        /// <returns>
        ///     Диагностическая информация
        /// </returns>
        [PublicAPI]
        public static string GetDiagnostics(bool enableStackTrace) => _adapter.GetDiagnostics(enableStackTrace);

        /// <summary>
        ///     Коллбек для логирования отчетов о дедлоках
        /// </summary>
        [PublicAPI]
        public static DeadlockReportCallback Callback { get; set; }

        #endregion

        #region lock cookie factory

        /// <summary>
        ///     Создать именованный объект блокировки типа <see cref="ReaderWriterLockSlim"/>
        /// </summary>
        /// <typeparam name="TOwner">
        ///     Тип объекта-владельца блокировки
        /// </typeparam>
        /// <param name="name">
        ///     Название блокировки
        /// </param>
        /// <param name="recursionPolicy">
        ///     Режим рекурсии
        /// </param>
        /// <returns>
        ///     Объект блокировки
        /// </returns>
        [PublicAPI]
        public static IRwLockObject ReaderWriterLock<TOwner>(
            [NotNull] string name,
            LockRecursionPolicy recursionPolicy = LockRecursionPolicy.NoRecursion)
            => ReaderWriterLock(typeof(TOwner), name, recursionPolicy);

        /// <summary>
        ///     Создать именованный объект блокировки типа <see cref="ReaderWriterLockSlim"/>
        /// </summary>
        /// <param name="owner">
        ///     Тип объекта-владельца блокировки
        /// </param>
        /// <param name="name">
        ///     Название блокировки
        /// </param>
        /// <param name="recursionPolicy">
        ///     Режим рекурсии
        /// </param>
        /// <returns>
        ///     Объект блокировки
        /// </returns>
        [PublicAPI]
        public static IRwLockObject ReaderWriterLock(
            [NotNull] Type owner,
            [NotNull] string name,
            LockRecursionPolicy recursionPolicy = LockRecursionPolicy.NoRecursion)
            => new NamedReaderWriterLockSlim(GetFriendlyTypeName(owner) + "#" + name, recursionPolicy);

        /// <summary>
        ///     Создать именованный объект блокировки
        /// </summary>
        /// <param name="name">
        ///     Название блокировки
        /// </param>
        /// <param name="owner">
        ///     Тип объекта-владельца блокировки
        /// </param>
        /// <returns>
        ///     Объект блокировки
        /// </returns>
        [PublicAPI]
        public static ILockObject Cookie([NotNull] Type owner, [NotNull] string name = "syncRoot")
            => new NamedLockCookie(GetFriendlyTypeName(owner) + "#" + name);

        /// <summary>
        ///     Создать именованный объект блокировки
        /// </summary>
        /// <typeparam name="TOwner">
        ///     Тип объекта-владельца блокировки
        /// </typeparam>
        /// <param name="name">
        ///     Название блокировки
        /// </param>
        /// <returns>
        ///     Объект блокировки
        /// </returns>
        [PublicAPI]
        public static ILockObject Cookie<TOwner>([NotNull] string name = "syncRoot") => Cookie(typeof(TOwner), name);

        private static string GetFriendlyTypeName(Type type)
        {
#if NET45 || NET46
            if (type.IsGenericType)
#endif
#if NETSTANDARD1_6
            if (type.GetTypeInfo().IsGenericTypeDefinition) 
#endif
            {
                var genericArguments = type.GetTypeInfo().GetGenericArguments();
                var i = type.Name.LastIndexOf('`');
                return string.Format(
                    "{0}.{1}<{2}>",
                    type.Namespace,
                    i >= 0 ? type.Name.Substring(0, i) : type.Name,
                    string.Join(", ", genericArguments.Select(GetFriendlyTypeName)));
            }

            return type.FullName;
        }

        #endregion

        #region raii locks

        /// <summary>
        ///     Захватить блокировку над объектом <paramref name="lockObject"/>
        /// </summary>
        /// <param name="lockObject">
        ///     Объект блокировки
        /// </param>
        /// <returns>
        ///     Disposable-токен блокировки
        /// </returns>
        [PublicAPI]
        public static DeadlockMonitorLockToken Lock([NotNull] object lockObject)
        {
            Enter(lockObject);
            return new DeadlockMonitorLockToken(lockObject);
        }

        /// <summary>
        ///     Проверить, имеется ли блокировка над объектом <paramref name="lockObject"/> 
        /// </summary>
        /// <param name="lockObject">
        ///     Объект блокировки
        /// </param>
        /// <returns>
        ///     true, если имеется блокировка, false - в противном случае
        /// </returns>
        [PublicAPI]
        public static bool HasLock([NotNull] object lockObject) => _adapter.HasLock(lockObject);

        /// <summary>
        ///     Проверить, имеется ли блокировка над объектом <paramref name="lockObject"/> 
        /// </summary>
        /// <param name="lockObject">
        ///     Объект блокировки
        /// </param>
        /// <exception cref="NotSynchonizedException">
        ///     Бросается, если искомой блокировки нет
        /// </exception>
        [PublicAPI]
        public static void VerifyHasLock([NotNull] object lockObject)
        {
            if (!HasLock(lockObject))
            {
                var namedObject = lockObject as INamedLockObject;
                throw new NotSynchonizedException(
                    $"Current thread has no lock on {namedObject?.Name ?? lockObject.ToString()}");
            }
        }

        /// <summary>
        ///     Захватить блокировку над объектом <paramref name="lockObject"/> на чтение
        /// </summary>
        /// <param name="lockObject">
        ///     Объект блокировки
        /// </param>
        /// <returns>
        ///     Disposable-токен блокировки
        /// </returns>
        [PublicAPI]
        public static ReadLockDisposableToken ReadLock([NotNull] ReaderWriterLockSlim lockObject)
        {
            EnterReadLock(lockObject);
            return new ReadLockDisposableToken(lockObject);
        }

        /// <summary>
        ///     Захватить блокировку над объектом <paramref name="lockObject"/> на чтение с возможностью повышения
        /// </summary>
        /// <param name="lockObject">
        ///     Объект блокировки
        /// </param>
        /// <returns>
        ///     Disposable-токен блокировки
        /// </returns>
        [PublicAPI]
        public static UpgradableReadLockDisposableToken UpgradableReadLock([NotNull] ReaderWriterLockSlim lockObject)
        {
            EnterUpgradeableReadLock(lockObject);
            return new UpgradableReadLockDisposableToken(lockObject);
        }

        /// <summary>
        ///     Захватить блокировку над объектом <paramref name="lockObject"/> на запись
        /// </summary>
        /// <param name="lockObject">
        ///     Объект блокировки
        /// </param>
        /// <returns>
        ///     Disposable-токен блокировки
        /// </returns>
        [PublicAPI]
        public static WriteLockDisposableToken WriteLock([NotNull] ReaderWriterLockSlim lockObject)
        {
            EnterWriteLock(lockObject);
            return new WriteLockDisposableToken(lockObject);
        }

        #endregion

        #region lock acquire/release

        /// <summary>
        ///     Захватить блокировку над объектом <paramref name="obj"/> на чтение
        /// </summary>
        /// <param name="obj">
        ///     Объект блокировки
        /// </param>
        [PublicAPI]
        public static void EnterReadLock([NotNull] ReaderWriterLockSlim obj)
        {
            _adapter.EnterReadLock(obj);
        }

        /// <summary>
        ///     Попытаться захватить блокировку над объектом <paramref name="obj"/> на чтение с таймаутом
        /// </summary>
        /// <param name="obj">
        ///     Объект блокировки
        /// </param>
        /// <param name="timeout">
        ///     Таймаут
        /// </param>
        /// <returns>
        ///     true, если блокировка была захвачена, false - в противном случае
        /// </returns>
        [PublicAPI]
        public static bool TryEnterReadLock([NotNull] ReaderWriterLockSlim obj, int timeout)
        {
            if (timeout == Timeout.Infinite)
            {
                _adapter.EnterReadLock(obj);
                return true;
            }

            return _adapter.TryEnterReadLock(obj, timeout);
        }

        /// <summary>
        ///     Захватить блокировку над объектом <paramref name="obj"/> на чтение с возможностью повышения
        /// </summary>
        /// <param name="obj">
        ///     Объект блокировки
        /// </param>
        [PublicAPI]
        public static void EnterUpgradeableReadLock([NotNull] ReaderWriterLockSlim obj)
        {
            _adapter.EnterUpgradeableReadLock(obj);
        }

        /// <summary>
        ///     Попытаться захватить блокировку над объектом <paramref name="obj"/> на чтение с возможностью повышения с таймаутом
        /// </summary>
        /// <param name="obj">
        ///     Объект блокировки
        /// </param>
        /// <param name="timeout">
        ///     Таймаут
        /// </param>
        /// <returns>
        ///     true, если блокировка была захвачена, false - в противном случае
        /// </returns>
        [PublicAPI]
        public static bool TryEnterUpgradeableReadLock([NotNull] ReaderWriterLockSlim obj, int timeout)
        {
            if (timeout == Timeout.Infinite)
            {
                _adapter.EnterUpgradeableReadLock(obj);
                return true;
            }

            return _adapter.TryEnterUpgradeableReadLock(obj, timeout);
        }

        /// <summary>
        ///     Захватить блокировку над объектом <paramref name="obj"/> на запись
        /// </summary>
        /// <param name="obj">
        ///     Объект блокировки
        /// </param>
        [PublicAPI]
        public static void EnterWriteLock([NotNull] ReaderWriterLockSlim obj)
        {
            _adapter.EnterWriteLock(obj);
        }

        /// <summary>
        ///     Попытаться захватить блокировку над объектом <paramref name="obj"/> на запись с таймаутом
        /// </summary>
        /// <param name="obj">
        ///     Объект блокировки
        /// </param>
        /// <param name="timeout">
        ///     Таймаут
        /// </param>
        /// <returns>
        ///     true, если блокировка была захвачена, false - в противном случае
        /// </returns>
        [PublicAPI]
        public static bool TryEnterWriteLock([NotNull] ReaderWriterLockSlim obj, int timeout)
        {
            if (timeout == Timeout.Infinite)
            {
                _adapter.EnterWriteLock(obj);
                return true;
            }

            return _adapter.TryEnterWriteLock(obj, timeout);
        }

        /// <summary>
        ///     Освободить блокировку над объектом <paramref name="obj"/> на чтение
        /// </summary>
        /// <param name="obj">
        ///     Объект блокировки
        /// </param>
        [PublicAPI]
        public static void ExitReadLock([NotNull] ReaderWriterLockSlim obj)
        {
            _adapter.ExitReadLock(obj);
        }

        /// <summary>
        ///     Освободить блокировку над объектом <paramref name="obj"/> на чтение с возможностью повышения
        /// </summary>
        /// <param name="obj">
        ///     Объект блокировки
        /// </param>
        [PublicAPI]
        public static void ExitUpgradeableReadLock([NotNull] ReaderWriterLockSlim obj)
        {
            _adapter.ExitUpgradeableReadLock(obj);
        }

        /// <summary>
        ///     Освободить блокировку над объектом <paramref name="obj"/> на запись
        /// </summary>
        /// <param name="obj">
        ///     Объект блокировки
        /// </param>
        [PublicAPI]
        public static void ExitWriteLock([NotNull] ReaderWriterLockSlim obj)
        {
            _adapter.ExitWriteLock(obj);
        }

        /// <summary>
        ///     Захватить блокировку над объектом <paramref name="obj"/>
        /// </summary>
        /// <param name="obj">
        ///     Объект блокировки
        /// </param>
        [PublicAPI]
        public static void Enter([NotNull] object obj)
        {
            _adapter.Enter(obj);
        }

        /// <summary>
        ///     Попробовать захватить блокировку над объектом <paramref name="obj"/>
        /// </summary>
        /// <param name="obj">
        ///     Объект блокировки
        /// </param>
        /// <param name="timeout">
        ///     Таймаут
        /// </param>
        [PublicAPI]
        public static bool TryEnter([NotNull] object obj, int timeout)
        {
            return _adapter.TryEnter(obj, timeout);
        }

        /// <summary>
        ///     Освободить блокировку над объектом <paramref name="obj"/>
        /// </summary>
        /// <param name="obj">
        ///     Объект блокировки
        /// </param>
        [PublicAPI]
        public static void Exit([NotNull] object obj)
        {
            _adapter.Exit(obj);
        }

        #endregion

        #endregion
    }
}

