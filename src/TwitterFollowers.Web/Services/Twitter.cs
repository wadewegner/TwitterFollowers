using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TwitterFollowers.Web.Models;
using TwitterOAuth.RestAPI.Models;
using TwitterOAuth.RestAPI.Resources;

namespace TwitterFollowers.Web.Services
{
    public class Twitter
    {
        private readonly TwitterOAuth.RestAPI.Authorization _authorization;
        private readonly HttpHelper _httpHelper;

        private readonly string _apiKey = ConfigurationManager.AppSettings["ApiKey"];
        private readonly string _apiSecret = ConfigurationManager.AppSettings["ApiSecret"];
        private readonly string _accessToken = ConfigurationManager.AppSettings["AccessToken"];
        private readonly string _accessTokenSecret = ConfigurationManager.AppSettings["AccessTokenSecret"];

        public string UserName = "wadewegner";

        public Twitter()
        {
            var secretModel = new SecretModel
            {
                ApiKey = _apiKey,
                ApiSecret = _apiSecret,
                AccessToken = _accessToken,
                AccessTokenSecret = _accessTokenSecret
            };

            _authorization = new TwitterOAuth.RestAPI.Authorization(secretModel);
            _httpHelper = new HttpHelper();
        }

        public async Task<UserLookupModel> UserAsync(string userId)
        {
            var uri = new Uri(string.Format("{0}?{1}", Urls.UsersLookup, string.Format("user_id={0}", userId)));
            var authHeader = _authorization.GetHeader(uri);
            var userLookup = await _httpHelper.HttpSend<List<UserLookupModel>>(authHeader, uri);

            return userLookup.FirstOrDefault();
        }

        public async Task<List<UserLookupModel>> UsersAsync(List<string> userIds)
        {
            var joinedUserIds = string.Join(",", userIds);
            var uri = new Uri(string.Format("{0}?{1}", Urls.UsersLookup, string.Format("user_id={0}", joinedUserIds)));
            var authHeader = _authorization.GetHeader(uri);
            var userLookup = await _httpHelper.HttpSend<List<UserLookupModel>>(authHeader, uri);

            return userLookup;
        }

        public async Task<FriendsIdsModel> FriendsIdsAsync(string parameter, string userNameOrId)
        {
            var uri = new Uri(string.Format("{0}?{1}", Urls.FriendsIds, string.Format("{0}={1}", parameter, userNameOrId)));
            var authHeader = _authorization.GetHeader(uri);
            var friendsId = await _httpHelper.HttpSend<FriendsIdsModel>(authHeader, uri);

            return friendsId;
        }

        public void MapTwitterUserToUser(User user, UserLookupModel userLookup)
        {
            user.Id = userLookup.id;
            user.Name = userLookup.name;
            user.ScreenName = userLookup.screen_name;
            user.ProfileImageUrl = userLookup.profile_image_url;
            if (userLookup.status != null)
            {
                if (!string.IsNullOrEmpty(userLookup.status.created_at))
                {
                    user.CreatedAt = DateTime.ParseExact(userLookup.status.created_at, "ddd MMM dd HH:mm:ss %K yyyy",
                        CultureInfo.InvariantCulture.DateTimeFormat);
                }
            }
            else
            {
                Console.WriteLine(userLookup.screen_name);
            }
            user.Location = userLookup.location;
            user.FollowRequestSent = userLookup.follow_request_sent;
            user.IdStr = userLookup.id_str;
            user.Url = userLookup.url;
            user.UtcOffset = userLookup.utc_offset;
            user.ProfileUseBackgroundImage = userLookup.profile_use_background_image;
            user.ListedCount = userLookup.listed_count;
            user.Lang = userLookup.lang;
            user.FollowersCount = userLookup.followers_count;
            user.Protected = userLookup.@protected;
            user.Notifications = userLookup.notifications;
            user.Verified = userLookup.verified;
            user.GeoEnabled = userLookup.geo_enabled;
            user.TimeZone = userLookup.time_zone;
            user.Description = userLookup.description;
            user.DefaultProfileImage = userLookup.default_profile_image;
            user.ProfileBackgroundImageUrl = userLookup.profile_background_image_url;
            user.StatusesCount = userLookup.statuses_count;
            user.FriendsCount = userLookup.friends_count;
            user.Following = userLookup.following;
        }
    }
}