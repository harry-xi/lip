using Lip.Core;
using Spectre.Console;
using System.Collections.Concurrent;

namespace Lip;

public class UserInteraction : IUserInteraction
{
    private readonly ConcurrentDictionary<string, (float, string)> _progressUpdates = new();

    public async Task<bool> Confirm(string format, params object[] args)
    {
        return await AnsiConsole.ConfirmAsync(string.Format(format, args));
    }

    public async Task<string?> PromptForInput(string format, params object[] args)
    {
        return await AnsiConsole.AskAsync<string>(string.Format(format, args));
    }

    public async Task<string> PromptForSelection(IEnumerable<string> options, string format, params object[] args)
    {
        return await AnsiConsole.PromptAsync(new SelectionPrompt<string>()
            .Title(string.Format(format, args))
            .AddChoices(options)
        );
    }

    public async Task RunProgressService()
    {
        await AnsiConsole.Progress()
        .Columns([
            new TaskDescriptionColumn()
            {
                Alignment = Justify.Left,
            },
            new ProgressBarColumn(),
            new PercentageColumn(),
            new RemainingTimeColumn(),
            new SpinnerColumn(),
        ])
        .StartAsync(async ctx =>
        {
            Dictionary<string, ProgressTask> tasks = [];

            while (true)
            {
                await Task.Delay(100); // Same as Progress.RefreshRate.

                foreach (var (id, (progress, description)) in _progressUpdates)
                {
                    if (!tasks.TryGetValue(id, out ProgressTask? value))
                    {
                        value = ctx.AddTask(description);
                        tasks[id] = value;
                    }

                    value.Description = description;
                    value.Value = progress;
                }

                // Here some updates may be ignored, but we don't know how to handle it.

                _progressUpdates.Clear();
            }
        });
    }

    public async Task UpdateProgress(string id, float progress, string format, params object[] args)
    {
        await Task.Delay(0); // Suppress warning.

        _progressUpdates[id] = new(progress * 100, string.Format(format, args));
    }
}
