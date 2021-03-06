using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using PinkUmbrella.Models;
using PinkUmbrella.Repositories;
using Tides.Models.Auth;
using Tides.Models.Public;

namespace PinkUmbrella.Services.Redis
{
    public class RedisRateLimitService : IRateLimitService
    {
        private static readonly ActorRateLimitModel Zero = new ActorRateLimitModel();
        private static readonly ActorRateLimitModel DefaultSingleUserLimit = new ActorRateLimitModel()
        {
            UploadImages = 150,
            UploadVideos = 10,
            ApiCall = 18000,
        }.SetActions(250).SetReactions(1000);

        private readonly RedisRepository _redis;
        private readonly UserManager<UserProfileModel> _users;

        public RedisRateLimitService(RedisRepository redis, UserManager<UserProfileModel> users)
        {
            _redis = redis;
            _users = users;
        }

        public Task<ActorRateLimitModel> GetAllLimitsForGroup(string group)
        {
            ActorRateLimitModel ret = null;
            switch (group)
            {
                case "dev": ret = new ActorRateLimitModel().SetAll(int.MaxValue);
                break;
                case "admin": ret = new ActorRateLimitModel
                {

                };
                break;
            }
            return Task.FromResult(ret);
        }

        public async Task<ActorRateLimitModel> GetAllLimitsForIP(IPAddressModel ip)
        {
            var res = await _redis.Get<ActorRateLimitModel>(ip, "limit");
            return res ?? DefaultSingleUserLimit;
        }

        public async Task<ActorRateLimitModel> GetAllLimitsForUser(PublicId userId)
        {
            var res = await _redis.Get<ActorRateLimitModel>(userId, "limit");
            return res ?? DefaultSingleUserLimit;
        }

        public async Task<ActorRateLimitModel> GetAllRatesForIP(IPAddressModel ip)
        {
            var res = await _redis.Get<ActorRateLimitModel>(ip, "rate");
            return res ?? Zero;
        }

        public async Task<ActorRateLimitModel> GetAllRatesForUser(PublicId userId)
        {
            var res = await _redis.Get<ActorRateLimitModel>(userId, "rate");
            return res ?? Zero;
        }

        public async Task<int> GetLimitForGroup(string group, string property)
        {
            var field = await _redis.FieldGet<ActorRateLimitModel>(group, property, "limit");
            if (field == null)
            {
                var all = await GetAllLimitsForGroup(group);
                field = all.GetType().GetProperty(property).GetValue(all)?.ToString() ?? "0";
            }
            return int.Parse(field);
        }

        public async Task<int> GetLimitForIP(IPAddressModel ip, string property)
        {
            return int.Parse((await _redis.FieldGet<ActorRateLimitModel>(ip, property, "limit")) ?? "0");
        }

        public async Task<int> GetLimitForUser(PublicId userId, string property)
        {
            var limit = (int)DefaultSingleUserLimit.GetType().GetProperty(property).GetValue(DefaultSingleUserLimit);
            if (userId.IsLocal)
            {
                var user = await _users.FindByIdAsync(userId.Id.Value.ToString());
                foreach (var group in await _users.GetRolesAsync(user))
                {
                    limit = System.Math.Max(await GetLimitForGroup(group, property), limit);
                }
            }
            var redisLimit = await _redis.FieldGet<ActorRateLimitModel>(userId, property, "limit");
            if (string.IsNullOrEmpty(redisLimit))
            {
                return limit;
            }
            else
            {
                return int.Parse(redisLimit);
            }
        }

        public async Task<int> GetRateForIP(IPAddressModel ip, string property)
        {
            return int.Parse((await _redis.FieldGet<ActorRateLimitModel>(ip, property, "rate")) ?? "0");
        }

        public async Task<int> GetRateForUser(PublicId userId, string property)
        {
            return int.Parse((await _redis.FieldGet<ActorRateLimitModel>(userId, property, "rate")) ?? "0");
        }

        public Task IncrementRateForIp(IPAddressModel ip, string property) => _redis.Increment<ActorRateLimitModel>(property, ip, "rate");

        public Task IncrementRateForUser(PublicId userId, string property) => _redis.Increment<ActorRateLimitModel>(property, userId, "rate");

        public async Task SetLimitForGroup(string group, string property, int? limit)
        {
            if (limit.HasValue)
            {
                await _redis.FieldSet<ActorRateLimitModel>(group, property, limit.Value.ToString(), "limit");
            }
            else
            {
                await _redis.FieldDelete<ActorRateLimitModel>(property, group, "limit");
            }
        }

        public async Task SetLimitForIP(IPAddressModel ip, string property, int? limit)
        {
            if (limit.HasValue)
            {
                await _redis.FieldSet<ActorRateLimitModel>(ip, property, limit.Value.ToString(), "limit");
            }
            else
            {
                await _redis.FieldDelete<ActorRateLimitModel>(property, ip, "limit");
            }
        }

        public async Task SetLimitForUser(PublicId userId, string property, int? limit)
        {
            if (limit.HasValue)
            {
                await _redis.FieldSet<ActorRateLimitModel>(userId, property, limit.Value.ToString(), "limit");
            }
            else
            {
                await _redis.FieldDelete<ActorRateLimitModel>(property, userId, "limit");
            }
        }

        public async Task<bool> TryActorAction(PublicId userId, string action)
        {
            var rate = await GetRateForUser(userId, action);
            var limit = await GetLimitForUser(userId, action);
            if (rate < limit)
            {
                await IncrementRateForUser(userId, action);
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> TryIP(IPAddressModel ip, string property)
        {
            var rate = await GetRateForIP(ip, property);
            var limit = await GetRateForIP(ip, property);
            if (rate < limit)
            {
                await IncrementRateForIp(ip, property);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}