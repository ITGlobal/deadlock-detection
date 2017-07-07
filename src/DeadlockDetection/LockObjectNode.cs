using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using JetBrains.Annotations;

namespace ITGlobal.DeadlockDetection
{
    [DebuggerDisplay("LockObject {Name}")]
    internal sealed class LockObjectNode
    {
        private readonly List<ThreadNode> waitingThreads = new List<ThreadNode>();

        private int refCounter;

        public LockObjectNode(object lockObject)
        {
            LockObject = lockObject;
        }

        [NotNull]
        public object LockObject { get; }

        public ThreadNode Owner { get; private set; }

        public string Name
        {
            get
            {
                var namedObject = LockObject as INamedLockObject;
                if (namedObject != null)
                {
                    return namedObject.Name;
                }

                return LockObject.ToString();
            }
        }

        public void RegisterWaitingThread(ThreadNode thread)
        {
            thread.WaitsForLockObject = this;
            waitingThreads.Add(thread);
        }

        public void RemoveWaitingThread(ThreadNode thread)
        {
            thread.WaitsForLockObject = null;
            waitingThreads.Remove(thread);
        }

        public IEnumerable<ThreadNode> GetWaitingThreads()
        {
            return waitingThreads;
        }

        public bool SetOwner(ThreadNode thread)
        {
            if (Owner != thread)
            {
                Interlocked.Exchange(ref refCounter, 1);
                Owner = thread;
                return true;
            }
            
            Interlocked.Increment(ref refCounter);
            return false;
        }

        public bool RemoveOwner(ThreadNode thread)
        {
            if (Owner != thread)
            {
                return true;
            }
            
            if (Interlocked.Decrement(ref refCounter) <= 0)
            {
                Owner = null;
                return true;
            }

            return false;
        }

        #region equality members

        private bool Equals(LockObjectNode other)
        {
            return LockObject.Equals(other.LockObject);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is LockObjectNode && Equals((LockObjectNode)obj);
        }

        public override int GetHashCode()
        {
            return LockObject.GetHashCode();
        }

        public static bool operator ==(LockObjectNode left, LockObjectNode right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LockObjectNode left, LockObjectNode right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}

