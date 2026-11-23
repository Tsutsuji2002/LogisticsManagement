using FluentAssertions;
using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Logistics.Server.IntegrationTests;

public class DatabaseModelTests
{
    private readonly ApplicationDbContext _context;

    public DatabaseModelTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public void DbContext_ShouldContainExactly90DomainEntityTypes()
    {
        var entityTypes = _context.Model.GetEntityTypes().ToList();

        // 90 domain entities
        entityTypes.Should().HaveCount(90, "the database architecture specifies exactly 90 domain entities across 14 schemas");
    }

    [Fact]
    public void DbContext_ShouldMapToAll14DomainSchemas()
    {
        var expectedSchemas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "organization",
            "identity",
            "configuration",
            "crm",
            "catalog",
            "sales",
            "shipping",
            "carrier",
            "warehouse",
            "inventory",
            "finance",
            "notification",
            "integration",
            "audit"
        };

        var actualSchemas = _context.Model.GetEntityTypes()
            .Select(t => t.GetSchema())
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        actualSchemas.Should().BeEquivalentTo(expectedSchemas, "all 14 domain-aligned schemas must be mapped");
    }

    [Fact]
    public void AllEntities_ShouldHavePrimaryKeyDefined()
    {
        var entityTypes = _context.Model.GetEntityTypes();

        foreach (var entityType in entityTypes)
        {
            var pk = entityType.FindPrimaryKey();
            pk.Should().NotBeNull($"entity type '{entityType.ClrType.Name}' must have a primary key configured");
            pk!.Properties.Should().NotBeEmpty($"entity type '{entityType.ClrType.Name}' primary key must have at least one property");
        }
    }

    [Fact]
    public void BaseEntityTypes_ShouldHavePublicIdAndAuditingConfigured()
    {
        var baseEntityTypes = _context.Model.GetEntityTypes()
            .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType));

        foreach (var entityType in baseEntityTypes)
        {
            var publicIdProp = entityType.FindProperty(nameof(BaseEntity.PublicId));
            publicIdProp.Should().NotBeNull($"BaseEntity '{entityType.ClrType.Name}' must have PublicId configured");
            publicIdProp!.IsNullable.Should().BeFalse($"BaseEntity '{entityType.ClrType.Name}' PublicId must not be nullable");

            var createdAtProp = entityType.FindProperty(nameof(BaseEntity.CreatedAt));
            createdAtProp.Should().NotBeNull($"BaseEntity '{entityType.ClrType.Name}' must have CreatedAt configured");
            createdAtProp!.IsNullable.Should().BeFalse();
        }
    }

    [Fact]
    public void DecimalProperties_ShouldHavePrecisionDefined()
    {
        var decimalPropertiesWithoutPrecision = new List<string>();

        foreach (var entityType in _context.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                {
                    var precision = property.GetPrecision();
                    if (precision == null)
                    {
                        decimalPropertiesWithoutPrecision.Add($"{entityType.ClrType.Name}.{property.Name}");
                    }
                }
            }
        }

        decimalPropertiesWithoutPrecision.Should().BeEmpty("every decimal column must have explicit precision to prevent truncation");
    }

    [Fact]
    public async Task CanSeedAndQueryMasterData_InMemory()
    {
        var company = new Company
        {
            Code = "LOG-GLOBAL",
            LegalName = "Logistics Global Corporation",
            TradingName = "Logistics Express",
            TaxNumber = "0109999999",
            Email = "ops@logistics.com",
            Phone = "+84-24-1234-5678",
            IsActive = true
        };

        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        var branch = new Branch
        {
            CompanyId = company.Id,
            Code = "BR-HN-CENTRAL",
            Name = "Hanoi Central Transit Hub",
            BranchType = "TransitHub",
            Phone = "+84-24-8765-4321",
            IsActive = true
        };

        _context.Branches.Add(branch);
        await _context.SaveChangesAsync();

        var loadedCompany = await _context.Companies
            .Include(c => c.Branches)
            .FirstOrDefaultAsync(c => c.Code == "LOG-GLOBAL");

        loadedCompany.Should().NotBeNull();
        loadedCompany!.Branches.Should().HaveCount(1);
        loadedCompany.Branches.First().Code.Should().Be("BR-HN-CENTRAL");
        loadedCompany.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
