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

    private void AddFormattedMessage(string template, params string[] placeholders)
    {
        _widgetManager.AddMessageToConsole(string.Format(template, [.. placeholders]));
    }

    private void AddLocalizedCommandError(CommandError error)
    {
        switch (error)
        {
            case CommandError.InvalidCommandFormat(var input, _):
                AddFormattedMessage(BuiltinTexts.InvalidCommandFormat, input);
                break;
            case CommandError.InvalidArgumentFormat(var input, var argIndex, var position):
                AddFormattedMessage(BuiltinTexts.InvalidArgumentFormat, input, argIndex.ToString(), position.ToString());
                break;
            case CommandError.UnrecognizedCommand(var commandName):
                AddFormattedMessage(BuiltinTexts.UnrecognizedCommand, commandName);
                break;
            case CommandError.TooFewArguments(var minCount, var actual):
                AddFormattedMessage(BuiltinTexts.TooFewArguments, minCount.ToString(), actual.ToString());
                break;
            case CommandError.TooManyArguments(var maxCount, var actual):
                AddFormattedMessage(BuiltinTexts.TooManyArguments, maxCount.ToString(), actual.ToString());
                break;
            case CommandError.InvalidArgumentType(var argIndex, var expectedType, var actualType):
                AddFormattedMessage(BuiltinTexts.InvalidArgumentType, argIndex.ToString(),
                    ArgumentTypeName(expectedType), ArgumentTypeName(actualType));
                break;
            case CommandError.ExecutionError(var exception):
                AddFormattedMessage(BuiltinTexts.ExecutionError, exception.Message);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// A reader-friendly name for a command argument type. Falls back to the CLR name, so a mod
    /// declaring a parameter type we have no wording for still gets a usable message.
    /// </summary>
    private static string ArgumentTypeName(Type type)
    {
        if (type == typeof(int))
            return BuiltinTexts.CommandArgumentTypeInteger;

        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            return BuiltinTexts.CommandArgumentTypeNumber;

        if (type == typeof(string) || type == typeof(Ident))
            return BuiltinTexts.CommandArgumentTypeText;

        if (type == typeof(bool))
            return BuiltinTexts.CommandArgumentTypeBoolean;

        return type.Name;
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