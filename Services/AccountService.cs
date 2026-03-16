using Course.Data;
using Course.Interfaces;

namespace Course.Services;

public class AccountService(ApiDbContext dbContext) : IAccount
{
    public Task<object> GetAdminDashboardSummary()
    {
        
        throw new NotImplementedException();
    }
}