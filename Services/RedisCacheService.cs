using StackExchange.Redis;
using Newtonsoft.Json;
using myappmvc.Interfaces;

namespace myappmvc.Services
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDatabase _db;// Kết nối Redis đến database


        // Constructor
        public RedisCacheService(IConnectionMultiplexer connectionMultiplexer)
        {
            _db = connectionMultiplexer.GetDatabase();
        }


        // Lấy giá trị từ Redis cache
        public async Task<T?> GetAsync<T>(string key)
        {
            var value = await _db.StringGetAsync(key);
            return value.HasValue ? JsonConvert.DeserializeObject<T>(value!) : default;// nếu không có giá trị thì trả về null
        }


        // Lưu giá trị vào Redis cache
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var json = JsonConvert.SerializeObject(value);// chuyển đổi đối tượng thành chuỗi JSON
            await _db.StringSetAsync(key, json, expiry);
        }


        // Xóa giá trị khỏi Redis cache
        public async Task RemoveAsync(string key)
        {
            await _db.KeyDeleteAsync(key);
        }
    }
}
