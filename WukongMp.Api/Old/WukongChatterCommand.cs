using System;

namespace WukongMp.Api.Old;

internal class WukongChatterCommand(Action<ReadOnlyMemory<string>> handler)
{
    public Action<ReadOnlyMemory<string>> Handler { get; } = handler;
}
