using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TAP22_23.AuctionSite.Interface;

namespace Conti {
    public class AuctionEntity {
        public int AuctionEntityId { get; set; }

        [Required]
        public string Description { get; set; }
        [Required]
        public DateTime EndsOn { get; set; }

        [Range(0, double.MaxValue)]
        [Required]
        public double CurrentPrice { get; set; }
        public double MaxBidValue { get; set; }
        public UserEntity SellerEntity { get; set; }
        public int SellerEntityId { get; set; }
        public UserEntity? WinnerEntity { get; set; }
        public int? WinnerEntityId { get; set; }
        public SiteEntity SiteEntity { get; set; }
        public int SiteEntityId { get; set; }
        public AuctionEntity(string description, DateTime endsOn, double currentPrice, int sellerEntityId, int siteEntityId) {
            Description = description;
            EndsOn = endsOn;
            CurrentPrice = currentPrice;
            SellerEntityId = sellerEntityId;
            SiteEntityId = siteEntityId;
        }
    }

    public class SessionEntity {
      
        public string SessionEntityId { get; set; }
        [Required]
        public DateTime ValidUntil { get; set; }
        [Required]
        public UserEntity UserEntity { get; set; }
        public int UserEntityId { get; set; }
        [Required]
        public SiteEntity SiteEntity { get; set; }
        public int SiteEntityId { get; set; }

        public SessionEntity(string sessionEntityId, DateTime validUntil, int userEntityId, int siteEntityId) {
            SessionEntityId = sessionEntityId;
            ValidUntil = validUntil;
            UserEntityId = userEntityId;
            SiteEntityId = siteEntityId;
        }

    }

    [Index(nameof(Username), nameof(SiteEntityId), IsUnique = true, Name = "UsernameAndSiteUnique")]
    public class UserEntity {
        public int UserEntityId { get; set; }

        [MinLength(DomainConstraints.MinUserName)]
        [MaxLength(DomainConstraints.MaxUserName)]
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; } 

        public SiteEntity? SiteEntity { get; set; }
        public int SiteEntityId { get; set; }

        public SessionEntity? Session { get; set; }
        public List<AuctionEntity>? AuctionSellers { get; set; }
        public List<AuctionEntity>? AuctionCurrentWinners { get; set; }
        public UserEntity(string username, string password, int siteEntityId) {
            Username = username;
            Password = password;
            SiteEntityId = siteEntityId;
            Session = null;

            AuctionSellers = new List<AuctionEntity>();
            AuctionCurrentWinners = new List<AuctionEntity>();
        }
    }

    [Index(nameof(Name), IsUnique = true, Name = "NameUnique")]
    public class SiteEntity {
        public int SiteEntityId { get; set; }

        [MinLength(DomainConstraints.MinSiteName)]
        [MaxLength(DomainConstraints.MaxSiteName)]
        [Required]
        public string Name { get; set; }

        [Range(DomainConstraints.MinTimeZone, DomainConstraints.MaxTimeZone)]
        [Required]
        public int TimeZone { get; set; }

        [Range(0, double.MaxValue)]
        [Required]
        public double MinimumBidIncrement { get; set; }

        [Range(0, int.MaxValue)]
        [Required]
        public int SessionExpirationTimeInSeconds { get; set; }

        public List<UserEntity>? Users { get; set; }
        public List<SessionEntity>? Sessions { get; set; }
        public List<AuctionEntity>? Auctions { get; set; }
        public SiteEntity(string name, int timeZone, int sessionExpirationTimeInSeconds, double minimumBidIncrement) {
            Name = name;
            TimeZone = timeZone;
            SessionExpirationTimeInSeconds = sessionExpirationTimeInSeconds;
            MinimumBidIncrement = minimumBidIncrement;

            Users = new List<UserEntity>();
            Sessions = new List<SessionEntity>();
            Auctions = new List<AuctionEntity>();
        }
    }

}