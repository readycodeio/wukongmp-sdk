using System.Collections.Generic;
using ReadyM.Api.Command;

namespace WukongMp.Sdk.Api;

public interface IWukongConsoleApi : IConsoleCommandRegistry
{
    void AddCommand(string commandName, ConsoleCommand command, IEnumerable<string>? availableFirstParams = null);
    bool HasCommand(string commandName);
    void WriteConsoleMessage(string message);
}