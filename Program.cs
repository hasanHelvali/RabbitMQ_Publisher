using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace RabbitMQ_Producer
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            ConnectionFactory connectionFactory = new ConnectionFactory();

            connectionFactory.Uri = new Uri("amqp://guest:guest@localhost:5672/");

            using IConnection connection = connectionFactory.CreateConnection();
            using IModel channel = connection.CreateModel();

            #region P2P Tasarımı
            string queueName = "example-p2p-queue";

            channel.QueueDeclare(
                queue: queueName,
                durable: false,
                exclusive: false,
                autoDelete: false);
            byte[] message = Encoding.UTF8.GetBytes("Selamun Aleykum");

            channel.BasicPublish(
                exchange: "",
                routingKey: queueName,
                body: message);

            Console.ReadLine();
            #endregion

            #region Publish/Subscribe Tasarımı
            string exchangeName = "example-pub-sub-exchange";

            channel.ExchangeDeclare(
                exchange: exchangeName,
                type: ExchangeType.Fanout);

            for (int i = 0; i < 100; i++)
            {
                await Task.Delay(200);
                byte[] message = Encoding.UTF8.GetBytes("Selamun Aleykum "+i);
                channel.BasicPublish(
                exchange: exchangeName,
                routingKey: string.Empty,
                body: message);
            }

            #endregion

            #region Work Queue Tasarımı
            string queueName = "example-work-queue";

            channel.QueueDeclare(
                queue: queueName,
                durable: false,
                exclusive: false,
                autoDelete: false);

            for (int i = 0; i < 100; i++)
            {
                await Task.Delay(200);

                byte[] message = Encoding.UTF8.GetBytes("Selamun Aleykum " + i);
                
                channel.BasicPublish(
                exchange: ,string.Empty,
                routingKey: queueName,
                body: message);
            }

            #endregion

            #region Request/Response Tasarımı
            string requestQueueName = "example-request-response-queue";
            
            channel.QueueDeclare(
                queue: requestQueueName,
                durable: false,
                exclusive: false,
                autoDelete: false);
            string replyQueueName = channel.QueueDeclare().QueueName;

            string correlationId=Guid.NewGuid().ToString();

            #region request mesajını olusturma ve gonderme
            IBasicProperties basicProperties = channel.CreateBasicProperties();
            basicProperties.CorrelationId = correlationId;
            basicProperties.ReplyTo= replyQueueName;
            for (int i = 0; i < 100; i++)
            {
                byte[] byteMessage = Encoding.UTF8.GetBytes($"Selamun Aleykum {i}");
                channel.BasicPublish(
                exchange: string.Empty,
                routingKey: requestQueueName,
                body: byteMessage,
                basicProperties: basicProperties);
            }
            #endregion

            #region response kuyrugunu dinleme
            EventingBasicConsumer cunsomer = new EventingBasicConsumer(channel);
            channel.BasicConsume(queue: replyQueueName, autoAck: true, cunsomer);
            cunsomer.Received += (sender, e) =>
            {
                if (e.BasicProperties.CorrelationId==correlationId)
                {
                    Console.WriteLine($"Response {Encoding.UTF8.GetString(e.Body.Span)}");
                }
            };
            #endregion

            #endregion


        }
    }
}
