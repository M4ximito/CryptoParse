using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;

public class BitcoinPriceService
{
    public async Task<decimal> GetBitcoinPrice(string currency)
    {
        string url = $"https://blockchain.info/ticker?base=BTC&currency={currency}";
        using (HttpClient client = new HttpClient())
        {
            string response = await client.GetStringAsync(url);
            JObject jsonResponse = JObject.Parse(response);
            decimal lastValue = (decimal)jsonResponse[currency]["last"];
            return lastValue;
        }
    }
}
