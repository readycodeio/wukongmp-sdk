using System;

namespace WukongMp.Api.Command;

public class ConsoleCommand(Action<ReadOnlyMemory<string>> handler)
{
    public Action<ReadOnlyMemory<string>> Handler { get; } = handler;
}
