using System;
using System.Threading.Tasks;
using WebSocketManager.Client;
using WebSocketManager.Common;

public class Program
{
    private static Connection _connection;
    private static RawStringInvocationStrategy _strategy;

    public static void Main(string[] args)
    {
        StartConnectionAsync();

        _strategy.OnRecieveMessage( (arguments) =>
        {
            Console.WriteLine($"{arguments[0]}");
        });

        Console.ReadLine();
        StopConnectionAsync();
    }

    public static async Task StartConnectionAsync()
    {
        _strategy = new RawStringInvocationStrategy();
        _connection = new Connection(_strategy);
        await _connection.StartConnectionAsync("ws://localhost:5000/ws/ALL_7de39c0ebb9c4dc0ba6acd9d4251ed7e");
    }

    public static async Task StopConnectionAsync()
    {
        await _connection.StopConnectionAsync();
    }
}