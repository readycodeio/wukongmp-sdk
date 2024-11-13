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
            // Check if a key is pressed without blocking
            if (Console.KeyAvailable)
            {
                float x = 0;
                float y = 0;

                var keyInfo = Console.ReadKey(intercept: true); // intercept true to prevent key from being shown

                switch (keyInfo.Key)
                {
                    case ConsoleKey.W:
                        x += 10;
                        break;
                    case ConsoleKey.S:
                        x -= 10;
                        break;
                    case ConsoleKey.A:
                        y -= 10;
                        break;
                    case ConsoleKey.D:
                        y += 10;
                        break;
                    default:
                        continue;
                }

                client.SendPositionUpdate(x, y, 0);
            }
        }
    }
}