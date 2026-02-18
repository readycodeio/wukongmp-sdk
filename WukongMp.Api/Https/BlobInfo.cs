namespace WukongMp.Api.Https;

public class BlobInfo(string name, byte[] content)
{
    public string Name { get; } = name;
    public byte[] Content { get; } = content;
}