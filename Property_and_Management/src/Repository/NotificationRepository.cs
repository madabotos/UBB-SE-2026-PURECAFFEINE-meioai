using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Property_and_Management.src.Model;
using Windows.ApplicationModel.Activation;

namespace Property_and_Management.src.Repository
{
    class NotificationRepository : IRepository<Notification>
    {
        public void Add(Notification newEntity)
        {
            throw new NotImplementedException();
        }

        public Notification Delete(int removedEntityId)
        {
            throw new NotImplementedException();
        }

        public Notification Get(int id)
        {
            throw new NotImplementedException();
        }

        public ImmutableList<Notification> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Update(int updatedEntityId, Notification newEntity)
        {
            throw new NotImplementedException();
        }
    }
}
