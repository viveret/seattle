using System.Threading.Tasks;
using PinkUmbrella.Models;
using PinkUmbrella.Models.Public;
using Poncho.Models;
using Poncho.Models.Public;

namespace PinkUmbrella.Services
{
    public interface IFeedService
    {
        Task<FeedModel> GetFeedForUser(PublicId userId, int? viewerId, bool includeReplies, PaginationModel pagination);
    }
}