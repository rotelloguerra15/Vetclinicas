using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace VetClinica.API.Data;

/// <summary>
/// Por padrão, o EF Core cacheia o modelo compilado (mapeamentos, schema padrão, etc.)
/// usando apenas o TIPO do DbContext como chave. Como o TenantDbContext usa
/// HasDefaultSchema(_schema) com um schema DIFERENTE por instância (uma clínica
/// por schema), sem esta chave customizada o EF reaproveitaria o modelo da
/// PRIMEIRA instância criada desde o boot — fazendo instâncias seguintes
/// gerarem SQL contra o schema ERRADO (vazamento entre tenants).
///
/// Esta factory inclui o schema na chave de cache, garantindo um modelo
/// compilado por schema. Registrada via ReplaceService no OnConfiguring.
/// </summary>
public class TenantModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        var schema = (context as TenantDbContext)?.Schema ?? "default";
        return (context.GetType(), schema, designTime);
    }
}
