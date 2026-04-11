using System;
using System.Threading.Tasks;
using Property_and_Management.Src.DataTransferObjects;

namespace Property_and_Management.Src.Interface
{
    public interface IServerClient : IObservable<IncomingNotification>
    {
        Task ListenAsync();
        void SubscribeToServer(int userIdentifier);
        void SendNotification(int userIdentifier, string title, string body);
        void StopListening();
    }
}

