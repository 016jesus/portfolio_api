using Microsoft.AspNetCore.Http;

namespace portfolio_api.Data
{
    public class HttpTenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpTenantProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? TenantId
        {
            get
            {
                var headerValue = _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                if (Guid.TryParse(headerValue, out var tenantId))
                    return tenantId;

                return null;
            }
        }
    }
}
