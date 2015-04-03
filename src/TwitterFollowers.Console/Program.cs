using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TwitterOAuth.RestAPI;
using TwitterOAuth.RestAPI.Exceptions;
using TwitterOAuth.RestAPI.Models;
using TwitterOAuth.RestAPI.Resources;

namespace TwitterFollowers.Console
{
    class Program
    {
        private static SecretModel _secretModel;
        private static Authorization _authorization;
        private static HttpHelper _httpHelper;

        private readonly static string ApiKey = ConfigurationManager.AppSettings["ApiKey"];
        private readonly static string ApiSecret = ConfigurationManager.AppSettings["ApiSecret"];
        private readonly static string AccessToken = ConfigurationManager.AppSettings["AccessToken"];
        private readonly static string AccessTokenSecret = ConfigurationManager.AppSettings["AccessTokenSecret"];

        const string UserName = "wadewegner";
        const string FriendIdsFile = @"c:\temp\twitter\friendIds.txt";
        const string NewFriendIdsFile = @"c:\temp\twitter\newFriendIds.txt";
        const string FriendsFriendsIdsFolder = @"c:\temp\twitter\friendids\";
        const string FollowedFolder = @"c:\temp\twitter\followed\";
        const string ToFollowFolder = @"c:\temp\twitter\tofollow\";
        const string EvaluatedFolder = @"c:\temp\twitter\evaluated\";
        const string FriendsFriendsIdsFile = FriendsFriendsIdsFolder + @"{0}friendIds.txt";
        private static string _friendId;
        private static string _file;

        static void Main(string[] args)
        {
            var mode = args.Length == 0 ? "1" : args[0];

            // setup the API
            SetupTwitterApi();

            switch (mode)
            {
                // find friends
                case "1":
                    GetAndStoreFriendsFriendIds();
                    break;

                // aggregate friends of friends
                case "2":
                    AggregateNewFriendsIds();
                    break;

                // Find folks to follow
                case "3":
                    FilterNewFriendsIdsByFollowerCount(500, 5000, -2);
                    break;

                // follow
                case "4":
                    FollowNewFriends();
                    break;
            }

            System.Console.ReadLine();
        }

        private static void FollowNewFriends()
        {
            var filePaths = Directory.GetFiles(ToFollowFolder, "*.txt");
            var toFollowAggregate = new List<string>();

            foreach (var file in filePaths)
            {
                var toFollowFile = File.ReadAllLines(file);
                var toFollow = new List<string>(toFollowFile);

                toFollowAggregate.AddRange(toFollow);

                var fileName = file.Replace(ToFollowFolder, "");
                File.Move(file, FollowedFolder + fileName);
            }

            foreach (var id in toFollowAggregate)
            {
                var followedUser = FollowUser(id);
                followedUser.Wait();
            }
        }

        private static async Task<FollowedModel> FollowUser(string id)
        {
            var uri = new Uri(string.Format("{0}?{1}&follow=true", Urls.FriendshipsCreate, string.Format("user_id={0}", id)));
            var authHeader = _authorization.GetHeader(uri, HttpMethod.Post);
            var result = await _httpHelper.HttpSend<FollowedModel>(authHeader, uri, HttpMethod.Post);

            System.Console.WriteLine("Followed: @{0}", result.screen_name);
            return result;
        }

        private static void FilterNewFriendsIdsByFollowerCount(int minFollowers, int maxFollowers, int months)
        {
            //TODO: exclude ids already evaluated

            var vewFriendIdsFile = File.ReadAllLines(NewFriendIdsFile);
            var newFriendsIds = new List<string>(vewFriendIdsFile);

            const int maxLookup = 175;
            var count = 0;
            var ticks = DateTime.Now.Ticks;
            var toFollowFile = ToFollowFolder + ticks + ".txt";
            var evaluatedFile = EvaluatedFolder + ticks + ".txt";

            try
            {
                foreach (var newFriendId in newFriendsIds)
                {
                    count++;

                    var userLookup = UsersAsync(newFriendId);
                    userLookup.Wait();

                    if (userLookup.Result.status != null)
                    {
                        var createdAt = DateTime.ParseExact(userLookup.Result.status.created_at,
                            "ddd MMM dd HH:mm:ss %K yyyy", CultureInfo.InvariantCulture.DateTimeFormat);

                        if (userLookup.Result.followers_count > minFollowers &&
                            userLookup.Result.followers_count < maxFollowers &&
                            userLookup.Result.verified == false &&
                            userLookup.Result.lang == "en" &&
                            createdAt > DateTime.Now.AddMonths(months))
                        {
                            if (!File.Exists(toFollowFile))
                            {
                                File.WriteAllText(toFollowFile, newFriendId + Environment.NewLine);
                            }
                            else
                            {
                                File.AppendAllText(toFollowFile, newFriendId + Environment.NewLine);
                            }
                        }

                        if (count == maxLookup)
                            break;
                    }

                    if (!File.Exists(evaluatedFile))
                    {
                        File.WriteAllText(evaluatedFile, newFriendId + Environment.NewLine);
                    }
                    else
                    {
                        File.AppendAllText(evaluatedFile, newFriendId + Environment.NewLine);
                    }
                }
            }
            catch (AggregateException ae)
            {
                ae.Handle((x) =>
                {
                    if (x is RateLimitedException)
                    {
                        var ex = (RateLimitedException)x;

                        var rateLimitResetUtc = Utils.FromUnixTime(ex.RateLimit);
                        var span = rateLimitResetUtc.Subtract(DateTime.UtcNow.AddSeconds(10));

                        System.Console.WriteLine("Try again in {0}", span);
                    }
                    return true;
                });
            }
        }

        private static void AggregateNewFriendsIds()
        {
            // get current friends
            var friendIdsFile = File.ReadAllLines(FriendIdsFile);
            var friendsId = new List<string>(friendIdsFile);

            // get all files for friend's friends
            var filePaths = Directory.GetFiles(FriendsFriendsIdsFolder, "*.txt");

            var newFriends = new List<string>();

            // iterate through friend's friends
            foreach (var file in filePaths)
            {
                var friendsFriendsFile = File.ReadAllLines(file);
                var friendsFriends = new List<string>(friendsFriendsFile);

                var notMyFriends = friendsFriends.Except(friendsId).ToList();
                newFriends.AddRange(notMyFriends.Except(newFriends));
            }

            Utils.StoreIds(newFriends, NewFriendIdsFile);
        }


        private static void GetAndStoreFriendsFriendIds()
        {
            List<string> friendsId;

            if (!File.Exists(FriendIdsFile))
            {
                System.Console.WriteLine("Reading my friends Ids from Twitter");
                var friendsIdsModel = FriendsIdsAsync("screen_name", UserName);
                friendsIdsModel.Wait();
                var ids = friendsIdsModel.Result.ids.Select(id => id.ToString(CultureInfo.InvariantCulture)).ToList();
                friendsId = Utils.StoreIds(ids, FriendIdsFile);
            }
            else
            {
                System.Console.WriteLine("Loading my friends Ids from text file");
                var logFile = File.ReadAllLines(FriendIdsFile);
                friendsId = new List<string>(logFile);
            }

            foreach (var friendId in friendsId)
            {
                var file = string.Format(FriendsFriendsIdsFile, friendId);
                if (!File.Exists(file))
                {
                    System.Console.WriteLine("Reading my friend {0}'s friends ids from Twitter", friendId);

                    _friendId = friendId;
                    _file = file;

                    Utils.DoWithRetry(LookupAndStoreFriendsFriendsIds, 2000);
                }
                else
                {
                    System.Console.WriteLine("{0} already exists", friendId);
                }
            }
        }

        private static void LookupAndStoreFriendsFriendsIds()
        {
            var friendFriendsIdsModel = FriendsIdsAsync("user_id", _friendId);
            friendFriendsIdsModel.Wait();
            var ids = friendFriendsIdsModel.Result.ids.Select(id => id.ToString(CultureInfo.InvariantCulture)).ToList();
            Utils.StoreIds(ids, _file);
        }

        private static async Task<UserLookupModel> UsersAsync(string userId)
        {
            var uri = new Uri(string.Format("{0}?{1}", Urls.UsersLookup, string.Format("user_id={0}", userId)));
            var authHeader = _authorization.GetHeader(uri);
            var userLookup = await _httpHelper.HttpSend<List<UserLookupModel>>(authHeader, uri);

            return userLookup.FirstOrDefault();
        }

        private static async Task<FriendsIdsModel> FriendsIdsAsync(string parameter, string userNameOrId)
        {
            var uri = new Uri(string.Format("{0}?{1}", Urls.FriendsIds, string.Format("{0}={1}", parameter, userNameOrId)));
            var authHeader = _authorization.GetHeader(uri);
            var friendsId = await _httpHelper.HttpSend<FriendsIdsModel>(authHeader, uri);

            return friendsId;
        }

        private static void SetupTwitterApi()
        {
            _secretModel = new SecretModel
            {
                ApiKey = ApiKey,
                ApiSecret = ApiSecret,
                AccessToken = AccessToken,
                AccessTokenSecret = AccessTokenSecret
            };

            _authorization = new Authorization(_secretModel);
            _httpHelper = new HttpHelper();
        }
    }
}
