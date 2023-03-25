using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAP22_23.AuctionSite.Interface;

namespace Conti {
    internal class Session : ISession {
        public string Id { get; }
        public DateTime ValidUntil {
            get {
                using (var c = new AuctionSiteContext(ConnectionString)) {
                    var session = c.Sessions.FirstOrDefault(s => s.SessionEntityId == Id);
                    if (session == null) throw new AuctionSiteInvalidOperationException("Session deleted, please log in");
                    return session.ValidUntil;
                }
            }
        }

        public IUser User { get; }
        private Site Site { get; }

        private string ConnectionString { get; }

        public Session(string id, IUser user, Site site, string connectionString) {
            Id = id;
            User = user;
            Site = site;
            ConnectionString = connectionString;
        }

        /*******************************************************************/
        public override bool Equals(object? obj) {
            var session = obj as Session;
            if (session == null) return false;
            return session.Id == Id;
        }
        public override int GetHashCode() { return Id.GetHashCode(); }

        /*******************************************************************/

        public void Logout() {
            using (var c = new AuctionSiteContext(ConnectionString)) {
                var session = c.Sessions.FirstOrDefault(s => s.SessionEntityId == Id);
                if (session == null) throw new AuctionSiteInvalidOperationException("Session is already deleted");
                c.Remove(session);
                c.SaveChanges();
            }
        }

        public void ResetExpirationTimeSession() {
            using (var c = new AuctionSiteContext(ConnectionString)) {
                var session = c.Sessions.FirstOrDefault(s => s.SessionEntityId == Id);
                if (session == null) throw new AuctionSiteInvalidOperationException("Session deleted,please Log in");
                session.ValidUntil = Site.Now().AddSeconds(Site.SessionExpirationInSeconds);
                c.SaveChanges();
            }
        }

        public IAuction CreateAuction(string description, DateTime endsOn, double startingPrice) {
            if (Site.ToyGetSessions().FirstOrDefault(s => s.Id == Id) == null)
                throw new AuctionSiteInvalidOperationException("Session deleted, please Log in");
            if (ValidUntil < Site.Now()) throw new AuctionSiteInvalidOperationException("Session expired, please Log in");
            if (description == "") throw new AuctionSiteArgumentException($"{nameof(description)} Cannot be empty");
            if (endsOn < Site.Now()) throw new AuctionSiteUnavailableTimeMachineException($"{nameof(endsOn)} is Invalid");
            var auction = new AuctionEntity(description, endsOn, startingPrice, ((User)User).Id, Site.Id);
            Utilities.CheckValidProperties(auction);
            using (var c = new AuctionSiteContext(ConnectionString)) {
                c.Auctions.Add(auction);
                c.SaveChanges();
                ResetExpirationTimeSession();
                auction = c.Auctions.Include(a => a.SellerEntity).First(a => a == auction);
                return new Auction(auction, Site, ConnectionString);
            }
        }

    }

}
