using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Property_and_Management.src.Interface;
using ServerCommunication;

namespace Property_and_Management.src.Service.Listeners
{
    public class NotificationClient : IServerClient, IDisposable
    {
        private bool _disposed;

        private readonly List<IObserver<MessageBase>> _subscribers = new();
        private readonly UdpClient _udpClient;

        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private CancellationToken CancellationToken => _cancellationTokenSource.Token;

        private const int MaxRetries = 5;
        private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);

        public IPEndPoint ServerEndpoint => new IPEndPoint(IPAddress.Loopback, 4544);

        public NotificationClient()
        {
            _udpClient = new UdpClient(0); // OS will autoasign
        }

        private void HandleMessagePacket(MessageWrapper wrappedMessage)
        {
            try
            {
                switch (wrappedMessage.Type)
                {
                    case nameof(SendNotificationMessage):
                        HandleSendNotificationMessage(wrappedMessage);
                        break;
                    default:
                        Console.WriteLine($"Message type cannot be handled: {wrappedMessage.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception when handling message packet: {ex.Message}");
            }
        }

        private void HandleSendNotificationMessage(MessageWrapper wrappedMessage)
        {
            // Deserialize the message
            SendNotificationMessage? message = wrappedMessage.Deserialize<SendNotificationMessage>();

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            // Send the message to the subscribers
            foreach (var subscriber in _subscribers)
            {
                subscriber.OnNext(message);
            }
        }

        public void StopListening() => _cancellationTokenSource.Cancel();

        public async Task ListenAsync()
        {
            int retryCount = 0;
            var retryDelay = InitialRetryDelay;

            while (!CancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync(CancellationToken);
                    retryCount = 0;
                    retryDelay = InitialRetryDelay;

                    MessageWrapper? wrappedMessage = CommunicationHelper.GetMessageWrapper(result.Buffer);

                    if (wrappedMessage == null)
                    {
                        Console.WriteLine($"Recived bad json: {Encoding.UTF8.GetString(result.Buffer)}");
                        continue;
                    }

                    HandleMessagePacket(wrappedMessage);
                }
                catch (SocketException ex)
                {
                    retryCount++;
                    if (retryCount > MaxRetries)
                    {
                        Console.WriteLine($"UDP client: max retries ({MaxRetries}) reached, stopping. Last error: {ex.Message}");
                        break;
                    }
                    Console.WriteLine($"UDP client: SocketException ({ex.Message}), retry {retryCount}/{MaxRetries} in {retryDelay.TotalSeconds}s");
                    try { await Task.Delay(retryDelay, CancellationToken); }
                    catch (OperationCanceledException) { break; }
                    retryDelay = TimeSpan.FromTicks(Math.Min(retryDelay.Ticks * 2, MaxRetryDelay.Ticks));
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        public IDisposable Subscribe(IObserver<MessageBase> observer)
        {
            _subscribers.Add(observer);
            return null;
        }

        public void SendNotification(int userId, string title, string body)
        {
            var sendNotificationMessage = new SendNotificationMessage
            {
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                Title = title,
                Body = body
            };

            byte[] data = CommunicationHelper.SerializeMessage(sendNotificationMessage);
            _udpClient.Send(data, data.Length, ServerEndpoint);
        }

        public void SubscribeToServer(int userId)
        {
            var subscribeToServerMessage = new SubscribeToServerMessage
            {
                UserId = userId,
            };

            byte[] data = CommunicationHelper.SerializeMessage(subscribeToServerMessage);
            _udpClient.Send(data, data.Length, ServerEndpoint);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cancellationTokenSource.Cancel();
            _udpClient.Close();
            _cancellationTokenSource.Dispose();
        }
    }
}
