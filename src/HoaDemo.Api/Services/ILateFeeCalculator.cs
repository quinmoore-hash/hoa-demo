using HoaDemo.Api.Models;

namespace HoaDemo.Api.Services
{
    public interface ILateFeeCalculator
    {
        decimal OutstandingBalance(Assessment assessment, DateTime asOf);
        decimal CalculateLateFee(Assessment assessment, Association policy, DateTime asOf);
    }
}
