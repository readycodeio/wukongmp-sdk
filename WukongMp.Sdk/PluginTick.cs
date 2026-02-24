namespace WukongMp.Sdk;

public readonly struct PluginTick(float deltaTime, float totalTime)
{
    public readonly float DeltaTime = deltaTime;
    public readonly float TotalTime = totalTime;
}