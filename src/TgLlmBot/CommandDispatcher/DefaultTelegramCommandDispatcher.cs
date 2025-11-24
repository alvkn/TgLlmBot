using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TgLlmBot.Commands.ChatWithLlm;
using TgLlmBot.Commands.DisplayHelp;
using TgLlmBot.Commands.Model;
using TgLlmBot.Commands.Ping;
using TgLlmBot.Commands.Rating;
using TgLlmBot.Commands.Repo;
using TgLlmBot.Commands.ResetSystemPrompt;
using TgLlmBot.Commands.SetSystemPrompt;
using TgLlmBot.Commands.Usage;
using TgLlmBot.Services.DataAccess;
using TgLlmBot.Services.Telegram.SelfInformation;
using RatingCommandHandler = TgLlmBot.Commands.Rating.RatingCommandHandler;

namespace TgLlmBot.CommandDispatcher;

public class DefaultTelegramCommandDispatcher : ITelegramCommandDispatcher
{
    private static readonly HashSet<MessageType> AllowedMessageTypes =
    [
        MessageType.Text,
        MessageType.Photo
    ];

    private readonly ChatWithLlmCommandHandler _chatWithLlmCommandHandler;
    private readonly DisplayHelpCommandHandler _displayHelpCommandHandler;
    private readonly ITelegramMessageStorage _messageStorage;
    private readonly ModelCommandHandler _modelCommandHandler;

    private readonly DefaultTelegramCommandDispatcherOptions _options;
    private readonly PingCommandHandler _pingCommandHandler;
    private readonly RatingCommandHandler _ratingCommandHandler;
    private readonly RepoCommandHandler _repoCommandHandler;
    private readonly ResetSystemPromptCommandHandler _resetSystemPromptCommandHandler;
    private readonly ITelegramSelfInformation _selfInformation;
    private readonly SetSystemPromptCommandHandler _setSystemPromptCommandHandler;
    private readonly UsageCommandHandler _usageCommandHandler;

    public DefaultTelegramCommandDispatcher(
        DefaultTelegramCommandDispatcherOptions options,
        ITelegramSelfInformation selfInformation,
        ITelegramMessageStorage messageStorage,
        DisplayHelpCommandHandler displayHelpCommandHandler,
        ChatWithLlmCommandHandler chatWithLlmCommandHandler,
        PingCommandHandler pingCommandHandler,
        RepoCommandHandler repoCommandHandler,
        ModelCommandHandler modelCommandHandler,
        UsageCommandHandler usageCommandHandler,
        RatingCommandHandler ratingCommandHandler,
        SetSystemPromptCommandHandler setSystemPromptCommandHandler,
        ResetSystemPromptCommandHandler resetSystemPromptCommandHandler)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(selfInformation);
        ArgumentNullException.ThrowIfNull(messageStorage);
        ArgumentNullException.ThrowIfNull(displayHelpCommandHandler);
        ArgumentNullException.ThrowIfNull(chatWithLlmCommandHandler);
        ArgumentNullException.ThrowIfNull(pingCommandHandler);
        ArgumentNullException.ThrowIfNull(repoCommandHandler);
        ArgumentNullException.ThrowIfNull(modelCommandHandler);
        ArgumentNullException.ThrowIfNull(usageCommandHandler);
        ArgumentNullException.ThrowIfNull(ratingCommandHandler);
        ArgumentNullException.ThrowIfNull(setSystemPromptCommandHandler);
        ArgumentNullException.ThrowIfNull(resetSystemPromptCommandHandler);
        _options = options;
        _selfInformation = selfInformation;
        _messageStorage = messageStorage;
        _displayHelpCommandHandler = displayHelpCommandHandler;
        _chatWithLlmCommandHandler = chatWithLlmCommandHandler;
        _pingCommandHandler = pingCommandHandler;
        _repoCommandHandler = repoCommandHandler;
        _modelCommandHandler = modelCommandHandler;
        _usageCommandHandler = usageCommandHandler;
        _ratingCommandHandler = ratingCommandHandler;
        _setSystemPromptCommandHandler = setSystemPromptCommandHandler;
        _resetSystemPromptCommandHandler = resetSystemPromptCommandHandler;
    }

    public async Task HandleMessageAsync(Message? message, UpdateType type, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (message is null)
        {
            return;
        }

        if (!AllowedMessageTypes.Contains(message.Type))
        {
            return;
        }

        var self = _selfInformation.GetSelf();
        await _messageStorage.StoreMessageAsync(message, self, cancellationToken);
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        var rawPrompt = $"{message.Text?.Trim()?.ToLowerInvariant()}";
        switch (rawPrompt)
        {
            case "!help":
                {
                    var command = new DisplayHelpCommand(message, type);
                    await _displayHelpCommandHandler.HandleAsync(command, cancellationToken);
                    return;
                }
            case "!ping":
                {
                    var command = new PingCommand(message, type);
                    await _pingCommandHandler.HandleAsync(command, cancellationToken);
                    return;
                }
            case "!repo":
                {
                    var command = new RepoCommand(message, type);
                    await _repoCommandHandler.HandleAsync(command, cancellationToken);
                    return;
                }
            case "!model":
                {
                    var command = new ModelCommand(message, type);
                    await _modelCommandHandler.HandleAsync(command, cancellationToken);
                    return;
                }
            case "!usage":
                {
                    var command = new UsageCommand(message, type);
                    await _usageCommandHandler.HandleAsync(command, cancellationToken);
                    return;
                }
            case "!rating":
                {
                    var command = new RatingCommand(message, type, self);
                    await _ratingCommandHandler.HandleAsync(command, cancellationToken);
                    return;
                }
            case "!reset":
                {
                    var command = new ResetSystemPromptCommand(message, type, self);
                    await _resetSystemPromptCommandHandler.HandleAsync(command, cancellationToken);
                    return;
                }
        }

        if (rawPrompt.StartsWith("!role", StringComparison.Ordinal))
        {
            var command = new SetSystemPromptCommand(message, type, self);
            await _setSystemPromptCommandHandler.HandleAsync(command, cancellationToken);
            return;
        }

        var prompt = message.Text ?? message.Caption;
        if (message.Chat.Type == ChatType.Private)
        {
            var command = new ChatWithLlmCommand(message, type, self, prompt);
            await _chatWithLlmCommandHandler.HandleAsync(command, cancellationToken);
        }
        else if (message.Chat.Type is ChatType.Group or ChatType.Supergroup)
        {
            if (prompt?.StartsWith(_options.BotName, StringComparison.OrdinalIgnoreCase) is true
                || message.ReplyToMessage?.From?.Id == self.Id)
            {
                var command = new ChatWithLlmCommand(message, type, self, prompt);
                await _chatWithLlmCommandHandler.HandleAsync(command, cancellationToken);
            }
        }
    }
}
