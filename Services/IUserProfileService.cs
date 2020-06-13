using System.Collections.Generic;
using System.Threading.Tasks;
using seattle.Models;
using seattle.ViewModels.Account;

namespace seattle.Services
{
    public interface IUserProfileService
    {
        Task<List<UserProfileModel>> GetMostRecentlyCreatedUsers();

        Task<UserProfileModel> GetUser(int id, int? viewerId);

        Task<UserProfileModel> GetUser(string handle, int? viewerId);

        UserProfileModel CreateUser(RegisterInputModel initial);

        Task DeleteUser(int id, int by_user_id);

        Task LogIn(int id, string from);

        Task<List<UserProfileModel>> GetFollowers(int userId, int? viewerId);

        Task<List<UserProfileModel>> GetFollowing(int userId, int? viewerId);

        Task<List<UserProfileModel>> GetBlocked(int userId, int? viewerId);
    }
}