using ReadyM.Relay.Client;

using var client = new RelayClient();

client.Start();

Console.WriteLine("Press any key to stop the client...");
Console.ReadKey();