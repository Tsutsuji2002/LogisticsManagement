using Logistics.Server.Domain.Common;
using Xunit;

namespace Logistics.Server.UnitTests.Domain;

public class ValueObjectTests
{
    private class Address : ValueObject
    {
        public string Street { get; }
        public string City { get; }

        public Address(string street, string city)
        {
            Street = street;
            City = city;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
        }
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var addr1 = new Address("123 Street", "Hanoi");
        var addr2 = new Address("123 Street", "Hanoi");

        Assert.Equal(addr1, addr2);
        Assert.True(addr1 == addr2);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var addr1 = new Address("123 Street", "Hanoi");
        var addr2 = new Address("456 Avenue", "Danang");

        Assert.NotEqual(addr1, addr2);
        Assert.True(addr1 != addr2);
    }

    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHashCode()
    {
        var addr1 = new Address("123 Street", "Hanoi");
        var addr2 = new Address("123 Street", "Hanoi");

        Assert.Equal(addr1.GetHashCode(), addr2.GetHashCode());
    }
}
