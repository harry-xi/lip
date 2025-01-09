namespace Lip;

public partial class Lip
{
    /// <summary>
    /// The type definition of user interaction event that occurred.
    /// </summary>
    public enum UserInteractionEventType
    {
        /// <summary>
        /// The user is prompted to provide the tooth path of the package in lip init.
        /// </summary>
        InitTooth,

        /// <summary>
        /// The user is prompted to provide the version of the package in lip init.
        /// </summary>
        InitVersion,

        /// <summary>
        /// The user is prompted to provide the name of the package in lip init.
        /// </summary>
        InitName,

        /// <summary>
        /// The user is prompted to provide the description of the package in lip init.
        /// </summary>
        InitDescription,

        /// <summary>
        /// The user is prompted to provide the author of the package in lip init.
        /// </summary>
        InitAuthor,

        /// <summary>
        /// The user is prompted to provide the avatar URL of the package in lip init.
        /// </summary>
        InitAvatarUrl,

        /// <summary>
        /// The user is prompted to confirm the initialization of the package in lip init.
        /// </summary>
        InitConfirm,
    }

    public class UserInteractionEventArgs(UserInteractionEventType eventType, List<string>? promptMessages) : EventArgs
    {
        /// <summary>
        /// The type of user interaction event that occurred.
        /// </summary>
        public UserInteractionEventType EventType { get; } = eventType;

        public List<string> PromptMessages { get; } = promptMessages ?? [];

        /// <summary>
        /// The input provided by the user.
        /// </summary>
        public string? Input { get; set; }
    }

    public event EventHandler<UserInteractionEventArgs>? OnUserInteractionPrompted;

    /// <summary>
    /// Prompts the user to provide input for the specified user interaction event.
    /// </summary>
    /// <param name="eventType">The type of user interaction event to prompt the user for input.</param>
    /// <param name="promptMessages">The messages to display to the user when prompting for input.</param>
    /// <returns>The input provided by the user.</returns>
    private async Task<string?> PromptUserInteraction(UserInteractionEventType eventType, List<string>? promptMessages = null)
    {
        UserInteractionEventArgs eventArgs = new(eventType, promptMessages);
        OnUserInteractionPrompted?.Invoke(this, eventArgs);

        await ConditionWaiter.AsyncWaitFor(() => eventArgs.Input != null);

        return eventArgs.Input;
    }
}
