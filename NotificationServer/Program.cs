namespace NotificationServer
{
    internal class Program
    {
        private static async Task Main()
        {
            // Controls the lifetime of the UDP listener loop.
            using var udpListenerLifetimeCancellationSource = new CancellationTokenSource();

            // Handle Ctrl+C
            Console.CancelKeyPress += (_, cancelKeyPressEventArgs) =>
            {
                cancelKeyPressEventArgs.Cancel = true;
                Console.WriteLine("Stopping server...");
                udpListenerLifetimeCancellationSource.Cancel();
            };

            await UdpNotificationServer.ListenAsync(udpListenerLifetimeCancellationSource.Token);
        }
    }
}
