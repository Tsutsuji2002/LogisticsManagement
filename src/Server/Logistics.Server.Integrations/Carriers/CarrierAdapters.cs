namespace Logistics.Server.Integrations.Carriers;

public class GhnCarrierAdapter : CarrierAdapterBase
{
    public override string CarrierCode => "GHN";
    public override string CarrierName => "Giao Hang Nhanh";
}

public class GhtkCarrierAdapter : CarrierAdapterBase
{
    public override string CarrierCode => "GHTK";
    public override string CarrierName => "Giao Hang Tiet Kiem";
}

public class ViettelPostCarrierAdapter : CarrierAdapterBase
{
    public override string CarrierCode => "VTPOST";
    public override string CarrierName => "Viettel Post";
}

public interface ICarrierAdapterFactory
{
    ICarrierAdapter GetAdapter(string carrierCode);
    IReadOnlyList<ICarrierAdapter> GetAllAdapters();
}

public class CarrierAdapterFactory : ICarrierAdapterFactory
{
    private readonly IEnumerable<ICarrierAdapter> _adapters;

    public CarrierAdapterFactory(IEnumerable<ICarrierAdapter> adapters)
    {
        _adapters = adapters;
    }

    public ICarrierAdapter GetAdapter(string carrierCode)
    {
        var adapter = _adapters.FirstOrDefault(a => string.Equals(a.CarrierCode, carrierCode, StringComparison.OrdinalIgnoreCase));
        if (adapter == null)
        {
            throw new NotSupportedException($"No carrier adapter is registered for carrier code '{carrierCode}'.");
        }
        return adapter;
    }

    public IReadOnlyList<ICarrierAdapter> GetAllAdapters()
    {
        return _adapters.ToList();
    }
}
