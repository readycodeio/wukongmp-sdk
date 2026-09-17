using System;

namespace WukongMp.Sdk;

/// <exclude />
[Obsolete("Part of old 0.x SDK. Use SDK 1.0 methods instead.")]
public interface IReadyConvertable<TSelf, TType>
    where TSelf : struct, IReadyEntity<TSelf>
    where TType : struct, IReadyEntity<TType>;