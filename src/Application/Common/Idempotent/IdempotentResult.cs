namespace Application.Common.Idempotent;

public sealed record IdempotentResult<T>(
    T Value,
    bool IsReplay)
{
    public static IdempotentResult<T> Created(T value) =>
        new(value, IsReplay: false);

    public static IdempotentResult<T> Replay(T value) =>
        new(value, IsReplay: true);
}