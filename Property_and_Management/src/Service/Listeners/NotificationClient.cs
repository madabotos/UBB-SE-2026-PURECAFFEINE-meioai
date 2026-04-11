using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Property_and_Management.Src.DataTransferObjects;
using Property_and_Management.Src.Interface;
using ServerCommunication;

namespace Property_and_Management.Src.Service.Listeners
{
    public class NotificationClient : IServerClient, IDisposable
    {
        private const int NotificationServerPort = 4544;
        private const int AutoAssignLocalUdpPort = 0;
        private const int InitialRetryCount = 0;
        private const int RetryBackoffMultiplier = 2;
        private bool disposed;

        private readonly List<IObserver<IncomingNotification>> subscribers = new();
        private readonly UdpClient udpClient;

        private readonly CancellationTokenSource cancellationTokenSource = new();
        private CancellationToken CancellationToken => cancellationTokenSource.Token;

        private const int MaxRetries = 5;
        private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);

        public IPEndPoint ServerEndpoint => new IPEndPoint(IPAddress.Loopback, NotificationServerPort);

        public NotificationClient()
        {
            udpClient = new UdpClient(AutoAssignLocalUdpPort); // OS will auto-assign
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
            catch (Exception exception)
            {
                Console.WriteLine($"Exception when handling message packet: {exception.Message}");
            }
        }

        private void HandleSendNotificationMessage(MessageWrapper wrappedMessage)
        {
            SendNotificationMessage? message = wrappedMessage.Deserialize<SendNotificationMessage>();

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            // Translate infrastructure message to domain Data Transfer Object before notifying subscribers,
            // so callers never need to know about ServerCommunication types.
            var incoming = new IncomingNotification
            {
                UserIdentifier = message.UserIdentifier,
                Timestamp = message.Timestamp,
                Title = message.Title,
                Body = message.Body
            };

            foreach (var subscriber in subscribers)
            {
                subscriber.OnNext(incoming);
            }
        }

        public void StopListening() => cancellationTokenSource.Cancel();

        public async Task ListenAsync()
        {
            int retryCount = InitialRetryCount;
            var retryDelay = InitialRetryDelay;

            while (!CancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await udpClient.ReceiveAsync(CancellationToken);
                    retryCount = InitialRetryCount;
                    retryDelay = InitialRetryDelay;

                    MessageWrapper? wrappedMessage = CommunicationHelper.GetMessageWrapper(result.Buffer);

                    if (wrappedMessage == null)
                    {
                        Console.WriteLine($"Received bad json: {Encoding.UTF8.GetString(result.Buffer)}");
                        continue;
                    }

                    HandleMessagePacket(wrappedMessage);
                }
                catch (SocketException socketException)
                {
                    retryCount++;
                    if (retryCount > MaxRetries)
                    {
                        Console.WriteLine($"UDP client: max retries ({MaxRetries}) reached, stopping. Last error: {socketException.Message}");
                        break;
                    }
                    Console.WriteLine($"UDP client: SocketException ({socketException.Message}), retry {retryCount}/{MaxRetries} in {retryDelay.TotalSeconds}s");
                    try
                    {
                        await Task.Delay(retryDelay, CancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    retryDelay = TimeSpan.FromTicks(Math.Min(retryDelay.Ticks * RetryBackoffMultiplier, MaxRetryDelay.Ticks));
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

        public IDisposable Subscribe(IObserver<IncomingNotification> observer)
        {
            subscribers.Add(observer);
            return new Unsubscriber(subscribers, observer);
        }

        public void SendNotification(int userIdentifier, string title, string body)
        {
            var message = new SendNotificationMessage
            {
                UserIdentifier = userIdentifier,
                Timestamp = DateTime.UtcNow,
                Title = title,
                Body = body
            };

            byte[] data = CommunicationHelper.SerializeMessage(message);
            udpClient.Send(data, data.Length, ServerEndpoint);
        }

        public void SubscribeToServer(int userIdentifier)
        {
            var message = new SubscribeToServerMessage { UserIdentifier = userIdentifier };
            byte[] data = CommunicationHelper.SerializeMessage(message);
            udpClient.Send(data, data.Length, ServerEndpoint);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            cancellationTokenSource.Cancel();
            udpClient.Close();
            cancellationTokenSource.Dispose();
        }

        private sealed class Unsubscriber : IDisposable
        {
            private readonly List<IObserver<IncomingNotification>> subscribers;
            private readonly IObserver<IncomingNotification> observer;

            public Unsubscriber(List<IObserver<IncomingNotification>> subscribers, IObserver<IncomingNotification> observer)
            {
                this.subscribers = subscribers;
                this.observer = observer;
            }

            public void Dispose() => subscribers.Remove(observer);
        }
    }
}

