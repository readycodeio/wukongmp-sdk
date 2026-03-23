namespace WukongMp.Sdk;

/// <exclude />
public interface IReadyConvertable<TSelf, TType>
    where TSelf : struct, IReadyEntity<TSelf>
    where TType : struct, IReadyEntity<TType>;