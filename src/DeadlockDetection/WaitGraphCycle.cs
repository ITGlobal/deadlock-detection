namespace ITGlobal.DeadlockDetection
{
    internal sealed class WaitGraphCycle
    {
        private readonly ThreadNode[] threads;
        private readonly LockObjectNode[] lockObjects;

        public WaitGraphCycle(ThreadNode[] threads, LockObjectNode[] lockObjects)
        {
            this.threads = threads;
            this.lockObjects = lockObjects;
        }

        public ThreadNode[] Threads { get { return threads; } }

        public LockObjectNode[] LockObjects { get { return lockObjects; } }
    }
}

