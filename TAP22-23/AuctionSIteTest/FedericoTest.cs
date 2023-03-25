namespace TAP22_23.AuctionSite.Testing {
    public class FedericoTest : AuctionSiteTests {
        [SetUp]
        public void SiteUsersAuctionInitialize() {
            const int timeZone = -2;
            TheHost.CreateSite(SiteName, timeZone, 300, 7);
            TheClock = (TestAlarmClock)TheAlarmClockFactory.InstantiateAlarmClock(timeZone);
            Site = TheHost.LoadSite(SiteName);
            Seller = CreateAndLogUser("seller", out SellerSession, Site);
            Bidder1 = CreateAndLogUser("bidder1", out Bidder1Session, Site);
            Bidder2 = CreateAndLogUser("bidder2", out Bidder2Session, Site);
            TheAuction = SellerSession.CreateAuction("Beautiful object to be desired by everybody", TheClock.Now.AddDays(1), 10);
            TheAuction1 = SellerSession.CreateAuction("Beautiful object to be desired by everybody", TheClock.Now.AddDays(1), 10);
            TheAuction2 = SellerSession.CreateAuction("Beautiful object to be desired by everybody", TheClock.Now.AddDays(1), 10);
        }

        protected ISite Site = null!;

        protected IUser Seller = null!;
        protected ISession SellerSession = null!;

        protected IUser Bidder1 = null!;
        protected ISession Bidder1Session = null!;

        protected IUser Bidder2 = null!;
        protected ISession Bidder2Session = null!;

        protected IAuction TheAuction = null!;
        protected IAuction TheAuction1 = null!;
        protected IAuction TheAuction2 = null!;

        protected const string SiteName = "site for auction tests";

        protected IUser CreateAndLogUser(string username, out ISession session, ISite site) {
            site.CreateUser(username, username);
            var user = site.ToyGetUsers().SingleOrDefault(u => u.Username == username);
            if (null == user)
                Assert.Inconclusive($"user {username} has not been created");
            var login = site.Login(username, username);
            if (null == login)
                Assert.Inconclusive($"user {username} should log in with password {username}");
            session = login!;
            return user!;
        }

        [Test]
        public void AuctionGame() {
            TheAuction.Bid(Bidder1Session, 11);
            var winner = TheAuction.CurrentWinner();
            Assert.That(winner, Is.EqualTo(Bidder1));
            TheAuction.Bid(Bidder1Session, 11 + 7);
            winner = TheAuction.CurrentWinner();
            Assert.That(winner, Is.EqualTo(Bidder1));
            TheAuction.Bid(Bidder2Session, 11 + 7 + 1);
            winner = TheAuction.CurrentWinner();
            Assert.That(winner, Is.EqualTo(Bidder2));
            TheAuction.Bid(Bidder1Session, 11 + 7 + 1);
            winner = TheAuction.CurrentWinner();
            Assert.That(winner, Is.EqualTo(Bidder2));
        }


        [Test]
        public void DeleteUserWith3WonAuctionsExpiredUpdateWinnerToNull() {
            TheAuction.Bid(Bidder1Session, 11);
            TheAuction1.Bid(Bidder1Session, 11);
            TheAuction2.Bid(Bidder1Session, 11);
            TheClock!.AddHours(36);
            Bidder1.Delete();
            Assert.Multiple(() => {
                Assert.That(TheAuction.CurrentWinner(), Is.Null);
                Assert.That(TheAuction1.CurrentWinner(), Is.Null);
                Assert.That(TheAuction2.CurrentWinner(), Is.Null);
            });

        }


        [Test]
        public void BidOnAuctionExpiredThrow() {
            TheClock!.AddHours(36);
            Assert.That(() => TheAuction.Bid(Bidder1Session, 11), Throws.TypeOf<AuctionSiteInvalidOperationException>());

        }


        [Test]
        public void CallWonAuctionsOnUserWithoutExpiredAuctionsWonReturnEmpty() {
            TheAuction.Bid(Bidder1Session, 11);
            TheAuction.Bid(Bidder2Session, 100);
            TheClock!.AddHours(36);
            var wonAuctions = Bidder1.WonAuctions();
            Assert.That(wonAuctions, Is.Empty);
        }


        [Test]
        public void CallWonAuctionsOnUserWithExpiredAuctionsWonReturn3() {
            TheAuction.Bid(Bidder1Session, 11);
            TheAuction1.Bid(Bidder1Session, 11);
            TheAuction2.Bid(Bidder1Session, 11);
            TheClock!.AddHours(36);
            var wonAuctions = Bidder1.WonAuctions().Count();
            Assert.That(wonAuctions, Is.EqualTo(3));
        }

        [Test]
        public void CallWonAuctionsOnUserWithNotExpiredAuctionsWonReturn0() {
            TheAuction.Bid(Bidder1Session, 11);
            TheAuction1.Bid(Bidder1Session, 11);
            TheAuction2.Bid(Bidder1Session, 11);
            var wonAuctions = Bidder1.WonAuctions().Count();
            Assert.That(wonAuctions, Is.EqualTo(0));
        }

        [Test]
        public void DeleteSiteWithUserSessionAuctionReturnEmptyDb() {
            Site.Delete();
            Assert.Multiple(() => {
                Assert.That(() => Site.ToyGetUsers(), Throws.TypeOf<AuctionSiteInvalidOperationException>());
                Assert.That(() => Site.ToyGetSessions(), Throws.TypeOf<AuctionSiteInvalidOperationException>());
                Assert.That(() => Site.ToyGetAuctions(true), Throws.TypeOf<AuctionSiteInvalidOperationException>());
            });
        }

    }
}