using System.Collections.Generic;
using ReadyM.Api.Command;

namespace WukongMp.Sdk.Api;

public interface IWukongConsoleApi
{
    void AddCommands(IEnumerable<IConsoleCommandRegistration> registrations);
    void WriteConsoleMessage(string message);
}