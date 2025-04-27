using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Application.Interfaces.Poker
{
    public interface IDomainEventPublisherService
    {
        Task PublishAsync<T>(T domainEvent) where T : notnull;
    }
}
