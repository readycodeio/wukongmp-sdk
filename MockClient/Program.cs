// See https://aka.ms/new-console-template for more information

using System;
using WukongMp.Common;

public class Program
{
    public static void Main()
    {
        var client = new WukongClient();
        client.StartClient();

        while (true)
        {
            if (Console.KeyAvailable)
            {
                var keyInfo = Console.ReadKey();
                client.SendKeyClick(keyInfo.Key);
            }
        }
    }
}