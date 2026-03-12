namespace WukongMp.Api.Https;

public readonly struct BlobInfo(string name, byte[] content)
{
    public string Name { get; } = name;
    public byte[] Content { get; } = content;
}