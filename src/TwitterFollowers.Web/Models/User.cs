using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace TwitterFollowers.Web.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Int64 Id { get; set; }

        public string Name { get; set; }
        public string ScreenName { get; set; }

        public string ProfileImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string Location { get; set; }
        public bool FollowRequestSent { get; set; }
        public string IdStr { get; set; }
        public string Url { get; set; }
        public int? UtcOffset { get; set; }
        public bool ProfileUseBackgroundImage { get; set; }
        public int? ListedCount { get; set; }
        public string Lang { get; set; }
        public int? FollowersCount { get; set; }
        public bool Protected { get; set; }
        public bool? Notifications { get; set; }
        public bool Verified { get; set; }
        public bool GeoEnabled { get; set; }
        public string TimeZone { get; set; }
        public string Description { get; set; }
        public bool DefaultProfileImage { get; set; }
        public string ProfileBackgroundImageUrl { get; set; }
        public int? StatusesCount { get; set; }
        public int? FriendsCount { get; set; }
        public bool? Following { get; set; }

        public bool AutoAdded { get; set; }
    }
}