using StorageRoom.Messages;
using Microsoft.Extensions.Configuration;
using EasyNetQ;
using System;

var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

IConfiguration config = builder.Build();

var amqp = config.GetConnectionString("AutoRabbitMQ");
using var bus = RabbitHutch.CreateBus(amqp);
Console.WriteLine("Подключился!");

var subscriberId = $"Listener@{Environment.MachineName}";
Console.WriteLine("Слушаю RabbitMQ. Жду сообщения...");

bus.Subscribe<Message>("my_queue", HandleMessage);

Console.ReadLine();

// Метод для обработки сообщений
void HandleMessage(Message message)
{
    Console.WriteLine($"Получено сообщение: {message.Content}");
    // Здесь можно добавить любую логику для обработки полученного сообщения
}
