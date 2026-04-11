using System;
using System.Threading.Tasks;
using Property_and_Management.src.DTO;

namespace Property_and_Management.src.Interface
{
    public interface IServerClient : IObservable<IncomingNotification>
    {
        Task ListenAsync();
        void SubscribeToServer(int userId);
        void SendNotification(int userId, string title, string body);
        void StopListening();
    }
}
