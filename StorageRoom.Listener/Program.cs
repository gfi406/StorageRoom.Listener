using StorageRoom.Messages;
using Microsoft.Extensions.Configuration;
using EasyNetQ;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System;
using System.IO;
using System.Threading.Tasks;

var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

IConfiguration config = builder.Build();
var amqp = config.GetConnectionString("AutoRabbitMQ");

// Подключение к RabbitMQ
using var bus = RabbitHutch.CreateBus(amqp);
Console.WriteLine("Подключился к RabbitMQ!");



// Подписка на очередь сообщений RabbitMQ
var subscriberId = $"Listener@{Environment.MachineName}";
try
{
    // Подписка на очередь сообщений
    await bus.PubSub.SubscribeAsync<BaggageMessages>(subscriberId, CreateTicket);
    Console.WriteLine("Слушаю RabbitMQ. Жду сообщения...");
    Console.ReadLine(); // Ожидаем сообщений
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка при подписке: {ex.Message}");
}

// Обработчик сообщений
async void CreateTicket(BaggageMessages message)
{
    try
    {
        Console.WriteLine($"Получено сообщение для багажного ярлыка: {message.BaggageTag}_{message.BaggageId}_{message.Weight}");
        Console.WriteLine($"BaggageId: {message.BaggageId}");
       
        CreatePdfTicket(message);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при создании билета: {ex.Message}");
    }
}

// Создание PDF билета
async void CreatePdfTicket(BaggageMessages message)
{
    // Имя файла PDF
    string fileName = $"BaggageTicket_{message.BaggageId}.pdf";
    string filePath = Path.Combine("Tickets", fileName);
    Directory.CreateDirectory("Tickets");

    // Создание документа
    using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
    var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A5);
    PdfWriter.GetInstance(doc, fs);
    doc.Open();

    // Заголовок
    var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
    var title = new iTextSharp.text.Paragraph($"Baggage Claim Ticket", titleFont)
    {
        Alignment = iTextSharp.text.Element.ALIGN_CENTER
    };
    doc.Add(title);
    doc.Add(new iTextSharp.text.Paragraph("\n"));

    await Task.Delay(10000);
    // Основная информация
    var contentFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
    doc.Add(new iTextSharp.text.Paragraph($"Baggage Tag: {message.BaggageTag}", contentFont));
    doc.Add(new iTextSharp.text.Paragraph($"Weight: {message.Weight} kg", contentFont));
    doc.Add(new iTextSharp.text.Paragraph($"Baggage ID: {message.BaggageId}", contentFont));
    doc.Add(new iTextSharp.text.Paragraph("\n"));

    // Завершение документа
    doc.Close();

    Console.WriteLine($"PDF-чек создан: {filePath}");
}

