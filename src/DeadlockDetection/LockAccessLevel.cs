namespace ITGlobal.DeadlockDetection
{
    /// <summary>
    ///     Запрашиваемый уровень блокировки
    /// </summary>
    internal enum LockAccessLevel
    {
        None,
        Read,
        UpgradeableRead,
        Write
    }
}

