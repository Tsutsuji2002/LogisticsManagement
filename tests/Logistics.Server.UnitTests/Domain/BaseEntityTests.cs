using Logistics.Server.Domain.Common;
using Xunit;

namespace Logistics.Server.UnitTests.Domain;

public class BaseEntityTests
{
    private class SampleEntity : BaseEntity
    {
        public void DoSomething()
        {
            RaiseDomainEvent(new SampleEvent(PublicId));
        }
    }

    private record SampleEvent(Guid EntityId) : DomainEvent;

    [Fact]
    public void NewEntity_InitializesPublicIdAndCreatedAt()
    {
        var entity = new SampleEntity();

        Assert.NotEqual(Guid.Empty, entity.PublicId);
        Assert.True(entity.CreatedAt <= DateTime.UtcNow);
        Assert.Null(entity.UpdatedAt);
        Assert.Empty(entity.DomainEvents);
    }

    [Fact]
    public void RaiseDomainEvent_AppendsEventToCollection()
    {
        var entity = new SampleEntity();
        entity.DoSomething();

        Assert.Single(entity.DomainEvents);
        var evt = entity.DomainEvents.First() as SampleEvent;
        Assert.NotNull(evt);
        Assert.Equal(entity.PublicId, evt.EntityId);
    }

    [Fact]
    public void ClearDomainEvents_EmptiesEventCollection()
    {
        var entity = new SampleEntity();
        entity.DoSomething();
        Assert.NotEmpty(entity.DomainEvents);

        entity.ClearDomainEvents();
        Assert.Empty(entity.DomainEvents);
    }
}
