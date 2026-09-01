using System;
using System.Collections.Generic;
using ReadyM.Api.Command;
using WukongMp.Api.Chat;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;

namespace WukongMp.Api.Command;

internal class WukongCommandConsole : IDisposable
{
    private readonly ConsoleCommandMatcher _matcher;
    private readonly WukongPlayerState _playerState;
    private readonly WukongWidgetManager _widgetManager;

    private bool UseDebugCommands
#if DEBUG
        => true;
#else
        => false;
#endif

    public WukongCommandConsole(
        ConsoleCommandMatcher matcher,
        WukongPlayerState playerState,
        WukongWidgetManager widgetManager)
    {
        Logging.LogDebug("Initializing WukongCommandConsole");

        _matcher = matcher;
        _playerState = playerState;
        _widgetManager = widgetManager;
    }

    public void Dispose()
    {
        Logging.LogDebug("Disposing WukongCommandConsole");
    }

    public void ProcessCommand(string command)
    {
        if (!string.IsNullOrWhiteSpace(command))
        {
            command = command.Trim();
            TryExecuteCommand(command);
        }
    }

    public void AddMessage(string message)
    {
        _widgetManager.AddMessageToConsole(message);
    }

    public void AddLocalizedMessage(string message, params string[] placeholders)
    {
        var translatedMessage = string.Format(BuiltinTexts.ResourceManager.GetString(message, BuiltinTexts.Culture)!, [.. placeholders]);
        _widgetManager.AddMessageToConsole(translatedMessage);
    }

    private void AddLocalizedCommandError(CommandError error)
    {
        switch (error)
        {
            case CommandError.InvalidCommandFormat(var input, _):
                AddLocalizedMessage(nameof(CommandError.InvalidCommandFormat), input);
                break;
            case CommandError.InvalidArgumentFormat(var input, var argIndex, var position):
                AddLocalizedMessage(nameof(CommandError.InvalidArgumentFormat), input, argIndex.ToString(), position.ToString());
                break;
            case CommandError.UnrecognizedCommand(var commandName):
                AddLocalizedMessage(nameof(CommandError.UnrecognizedCommand), commandName);
                break;
            case CommandError.TooFewArguments(var minCount, var actual):
                AddLocalizedMessage(nameof(CommandError.TooFewArguments), minCount.ToString(), actual.ToString());
                break;
            case CommandError.TooManyArguments(var maxCount, var actual):
                AddLocalizedMessage(nameof(CommandError.TooManyArguments), maxCount.ToString(), actual.ToString());
                break;
            case CommandError.InvalidArgumentType(var argIndex, var expectedType, var actualType):
            {
                var expectedTypeName = BuiltinTexts.ResourceManager.GetString($"CommandArgumentType.{expectedType.Name}", BuiltinTexts.Culture)!;
                var actualTypeName = BuiltinTexts.ResourceManager.GetString($"CommandArgumentType.{actualType.Name}", BuiltinTexts.Culture)!;
                AddLocalizedMessage(nameof(CommandError.InvalidArgumentType), argIndex.ToString(), expectedTypeName, actualTypeName);
                break;
            }
            case CommandError.ExecutionError(var exception):
                AddLocalizedMessage(nameof(CommandError.ExecutionError), exception.Message);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private bool TryExecuteCommand(string message)
    {
        if (!_matcher.TryMatch(message, out var command, out var parsedCommandCall, out var error))
        {
            AddLocalizedCommandError(error);
            return false;
        }

        if (command.Value.IsDebugOnly && !UseDebugCommands)
        {
            return false;
        }

        if (CanExecuteCommand())
        {
            try
            {
                command.Value.Handler.DynamicInvoke(parsedCommandCall.Value.Args);
            }
            catch (Exception ex)
            {
                error = new CommandError.ExecutionError(ex);
                AddLocalizedCommandError(error);
                return false;
            }
        }

        return true;
    }

    private bool CanExecuteCommand()
    {
        return !_playerState.LocalMainCharacter.HasValue || !_playerState.LocalMainCharacter.Value.GetLocalState().IsInSequence;
    }

    public List<string> GetAvailableCommands()
        => [.. _matcher.Registry.GetCommandNames(UseDebugCommands)];

    public List<string> GetAvailableFirstParams(string commandName)
        => _matcher.Registry.GetCommandAvailableFirstParams(commandName);
}