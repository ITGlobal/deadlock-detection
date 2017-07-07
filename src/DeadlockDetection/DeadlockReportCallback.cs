using JetBrains.Annotations;

namespace ITGlobal.DeadlockDetection
{
    /// <summary>
    ///     Коллбек для логирования отчетов о дедлоках
    /// </summary>
    [PublicAPI]
    public delegate void DeadlockReportCallback(string errorMessage);
}