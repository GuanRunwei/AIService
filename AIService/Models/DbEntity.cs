using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace AIService.Models
{
    public class DbEntity : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Dictionary> Dictionaries { get; set; }
        public DbSet<Knowledge> Knowledges { get; set; }
        public DbSet<News> News { get; set; }
        public DbSet<SearchHistory> SearchHistories { get; set; }
        public DbSet<Talk> Talks { get; set; }
        public DbSet<WordsHistory> WordsHistories { get; set; }
        public DbSet<ChatRecord> ChatRecords { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<FollowRecord> FollowRecords { get; set; }
        public DbSet<Picture> Pictures { get; set; }
        public DbSet<NewsComment> NewsComments { get; set; }
        public DbSet<OptionalStock> OptionalStocks { get; set; }
        public DbSet<ASharePlate> ASharePlates { get; set; }
        public DbSet<AShareIndustry> AShareIndustries { get; set; }
        public DbSet<HKSharePlate> HKSharePlates { get; set; }
        public DbSet<StockComment> StockComments { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<SimulationStock> SimulationStocks { get; set; }
        public DbSet<StockAccount> StockAccounts { get; set; }
        public DbSet<SellStock> SellStocks { get; set; }
        public DbSet<TradeHistory> TradeHistories { get; set; }
        public DbSet<StockSearch> StockSearches { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
#if DEBUG
            optionsBuilder.UseSqlServer(@"data source=0.0.0.0,1433;initial catalog=AIStock;User Id=sa;Password=Grw19980628;MultipleActiveResultSets=true ");
#else
            optionsBuilder.UseSqlServer(@"data source=.;initial catalog=AIStock;User Id=sa;Password=Grw19980628 ");
#endif
        }

        ~DbEntity()
        {
            Dispose();
        }

    }
}
