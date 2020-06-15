using System.Collections.Generic;
using System.Threading.Tasks;
using PinkUmbrella.Models;

namespace PinkUmbrella.Services
{
    public interface IPostService
    {
        Task<PostModel> GetPost(int id, int? viewerId);
        Task<NewPostResult> TryCreatePosts(List<PostModel> post_chain);
        Task<NewPostResult> TryCreateTextPosts(int userId, string[] post_chain, Visibility visibility);
        Task DeletePost(int id, int userId);
        Task UpdateShadowBanStatus(int id, bool status);


        Task<FeedModel> GetFeedForUser(int userId, int? viewerId, bool includeReplies, PaginationModel pagination);
        Task<FeedModel> GetMentionsForUser(int userId, int? viewerId, bool includeReplies, PaginationModel pagination);
        Task<FeedModel> GetPostsForUser(int userId, int? viewerId, bool includeReplies, PaginationModel pagination);


        Task BindReferences(PostModel post, int? viewerId);

        bool CanView(PostModel post, int? viewerId);

        Task<PaginatedModel<PostModel>> GetMostReportedPosts();
        
        Task<PaginatedModel<PostModel>> GetMostBlockedPosts();

        Task<PaginatedModel<PostModel>> GetMostDislikedPosts();
    }
}