using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using System.Xml.Linq;
using TAP22_23.AlarmClock.Interface;
using TAP22_23.AuctionSite.Interface;
using static Conti.AuctionSiteContext;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Conti {

    public class Host : IHost {
        public string ConnectionString { get; }
        public IAlarmClockFactory AlarmClockFactory { get; }
        public Host(string connectionString, IAlarmClockFactory alarmClockFactory) {
            ConnectionString = connectionString;
            AlarmClockFactory = alarmClockFactory;
        }


        public void CreateSite(string name, int timezone, int sessionExpirationTimeInSeconds, double minimumBidIncrement) {
            if (name == "")
                throw new AuctionSiteArgumentException($"Empty string {nameof(name)}");
            var site = new SiteEntity(name, timezone, sessionExpirationTimeInSeconds, minimumBidIncrement);
            Utilities.CheckValidProperties(site);
            using (var c = new AuctionSiteContext(ConnectionString)) {
                c.Sites.Add(site);
                try {
                    c.SaveChanges();
                } catch (AuctionSiteNameAlreadyInUseException e) {
                    throw new AuctionSiteNameAlreadyInUseException(name, $"This {nameof(name)} is already used for another site", e);
                }
            }

        }

        public IEnumerable<(string Name, int TimeZone)> GetSiteInfos() {
            List<SiteEntity> sites;
            using (var c = new AuctionSiteContext(ConnectionString)) {
                try {
                    sites = c.Sites.ToList();
                } catch (SqlException e) {
                    throw new AuctionSiteUnavailableDbException("Unavailable Db", e);
                }
            }
            foreach (var site in sites) {
                yield return (site.Name, site.TimeZone);
            }
        }

        public ISite LoadSite(string name) {
            if (name == "")
                throw new AuctionSiteArgumentException($"{nameof(name)} string cannot be empty");
            if (name == null)
                throw new AuctionSiteArgumentNullException($"{nameof(name)} string cannot be null");
            using (var c = new AuctionSiteContext(ConnectionString)) {
                try {
                    var site = c.Sites.FirstOrDefault(u => u.Name == name);
                    if (site == null)
                        throw new AuctionSiteInexistentNameException($"Site called {name} is not present");
                    return new Site(site, ConnectionString, AlarmClockFactory.InstantiateAlarmClock(site.TimeZone));
                } catch (SqlException e) {
                    throw new AuctionSiteUnavailableDbException("Unavailable Db", e);
                }
            }
        }
    }

}