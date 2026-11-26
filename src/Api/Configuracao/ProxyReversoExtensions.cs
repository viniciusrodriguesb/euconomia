using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Api.Configuracao;

internal static class ProxyReversoExtensions
{
    public static IServiceCollection AdicionarProxyReverso(this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.Configure<ForwardedHeadersOptions>(opcoes =>
        {
            opcoes.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            opcoes.ForwardLimit = 1;
            var proxy = configuracao["ProxyReverso:IpConfiavel"];
            if (!string.IsNullOrWhiteSpace(proxy))
                opcoes.KnownProxies.Add(IPAddress.Parse(proxy));
        });
        return servicos;
    }
}
