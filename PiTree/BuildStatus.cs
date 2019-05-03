namespace PiTree
{
    public enum BuildStatus
    {
        None = 0,
        Succeeded = 1,
        PartiallySucceeded = 2,
        SucceededAndPartiallySucceeded = Succeeded | PartiallySucceeded,
        Failed = 4,
        SucceededAndFailed = Succeeded | Failed,
        PartiallySucceededAndFailed = PartiallySucceeded | Failed,
        All = Succeeded | PartiallySucceeded | Failed,
    }
}
