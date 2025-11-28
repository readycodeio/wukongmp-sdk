using System;

namespace WukongMp.Api.Chat;

public class WukongChatterCommand(Action<ReadOnlyMemory<string>> handler)
{
    public Action<ReadOnlyMemory<string>> Handler { get; } = handler;
}
