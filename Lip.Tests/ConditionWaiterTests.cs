using System.Threading;

namespace Lip.Tests;

public class ConditionWaiterTests
{
    [Fact]
    public async Task WaitAsync_TrueCondition_Passes()
    {
        await ConditionWaiter.AsyncWaitFor(() => true);
    }

    [Fact]
    public async Task WaitAsync_FalseConditionTurnsTrue_Passes()
    {
        bool condition = false;
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            condition = true;
        });

        await ConditionWaiter.AsyncWaitFor(() => condition);
    }

    [Fact]
    public async Task WaitAsync_FalseConditionWithTimeout_ThrowsTimeoutException()
    {
        await Assert.ThrowsAsync<TimeoutException>(() => ConditionWaiter.AsyncWaitFor(() => false,
            timeout: TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task WaitAsync_FalseConditionTurnsTrueBeforeTimeout_Passes()
    {
        bool condition = false;
        _ = Task.Run(async () =>
        {
            await Task.Delay(250);
            condition = true;
        });

        await ConditionWaiter.AsyncWaitFor(() => condition, timeout: TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public async Task WaitAsync_FalseConditionTurnsTrueAfterTimeout_ThrowsTimeoutException()
    {
        bool condition = false;
        _ = Task.Run(async () =>
        {
            await Task.Delay(750);
            condition = true;
        });

        await Assert.ThrowsAsync<TimeoutException>(() => ConditionWaiter.AsyncWaitFor(() => condition,
            timeout: TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task WaitAsync_FalseConditionTurnsTrueBeforeTimeoutWithCustomInterval_Passes()
    {
        bool condition = false;
        _ = Task.Run(async () =>
        {
            await Task.Delay(250);
            condition = true;
        });

        await ConditionWaiter.AsyncWaitFor(() => condition, timeout: TimeSpan.FromMilliseconds(500),
            interval: TimeSpan.FromMilliseconds(100));
    }
}
