using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAP22_23.AuctionSite.Interface;
using static System.Collections.Specialized.BitVector32;

namespace Conti {
    internal class Auction : IAuction {
        public int Id { get; }
        public IUser Seller { get; }
        public string Description { get; }
        public DateTime EndsOn { get; }
        private Site Site { get; }
        private string ConnectionString { get; }

        public Auction(int id, IUser seller, Site site, string description, DateTime endOn, string connectionString) {
            Id = id;
            Seller = seller;
            Description = description;
            EndsOn = endOn;
            ConnectionString = connectionString;
            Site = site;
        }
        public Auction(AuctionEntity auction, Site site, string connectionString) :
            this(auction.AuctionEntityId, new User(auction.SellerEntity, site, connectionString), site, auction.Description, auction.EndsOn, connectionString) { }

        /*******************************************************************/
        public override bool Equals(object? obj) {
            var auction = obj as Auction;
            if (auction == null) return false;
            return auction.Id == Id && auction.Site.Id == Site.Id;
        }

        public override int GetHashCode() { return Id.GetHashCode(); }

        private void CheckDeleteAuction() {
            using (var c = new AuctionSiteContext(ConnectionString)) {
                var auction = c.Auctions.FirstOrDefault(a => a.AuctionEntityId == Id);
                if (auction == null) throw new AuctionSiteInvalidOperationException("Auction deleted");
            }
        }
        /*******************************************************************/
        public IUser? CurrentWinner() {
            using (var c = new AuctionSiteContext(ConnectionString)) {
                CheckDeleteAuction();
                var wonuser = c.Auctions.Include(a => a.WinnerEntity).FirstOrDefault(a => a.AuctionEntityId == Id)?.WinnerEntity;
                return wonuser != null ? new User(wonuser, Site, ConnectionString) : null;
            }
        }

        public double CurrentPrice() {
            using (var c = new AuctionSiteContext(ConnectionString)) {
                CheckDeleteAuction();
                var currentPrice = c.Auctions.FirstOrDefault(a => a.AuctionEntityId == Id)!.CurrentPrice;
                return currentPrice;
            }
        }

        public void Delete() {
            using (var c = new AuctionSiteContext(ConnectionString)) {
                var auction = c.Auctions.FirstOrDefault(a => a.AuctionEntityId == Id);
                if (auction == null) throw new AuctionSiteInvalidOperationException("Auction is already deleted");
                var thisAuction = c.Auctions.FirstOrDefault(a => a.AuctionEntityId == Id);
                if (thisAuction != null) {
                    c.Remove(thisAuction);
                    c.SaveChanges();
                }
            }
        }

        /*******************************************************************/
        public bool Bid(ISession session, double offer) {

            BidParamVerify(session, offer);

            using (var c = new AuctionSiteContext(ConnectionString)) {
                var bidderSession = session as Session;
                var thisAuction = c.Auctions.FirstOrDefault(a => a.AuctionEntityId == Id);

                if (EndsOn < Site.Now()) throw new AuctionSiteInvalidOperationException("Auction is already close");

                if (Site.ToyGetSessions().FirstOrDefault(s => s.Id == bidderSession!.Id) == null)
                    throw new AuctionSiteArgumentException($"{nameof(session)} deleted, please Log in");
                if (bidderSession!.ValidUntil < Site.Now()) throw new AuctionSiteArgumentException($"{nameof(session)} expired, please Log in");

                var bidderSiteId = c.Sessions.First(s => s.SessionEntityId == bidderSession.Id).SiteEntityId;
                if (bidderSiteId != Site.Id) throw new AuctionSiteArgumentException("Auction not found on this site");

                var bidder = c.Users.First(u => u.UserEntityId == ((User)session.User).Id);
                if (bidder == null) throw new AuctionSiteInvalidOperationException("User deleted");

                if (thisAuction!.SellerEntityId == bidder.UserEntityId) throw new AuctionSiteArgumentException("Owner cannot bid");

                ((Session)session).ResetExpirationTimeSession();

                return IsValidBid(offer, thisAuction, bidder, c);
            }
        }

        private void BidParamVerify(ISession session, double offer) {
            if (session == null) throw new AuctionSiteArgumentNullException($"{nameof(session)} cannot be null");
            if (offer < 0) throw new AuctionSiteArgumentOutOfRangeException($"{nameof(offer)} must be greater than zero");
            var bidderSession = session as Session;
            if (bidderSession == null) throw new AuctionSiteArgumentNullException($"{nameof(session)} cannot be null");
            CheckDeleteAuction();
        }

        private bool IsValidBid(double offer, AuctionEntity thisAuction, UserEntity bidder, AuctionSiteContext c) {

            if (thisAuction.WinnerEntityId == bidder.UserEntityId) {
                if (offer < thisAuction.MaxBidValue + Site.MinimumBidIncrement) return false;
                thisAuction.MaxBidValue = offer;
            } else if (thisAuction.WinnerEntityId == null) {
                if (offer < CurrentPrice()) return false;
                thisAuction.MaxBidValue = offer;
                thisAuction.WinnerEntityId = bidder.UserEntityId;
            } else if (thisAuction.WinnerEntityId != null && thisAuction.WinnerEntityId != bidder.UserEntityId) {
                if (offer < CurrentPrice() + Site.MinimumBidIncrement) return false;
                if (thisAuction.MaxBidValue < offer) {
                    thisAuction.CurrentPrice = Math.Min(thisAuction.MaxBidValue + Site.MinimumBidIncrement, offer);
                    thisAuction.MaxBidValue = offer;
                    thisAuction.WinnerEntityId = bidder.UserEntityId;
                } else
                    thisAuction.CurrentPrice = Math.Min(thisAuction.MaxBidValue, offer + Site.MinimumBidIncrement);
            }

            c.SaveChanges();
            return true;
        }

        /*******************************************************************/
    }
}
