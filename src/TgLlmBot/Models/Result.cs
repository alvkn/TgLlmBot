using System.Diagnostics.CodeAnalysis;

namespace TgLlmBot.Models;

/// <summary>
///     Generic result of executing any operation.
/// </summary>
/// <typeparam name="TOk">The data type returned in case of a successful operation execution.</typeparam>
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
public class Result<TOk>
{
    private Result()
    {
        IsFailed = true;
    }

    private Result(TOk value)
    {
        Value = value;
        IsFailed = false;
    }

    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsFailed { get; }

    /// <summary>
    ///     Result of a successful operation execution.
    /// </summary>
    public TOk? Value { get; }

    /// <summary>
    ///     Returns a result indicating the successful completion of the operation.
    /// </summary>
    /// <param name="result">The result of a successful operation completion.</param>
    /// <returns>The <see cref="Result{TOk}" /> corresponding to the successful execution of the operation.</returns>
    public static Result<TOk> Success(TOk result)
    {
        return new(result);
    }

    /// <summary>
    ///     Returns a result indicating the unsuccessful execution of the operation.
    /// </summary>
    /// <returns>The <see cref="Result{TOk}" /> corresponding to the unsuccessful execution of the operation.</returns>
    public static Result<TOk> Fail()
    {
        return new();
    }
}
