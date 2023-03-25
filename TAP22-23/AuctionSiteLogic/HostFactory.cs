using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAP22_23.AlarmClock.Interface;
using TAP22_23.AuctionSite.Interface;

namespace Conti {
    public class HostFactory : IHostFactory {
        static void ParameterVerify(string connectionString) {
            if (String.IsNullOrEmpty(connectionString))
                throw new AuctionSiteArgumentNullException("connection string cannot be null or empty");
        }

        public void CreateHost(string connectionString) {
            ParameterVerify(connectionString);

            using (var c = new AuctionSiteContext(connectionString)) {
                try {
                    c.Database.EnsureDeleted();
                    c.Database.EnsureCreated();
                } catch (SqlException e) {
                    throw new AuctionSiteUnavailableDbException("Unavailable Db", e);
                }
            }
        }

        public IHost LoadHost(string connectionString, IAlarmClockFactory alarmClockFactory) {
            ParameterVerify(connectionString);
            if (alarmClockFactory == null)
                throw new AuctionSiteArgumentNullException("alarmClockFactory cannot be null");

            using (var c = new AuctionSiteContext(connectionString)) {
                try {
                    if (!c.Database.CanConnect()) throw new AuctionSiteUnavailableDbException();
                    return new Host(connectionString, alarmClockFactory);
                } catch (SqlException e) {
                    throw new AuctionSiteUnavailableDbException("Unavailable Db", e);
                }
            }
        }
    }
}