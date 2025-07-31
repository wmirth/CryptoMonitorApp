using System.Text.Json.Serialization;
using System.Collections.Generic; // 引入 Dictionary 所需的命名空间

namespace CryptoMonitorApp
{
    // 这个类用于反序列化每个加密货币的具体数据，如 USD 价格和 24 小时变化
    public class CryptoCurrencyDetails
    {
        [JsonPropertyName("usd")]
        public decimal usd { get; set; }

        [JsonPropertyName("usd_24h_change")] // 注意，这里我们已经修正为 usd_24h_change
        public decimal? usd_24h_change { get; set; }
    }

    // 根对象，使用 Dictionary 来灵活处理多个加密货币的数据
    // 键是加密货币的 ID (如 "bitcoin", "ethereum", "solana" 等)
    // 值是对应的 CryptoCurrencyDetails 对象
    public class CryptoData : Dictionary<string, CryptoCurrencyDetails?>
    {
        // 这个类现在是 Dictionary<string, CryptoCurrencyDetails?> 的子类
        // 它不再需要固定的 bitcoin 和 ethereum 属性，因为它们会作为字典的键值对被解析
    }
}