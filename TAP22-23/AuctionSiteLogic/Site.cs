using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TAP22_23.AlarmClock.Interface;
using TAP22_23.AuctionSite.Interface;

namespace Conti {
    public class Site : ISite {
        public string Name { get; }
        public int Timezone { get; }
        public int SessionExpirationInSeconds { get; }
        public double MinimumBidIncrement { get; }
        public int Id { get; }
        private string ConnectionString { get; }
        private IAlarmClock AlarmClock { get; }
        private IAlarm Alarm { get; set; }
        public Site(int id, string name, int timeZone, int sessionExpirationInSeconds, double minimumBidIncrement, string connectionString, IAlarmClock alarmClock) {
            Id = id;
            Name = name;
            Timezone = timeZone;
            SessionExpirationInSeconds = sessionExpirationInSeconds;
            MinimumBidIncrement = minimumBidIncrement;
            ConnectionString = connectionString;
            AlarmClock = alarmClock;
            Alarm = alarmClock.InstantiateAlarm(5 * 60 * 1000);
            Alarm.RingingEvent += deleteExpiredSessionsEvent;
            deleteExpiredSessionsEvent();
        }
        public Site(SiteEntity site, string connectionString, IAlarmClock alarmClock) :
            this(site.SiteEntityId, site.Name, site.TimeZone, site.SessionExpirationTimeInSeconds, site.MinimumBidIncrement, connectionString, alarmClock) { }

        private void deleteExpiredSessionsEvent() {
            using (var c = new AuctionSiteContext(ConnectionString)) {
                var sessionExpiredEvent = c.Sessions.ToList();
                var sessionExpired = sessionExpiredEvent.Where(s => s.SiteEntityId == Id && s.ValidUntil < Now()).ToList();
                c.RemoveRange(sessionExpired);
                c.SaveChanges();
            }
            Alarm = AlarmClock.InstantiateAlarm(5 * 60 * 1000);
        }

        private void CheckDeleteSite() {

            using (var c = new AuctionSiteContext(ConnectionString)) {
                var site = c.Sites.FirstOrDefault(s => s.SiteEntityId == Id);
                if (site == null) throw new AuctionSiteInvalidOperationException("Site deleted, please Load a existing site");
            }
        }
        public DateTime Now() {
            return AlarmClock.Now;
        }
        /*******************************************************************/
        private IEnumerable<IUser> GetEnumerableUsers(List<UserEntity> users) {
            foreach (var user in users) {
                yield return new User(user, this, ConnectionString);
            }
        }
        public IEnumerable<IUser> ToyGetUsers() {
            List<UserEntity>? users;
            using (var c = new AuctionSiteContext(ConnectionString)) {
                CheckDeleteSite();
                users = c.Users.Where(u => u.SiteEntityId == Id).ToList();
            }
            return GetEnumerableUsers(users);
        }

        /*******************************************************************/
        private IEnumerable<ISession> GetEnumerableSessions(List<SessionEntity> sessions) {
            foreach (var session in sessions)
                yield return new Session(session.SessionEntityId, new User(session.UserEntity, this, ConnectionString), this, ConnectionString);
        }

        public IEnumerable<ISession> ToyGetSessions() {
            List<SessionEntity>? sessions;
            using (var c = new AuctionSiteContext(ConnectionString)) {
                CheckDeleteSite();
                sessions = c.Sessions.Include(s => s.UserEntity).Where(s => s.SiteEntityId == Id).ToList();
            }

            return GetEnumerableSessions(sessions);

        }
        /*******************************************************************/
        private IEnumerable<IAuction> GetEnumerableAuctions(List<AuctionEntity> auctions) {
            foreach (var auction in auctions)
                yield return new Auction(auction, this, ConnectionString);
        }
        //true = solo quelle aperte, false = tutte le aste
        public IEnumerable<IAuction> ToyGetAuctions(bool onlyNotEnded) {
            List<AuctionEntity> auctions;
            using (var c = new AuctionSiteContext(ConnectionString)) {
                CheckDeleteSite();
                auctions = c.Auctions.Include(a => a.SellerEntity)
                    .Where(a => a.SiteEntityId == Id && (!onlyNotEnded || a.EndsOn > Now())).ToList();
            }

            return GetEnumerableAuctions(auctions);
        }
        /*******************************************************************/
        public ISession? Login(string username, string password) {
            if (username == "") throw new AuctionSiteArgumentException($"empty string {nameof(username)}");
            var validetuser = new UserEntity(username, Utilities.hashPassword(password), Id);
            Utilities.CheckValidProperties(validetuser);
            if (password.Length < DomainConstraints.MinUserPassword)
                throw new AuctionSiteArgumentException($"{nameof(password)} too short");
            using (var c = new AuctionSiteContext(ConnectionString)) {
                CheckDeleteSite();
                var user = c.Users.FirstOrDefault(u => u.Username == username && u.SiteEntityId == Id);
                if (user == null) return null;
                if (!Utilities.verifyPassword(user.Password, password)) return null;

                var session = c.Sessions.FirstOrDefault(s => s.UserEntityId == user.UserEntityId && s.ValidUntil > Now());
                if (session != null) return new Session(session.SessionEntityId, new User(user, this, ConnectionString), this, ConnectionString);

                var newSession = new SessionEntity(user.Username + Name + Now(), Now().AddSeconds(SessionExpirationInSeconds), user.UserEntityId, Id);

                c.Sessions.Add(newSession);
                c.SaveChanges();

                return new Session(newSession.SessionEntityId, new User(user, this, ConnectionString), this, ConnectionString);

            }
        }
        public void CreateUser(string username, string password) {
            if (username == "") throw new AuctionSiteArgumentException($"empty string {nameof(username)}");
            var user = new UserEntity(username, Utilities.hashPassword(password), Id);
            Utilities.CheckValidProperties(user);
            if (password.Length < DomainConstraints.MinUserPassword)
                throw new AuctionSiteArgumentException($"{nameof(password)} too short");
            using (var c = new AuctionSiteContext(ConnectionString)) {
                c.Users.Add(user);
                try {
                    c.SaveChanges();
                } catch (AuctionSiteNameAlreadyInUseException e) {
                    throw new AuctionSiteNameAlreadyInUseException(username, $"This {nameof(username)} is already used for site with name {Name}", e);
                }
            }
        }

        public void Delete() {
            using (var c = new AuctionSiteContext(ConnectionString)) {
                var site = c.Sites.FirstOrDefault(s => s.SiteEntityId == Id);
                if (site == null) throw new AuctionSiteInvalidOperationException("Site is already deleted");
                c.Remove(site);
                c.SaveChanges();
            }
        }

    }

}
