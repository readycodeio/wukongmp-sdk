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
                float x = 0;
                float y = 0;
                const float force = 200;

                var keyInfo = Console.ReadKey(intercept: true); // intercept true to prevent key from being shown

                switch (keyInfo.Key)
                {
                    case ConsoleKey.W:
                        x += force;
                        break;
                    case ConsoleKey.S:
                        x -= force;
                        break;
                    case ConsoleKey.A:
                        y -= force;
                        break;
                    case ConsoleKey.D:
                        y += force;
                        break;
                    default:
                        continue;
                }

                client.SendPositionUpdate(x, y, 0);
            }
        }
    }
}