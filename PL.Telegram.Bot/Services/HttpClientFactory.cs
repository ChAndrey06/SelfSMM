using System.Net;
using PL.Telegram.Bot.Models;

namespace PL.Telegram.Bot.Services;

public static class HttpClientFactory
{
    public static ProxyConfiguration[] Proxies { private get; set; }

    private static HttpClient GetProxyClient()
    {
        var proxyConf = Proxies[new Random().Next(0, Proxies.Length)];

        var proxyURI = new Uri(proxyConf.Address);
        var credentials = new NetworkCredential(proxyConf.Login, proxyConf.Password);

        WebProxy proxy;

        if (proxyConf.NeedAuthorization)
        {
            proxy = new WebProxy(proxyURI, true, null, credentials);
        }
        else
        {
            proxy = new WebProxy(proxyURI, true, null);
        }

        var httpClientHandler = new HttpClientHandler
        {
            Proxy = proxy,
        };

        if (proxyConf.NeedAuthorization)
        {
            httpClientHandler.PreAuthenticate = true;
            httpClientHandler.UseDefaultCredentials = false;
            httpClientHandler.Credentials = credentials;
        }
        else
        {
            httpClientHandler.PreAuthenticate = false;
            httpClientHandler.UseDefaultCredentials = false;
        }

        return new HttpClient(handler: httpClientHandler, disposeHandler: true);
    }

    private static HttpClient GetBasicClient() => new ();

    public static HttpClient GetHttpClient(bool needProxy)
    {
        var client = needProxy ? GetProxyClient() : GetBasicClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        return client;
    }
}