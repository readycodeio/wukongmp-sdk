using ReadyM.Api.Command;

namespace WukongMp.Sdk.Api;

public interface IWukongConsoleApi
{
    void AddCommands(params IConsoleCommandRegistration[] registrations);
    void WriteConsoleMessage(string message);
}