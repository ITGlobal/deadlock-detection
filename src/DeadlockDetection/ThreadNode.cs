using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using JetBrains.Annotations;

namespace ITGlobal.DeadlockDetection
{
    [DebuggerDisplay("Thread {Name}")]
    internal sealed class ThreadNode
    {
        [NotNull]
        private readonly Thread thread;
        private readonly HashSet<LockObjectNode> ownedLocks = new HashSet<LockObjectNode>();

        public ThreadNode(Thread thread)
        {
            this.thread = thread;
        }

        public Thread Thread { get { return thread; } }

        public LockObjectNode WaitsForLockObject { get; set; }

        public string Name
        {
            get
            {
                if (!string.IsNullOrEmpty(thread.Name))
                {
                    return string.Format("#{0} \"{1}\"", thread.ManagedThreadId, thread.Name);
                }

                return string.Format("#{0}", thread.ManagedThreadId);
            }
        }

        public void RegisterOwnedLock(LockObjectNode node)
        {
            if (node.SetOwner(this))
            {
                ownedLocks.Add(node);
            }
        }

        public void RemoveOwnedLock(LockObjectNode node)
        {
            if (node.RemoveOwner(this))
            {
                ownedLocks.Remove(node);
            }
        }

        public bool OwnsLock(LockObjectNode node)
        {
            return ownedLocks.Contains(node);
        }

        public IEnumerable<LockObjectNode> GetOwnedLocks()
        {
            return ownedLocks;
        }

        #region equality members

        private bool Equals(ThreadNode other)
        {
            return thread.Equals(other.thread);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is ThreadNode && Equals((ThreadNode)obj);
        }

        public override int GetHashCode()
        {
            return thread.GetHashCode();
        }

        public static bool operator ==(ThreadNode left, ThreadNode right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ThreadNode left, ThreadNode right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}

