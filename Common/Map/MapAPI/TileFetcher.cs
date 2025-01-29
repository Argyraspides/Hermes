using System.Net.Http;
using System.Threading.Tasks;

public static class TileFetcher
{
    private static readonly HttpClient m_client = new HttpClient();

    public static async Task<byte[]> FetchTileDataAsync(string url)
    {
        byte[] data = await m_client.GetByteArrayAsync(url);
        return data;
    }
}
