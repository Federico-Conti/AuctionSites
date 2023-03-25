using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Runtime.Serialization;
using TAP22_23.AuctionSite.Interface;

namespace Conti {

    public class AuctionSiteContext : TapDbContext {
        public AuctionSiteContext(string connectionString) : base(new DbContextOptionsBuilder<AuctionSiteContext>().UseSqlServer(connectionString).Options) {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            base.OnConfiguring(optionsBuilder);
            // optionsBuilder.LogTo((s) => Console.WriteLine(s)).EnableSensitiveDataLogging();
        }

        public override int SaveChanges() {
            try {
                return base.SaveChanges();
            } catch (SqlException e) {
                throw new AuctionSiteUnavailableDbException("Unavailable Db", e);
            } catch (DbUpdateException e) {
                var sqlException = e.InnerException as SqlException;
                if (sqlException == null)
                    throw new AuctionSiteUnavailableDbException("Missing information from Db exception", e);
                switch (sqlException.Number) { // ref: http://www.sql-server-helper.com/error-messages/msg-1-500.aspx
                    case < 54: throw new AuctionSiteUnavailableDbException("Unavailable Db", e);
                    case 2601: throw new AuctionSiteNameAlreadyInUseException("AuctionSite", "Unique violation", e);
                    case 515: throw new AuctionSiteArgumentNullException("Not Null violation", e);
                    case 547: throw new AuctionSiteInvalidOperationException("Invalid Foreign key", e);
                    case 2627: throw new AuctionSiteNameAlreadyInUseException("AuctionSite", "Invalid Primary key", e);
                    default:
                        throw new AuctionSiteUnavailableDbException("Missing information form Db exception", e);
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            var session = modelBuilder.Entity<SessionEntity>();
            session.HasOne(s => s.SiteEntity)
                .WithMany(s => s.Sessions)
                .HasForeignKey(s => s.SiteEntityId)
                .OnDelete(DeleteBehavior.NoAction);
            session.HasOne(s => s.UserEntity)
                .WithOne(u => u.Session);
            session.HasIndex(x => x.UserEntityId).IsUnique(false);

            var user = modelBuilder.Entity<UserEntity>();
            user.HasMany(u => u.AuctionSellers)
                .WithOne(a => a.SellerEntity)
                .HasForeignKey(a => a.SellerEntityId)
                .OnDelete(DeleteBehavior.NoAction);
            user.HasMany(u => u.AuctionCurrentWinners)
                .WithOne(a => a.WinnerEntity)
                .HasForeignKey(a => a.WinnerEntityId)
                .OnDelete(DeleteBehavior.NoAction);
            user.HasOne(u => u.SiteEntity)
                .WithMany(s => s.Users)
                .HasForeignKey(u => u.SiteEntityId)
                .OnDelete(DeleteBehavior.Cascade);


            var auction = modelBuilder.Entity<AuctionEntity>();
            auction.HasOne(a => a.SiteEntity)
                .WithMany(s => s.Auctions)
                .HasForeignKey(a => a.SiteEntityId)
                .OnDelete(DeleteBehavior.Cascade);



        }

        public DbSet<SiteEntity> Sites { get; set; }
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<SessionEntity> Sessions { get; set; }
        public DbSet<AuctionEntity> Auctions { get; set; }
    }

}