namespace WukongMp.Sdk.Api;

public interface IWukongFileApi
{
    string GetSaveFileFullName<T>(string slotName) where T : ModBase;
}