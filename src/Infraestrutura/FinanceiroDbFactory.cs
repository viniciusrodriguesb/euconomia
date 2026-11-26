using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infraestrutura;

public sealed class FinanceiroDbFactory : IDesignTimeDbContextFactory<FinanceiroDb>
{
    public FinanceiroDb CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Financeiro")
            ?? "Host=127.0.0.1;Port=5433;Database=euconomia";
        return new FinanceiroDb(new DbContextOptionsBuilder<FinanceiroDb>().UseNpgsql(connection).Options);
    }
}
