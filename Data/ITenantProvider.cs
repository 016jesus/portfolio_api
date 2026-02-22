namespace portfolio_api.Data
{
    public interface ITenantProvider
    {
        Guid? TenantId { get; }
    }
}
