using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace ITGlobal.DeadlockDetection
{
    /// <summary>
    ///     Движок отслеживания взаимоблокировок
    /// </summary>
    internal static class DeadlockDetectionEngine
    {
        #region fields

        /// <summary>
        ///     Блокировка графа
        /// </summary>
        private static readonly object _WaitGraphLock = new object();

        /// <summary>
        ///     Таблица потоков
        /// </summary>
        private static readonly Dictionary<int, ThreadNode> _ThreadNodes = new Dictionary<int, ThreadNode>();

        /// <summary>
        ///     Таблица объектов блокировки
        /// </summary>
        private static readonly ConditionalWeakTable<object, LockObjectNode> _LockObjectNodes = new ConditionalWeakTable<object, LockObjectNode>();

        #endregion

        #region public methods

        /// <summary>
        ///     Зарегистрировать текущий поток как ожидающиего блокировки <paramref name="lockObject"/> с уровнем доступа <paramref name="level"/>
        /// </summary>
        public static void RegisterWaiting(object lockObject, LockAccessLevel level)
        {
            lock (_WaitGraphLock)
            {
                var node = GetLockObjectNode(lockObject);
                var thread = GetThreadNode(Thread.CurrentThread);
                node.RegisterWaitingThread(thread);
            }
        }

        /// <summary>
        ///     Отменить регистрацию текущего потока как ожидающиего блокировки <paramref name="lockObject"/> с уровнем доступа <paramref name="level"/>
        /// </summary>
        public static void RemoveWaiting(object lockObject, LockAccessLevel level)
        {
            lock (_WaitGraphLock)
            {
                var node = GetLockObjectNode(lockObject);
                var thread = GetThreadNode(Thread.CurrentThread);
                node.RemoveWaitingThread(thread);
            }
        }

        /// <summary>
        ///     Зарегистрировать текущий поток как владельца блокировки <paramref name="lockObject"/> с уровнем доступа <paramref name="level"/>
        /// </summary>
        public static void RegisterOwner(object lockObject, LockAccessLevel level)
        {
            lock (_WaitGraphLock)
            {
                var node = GetLockObjectNode(lockObject);
                var thread = GetThreadNode(Thread.CurrentThread);
                node.RemoveWaitingThread(thread);
                thread.RegisterOwnedLock(node);
            }
        }

        /// <summary>
        ///     Удалить текущий поток как владелец блокировки <paramref name="lockObject"/> с уровнем доступа <paramref name="level"/>
        /// </summary>
        public static void ReleaseOwner(object lockObject, LockAccessLevel level)
        {
            lock (_WaitGraphLock)
            {
                var node = GetLockObjectNode(lockObject);
                var thread = GetThreadNode(Thread.CurrentThread);
                node.RemoveWaitingThread(thread);
                thread.RemoveOwnedLock(node);
            }
        }

        /// <summary>
        ///     Проверить, не создает ли дедлока запрос текущим потоком блокировки <paramref name="lockObject"/> с уровнем доступа <paramref name="level"/>.
        /// </summary>
        public static void VerifyDeadlock(object lockObject, LockAccessLevel level)
        {
            lock (_WaitGraphLock)
            {
                // Пытаемся найти цикл в графе ожидания, который начинается с текущего потока
                // Если такой цикл существует, то имеем дедлок (произвольной глубины)
                var currentThread = GetThreadNode(Thread.CurrentThread);
                var cycle = FindCycle(currentThread);
                if (cycle == null)
                {
                    return;
                }

                // Формируем исключение
                DeadlockException exception;
                try
                {
                    var lockObjectNode = GetLockObjectNode(lockObject);
                    exception = CreateDeadlockException(lockObjectNode, currentThread, cycle, level);
                }
                finally
                {
                    // Отменяем текущую операцию ожидания
                    var node = GetLockObjectNode(lockObject);
                    node.RemoveWaitingThread(currentThread);
                }

                // Бросаем исключение
                throw exception;
            }
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
        public static string GetDiagnostics(bool enableStackTrace)
        {
            var message = new StringBuilder();

            lock (_WaitGraphLock)
            {
                foreach (var threadNode in _ThreadNodes.Values)
                {
                    AppendThreadState(threadNode, message, enableStackTrace);
                }
            }

            return message.ToString();
        }

        /// <summary>
        ///     Получить уровень блокировки объекта <paramref name="lockObject"/> текущим потоком
        /// </summary>
        public static LockAccessLevel GetLockAccessLevel(object lockObject)
        {
            lock (_WaitGraphLock)
            {
                var node = GetLockObjectNode(lockObject);
                var thread = GetThreadNode(Thread.CurrentThread);
                return thread.OwnsLock(node) ? LockAccessLevel.Write : LockAccessLevel.None;
            }
        }

        #endregion

        #region private methods

        private static WaitGraphCycle FindCycle(ThreadNode thread)
        {
            var threads = new HashSet<ThreadNode>();

            while (true)
            {
                // Если поток не ожидает лока, то он не может быть частью цикла
                if (thread.WaitsForLockObject == null)
                {
                    return null;
                }

                // Если поток ожидает лока, коим уже владеет, он не может быть частью цикла,
                // ибо это случай рекурсивной блокировки
                if (thread.WaitsForLockObject.Owner == thread)
                {
                    return null;
                }

                // Если поток уже обрабатывался, то это цикл
                if (threads.Contains(thread))
                {
                    // Обнаружен цикл
                    return new WaitGraphCycle(
                        threads.ToArray(),
                        threads.Select(_ => _.WaitsForLockObject).ToArray()
                        );
                }

                // Объект thread.WaitsForLockObject никем не занят, дедлока нет
                if (thread.WaitsForLockObject.Owner == null)
                {
                    return null;
                }

                // Добавляем поток в стек
                threads.Add(thread);

                // Идем дальше
                thread = thread.WaitsForLockObject.Owner;
            }
        }

        private static DeadlockException CreateDeadlockException(
            LockObjectNode lockObject,
            ThreadNode currentThread,
            WaitGraphCycle cycle,
            LockAccessLevel level)
        {
            var errorMessage = new StringBuilder();

            errorMessage.AppendFormat(
                "Deadlock detected in thread {0} while trying to acquire {1} access to object \"{2}\".\r\n",
                currentThread.Name,
                level,
                lockObject.Name);

            errorMessage.Append("Wait chain: \r\n");

            foreach (var obj in cycle.LockObjects)
            {
                errorMessage.AppendFormat(
                    " * Thread {0} waits for {1} owned by thread {2}\r\n",
                    cycle.Threads.Where(_ => _.WaitsForLockObject == obj).Select(_ => _.Name).FirstOrDefault(),
                    obj.Name,
                    obj.Owner.Name);
            }

            foreach (var thread in cycle.Threads)
            {
                AppendThreadState(thread, errorMessage, true);
            }

            var message = errorMessage.ToString();
            DeadlockMonitor.Callback?.Invoke(message);

            return new DeadlockException(message);
        }

        private static void AppendThreadState(ThreadNode threadNode, StringBuilder message, bool enableStackTrace)
        {
            message.AppendFormat("[Thread {0}] {1:G}\r\n", threadNode.Name, threadNode.Thread.ThreadState);

            if (threadNode.WaitsForLockObject != null)
            {
                message.AppendFormat("Waits for lock {0}\r\n", threadNode.WaitsForLockObject.Name);
            }
            else
            {
                message.AppendFormat("Doesn't wait for any lock\r\n");
            }

            message.AppendFormat(
                "Owned locks: [ {0} ]\r\n",
                string.Join(", ", threadNode.GetOwnedLocks().Select(_ => _.Name)));
            if (enableStackTrace)
            {
                message.AppendLine("Stack trace:");
                AppendStackTrace(threadNode, message);
            }
            message.AppendLine("------------");
        }

        private static void AppendStackTrace(ThreadNode threadNode, StringBuilder message)
        {
#if NETSTANDARD1_6
            message.AppendLine("(not available)");
#elif NET45 || NET46
            string stackTraceString = null;
            var needResume = false;
            try
            {

                if (threadNode.Thread != Thread.CurrentThread)
                {
#pragma warning disable 612, 618
                    threadNode.Thread.Suspend();
#pragma warning restore 612, 618
                    needResume = true;
                }
#pragma warning disable 612, 618
                var stackTrace = new StackTrace(threadNode.Thread, false);
#pragma warning restore 612, 618
                stackTraceString = stackTrace.ToString();
            }
            catch 
            {
                if (stackTraceString == null)
                {
                    stackTraceString = "(not available)";
                }
            }
            finally
            {
                if (needResume)
                {
#pragma warning disable 612, 618
                    threadNode.Thread.Resume();
#pragma warning restore 612, 618
                }
            }

            message.AppendLine(stackTraceString);
#endif
        }

        private static ThreadNode GetThreadNode(Thread thread)
        {
            lock (_WaitGraphLock)
            {
                ThreadNode node;
                if (!_ThreadNodes.TryGetValue(thread.ManagedThreadId, out node))
                {
                    node = new ThreadNode(thread);
                    _ThreadNodes.Add(thread.ManagedThreadId, node);
                }

                return node;
            }
        }

        private static LockObjectNode GetLockObjectNode(object lockObject)
        {
            lock (_WaitGraphLock)
            {
                LockObjectNode node;
                if (! _LockObjectNodes.TryGetValue(lockObject, out node))
                {
                    node = new LockObjectNode(lockObject);
                    _LockObjectNodes.Add(lockObject, node);
                }

                return node;
            }
        }

        #endregion
    }
}

