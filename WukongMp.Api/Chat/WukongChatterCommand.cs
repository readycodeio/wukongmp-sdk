using System;

namespace WukongMp.Api.Chat;

internal class WukongChatterCommand(Action<ReadOnlyMemory<string>> handler)
{
    public Action<ReadOnlyMemory<string>> Handler { get; } = handler;
}
