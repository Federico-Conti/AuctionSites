using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAP22_23.AuctionSite.Interface;
using static System.Collections.Specialized.BitVector32;

namespace Conti {
    public class User : IUser {

        public string Username { get; }
        public string Password { get; }
        public int Id { get; }

        private string ConnectionString { get; }
        private Site Site { get; }

        public User(int id, string username, string password, Site site, string connectionString) {
            Username = username;
            Password = password;
            Id = id;
            ConnectionString = connectionString;
            Site = site;
        }

        public User(UserEntity user, Site site, string connectionString) :
            this(user.UserEntityId, user.Username, user.Password, site, connectionString) { }

        public IEnumerable<IAuction> WonAuctions() {
            using (var c = new AuctionSiteContext(ConnectionString)) {
                var user = c.Users.Include(u => u.AuctionCurrentWinners).FirstOrDefault(u => u.UserEntityId == Id);
                if (user == null) throw new AuctionSiteInvalidOperationException($"{nameof(Username)} deleted, please Create new user");
                var closeAuctions = Site.ToyGetAuctions(false)
                    .Where(s => s.EndsOn < Site.Now() && s.CurrentWinner() != null && s.CurrentWinner()!.Equals(new User(user, Site, ConnectionString)));
                return closeAuctions;
            }
        }

        //Oltre a questo,l'unico modo per eliminare gli utenti è cancellare il sito e questo implica anche la cancellazione di tutte le aste attive e non
        public void Delete() {
            var openAuction = Site.ToyGetAuctions(true).FirstOrDefault(a => Equals(a.Seller) || Equals(a.CurrentWinner()));
            if (openAuction != null) throw new AuctionSiteInvalidOperationException($"{Username} is the owner or current winner of an open auction");

            using (var c = new AuctionSiteContext(ConnectionString)) {
                var user = c.Users.FirstOrDefault(u => u.UserEntityId == Id);
                if (user == null) throw new AuctionSiteInvalidOperationException($"{Username} is already deleted");
                //Delete Own Auctions
                var closeAuctions = Site.ToyGetAuctions(false).Where(s => s.Seller.Equals(this));
                foreach (var auction in closeAuctions)
                    auction.Delete();
                // Set Winners to null in Expired Auction if Winner is the user I'm deleting
                var closeWonAuctions = Site.ToyGetAuctions(false)
                    .Where(s => s.EndsOn < Site.Now() && (s.CurrentWinner() != null && s.CurrentWinner()!.Equals(new User(user, Site, ConnectionString))));

                foreach (var auction in closeWonAuctions) {
                    var closeWonAuctionsEntity = c.Auctions.FirstOrDefault(a => a.AuctionEntityId == auction.Id);
                    if (closeWonAuctionsEntity != null)
                        closeWonAuctionsEntity.WinnerEntityId = null;
                }

                c.Remove(user);
                c.SaveChanges();
            }
        }

        public override bool Equals(object? obj) {
            var objuser = obj as User;
            if (objuser == null) return false;
            return Id == objuser.Id;
        }

        public override int GetHashCode() {
            return Id.GetHashCode();
        }
    }
}
