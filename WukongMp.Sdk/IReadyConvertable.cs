namespace WukongMp.Sdk;

public interface IReadyConvertable<TSelf, TType>
    where TSelf : struct, IReadyEntity<TSelf>
    where TType : struct, IReadyEntity<TType>
{
    // empty
}