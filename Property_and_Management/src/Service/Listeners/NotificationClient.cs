using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class NotificationClient : IServerClient
    {
        private readonly object _subscriberLock = new();
        private readonly List<IObserver<MessageBase>> _subscribers = new();
        private readonly UdpClient _udpClient;

        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public IPEndPoint ServerEndpoint => new IPEndPoint(IPAddress.Loopback, 4544);

        public NotificationClient()
        {
            _udpClient = new UdpClient(0); // OS will auto-assign a port
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
                        Debug.WriteLine($"Message type cannot be handled: {wrappedMessage.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception when handling message packet: {ex.Message}");
            }
        }

        private void HandleSendNotificationMessage(MessageWrapper wrappedMessage)
        {
            SendNotificationMessage? message = wrappedMessage.Deserialize<SendNotificationMessage>();

            if (message == null)
            {
                Debug.WriteLine("Failed to deserialize SendNotificationMessage");
                return;
            }

            IObserver<MessageBase>[] snapshot;
            lock (_subscriberLock) { snapshot = _subscribers.ToArray(); }
            foreach (var subscriber in snapshot)
            {
                subscriber.OnNext(message);
            }
        }

        public void StopListening() => _cancellationTokenSource.Cancel();

        public async Task ListenAsync()
        {
            try
            {
                while (!CancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = await _udpClient.ReceiveAsync(CancellationToken);
                        MessageWrapper? wrappedMessage = CommunicationHelper.GetMessageWrapper(result.Buffer);

                        if (wrappedMessage == null)
                        {
                            Debug.WriteLine($"Received bad json: {Encoding.UTF8.GetString(result.Buffer)}");
                            continue;
                        }

                        HandleMessagePacket(wrappedMessage);
                    }
                    catch (SocketException ex)
                    {
                        // Windows sends ICMP Port Unreachable when the server isn't listening,
                        // which surfaces as SocketException on the next ReceiveAsync. Safe to retry.
                        Debug.WriteLine($"UDP socket error (retrying): {ex.SocketErrorCode}");
                        await Task.Delay(500, CancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("UDP client listening cancelled");
            }
            catch (ObjectDisposedException)
            {
                Debug.WriteLine("UDP client socket closed");
            }
            // NOTE: Do NOT close _udpClient here -- it's still needed for SendNotification/SubscribeToServer
        }

        public IDisposable Subscribe(IObserver<MessageBase> observer)
        {
            lock (_subscriberLock) { _subscribers.Add(observer); }
            return new Unsubscriber(_subscriberLock, _subscribers, observer);
        }

        private sealed class Unsubscriber : IDisposable
        {
            private readonly object _lock;
            private readonly List<IObserver<MessageBase>> _observers;
            private readonly IObserver<MessageBase> _observer;

            public Unsubscriber(object @lock, List<IObserver<MessageBase>> observers, IObserver<MessageBase> observer)
            {
                _lock = @lock;
                _observers = observers;
                _observer = observer;
            }

            public void Dispose() { lock (_lock) { _observers.Remove(_observer); } }
        }

        public void SendNotification(int userId, string title, string body)
        {
            try
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to send notification via UDP: {ex.Message}");
            }
        }

        public void SubscribeToServer(int userId)
        {
            try
            {
                var subscribeToServerMessage = new SubscribeToServerMessage
                {
                    UserId = userId,
                };

                byte[] data = CommunicationHelper.SerializeMessage(subscribeToServerMessage);
                _udpClient.Send(data, data.Length, ServerEndpoint);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to subscribe to server via UDP: {ex.Message}");
            }
        }
    }
}
