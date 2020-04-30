﻿// <auto-generated />
using System;
using AIService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AIService.Migrations
{
    [DbContext(typeof(DbEntity))]
    partial class DbEntityModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.14-servicing-32113")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("AIService.Models.Answer", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("AnswerContent");

                    b.Property<DateTime>("AnswerTime");

                    b.Property<long>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Answers");
                });

            modelBuilder.Entity("AIService.Models.AShareIndustry", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("IndustryCode");

                    b.Property<string>("IndustryName");

                    b.Property<int>("ParentId");

                    b.Property<string>("ParentPlateName");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.ToTable("AShareIndustries");
                });

            modelBuilder.Entity("AIService.Models.ASharePlate", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("ParentId");

                    b.Property<string>("PlateName");

                    b.HasKey("Id");

                    b.ToTable("ASharePlates");
                });

            modelBuilder.Entity("AIService.Models.ChatRecord", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ChatContent");

                    b.Property<DateTime>("ChatTime");

                    b.Property<long>("UserId1");

                    b.Property<long>("UserId2");

                    b.HasKey("Id");

                    b.ToTable("ChatRecords");
                });

            modelBuilder.Entity("AIService.Models.Comment", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("CommentTime");

                    b.Property<string>("Commenter");

                    b.Property<string>("Point");

                    b.Property<long>("TalkId");

                    b.Property<long>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("TalkId");

                    b.ToTable("Comments");
                });

            modelBuilder.Entity("AIService.Models.Dictionary", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Explain");

                    b.Property<string>("Word");

                    b.HasKey("Id");

                    b.ToTable("Dictionaries");
                });

            modelBuilder.Entity("AIService.Models.Feedback", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Content");

                    b.Property<DateTime>("FeedbackTime");

                    b.Property<long>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Feedbacks");
                });

            modelBuilder.Entity("AIService.Models.FollowRecord", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("FollowTime");

                    b.Property<long>("FollowedId");

                    b.Property<long>("FollowingId");

                    b.HasKey("Id");

                    b.ToTable("FollowRecords");
                });

            modelBuilder.Entity("AIService.Models.HKSharePlate", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ParentName");

                    b.Property<string>("PlateCode");

                    b.Property<string>("PlateName");

                    b.HasKey("Id");

                    b.ToTable("HKSharePlates");
                });

            modelBuilder.Entity("AIService.Models.Knowledge", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Answer");

                    b.Property<string>("PossibleQuestion");

                    b.Property<string>("Question");

                    b.HasKey("Id");

                    b.ToTable("Knowledges");
                });

            modelBuilder.Entity("AIService.Models.News", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Content");

                    b.Property<DateTime>("IssueTime");

                    b.Property<int>("NewsType");

                    b.Property<string>("PicUrl1");

                    b.Property<string>("PicUrl2");

                    b.Property<string>("PicUrl3");

                    b.Property<string>("Source");

                    b.Property<string>("Title");

                    b.HasKey("Id");

                    b.ToTable("News");
                });

            modelBuilder.Entity("AIService.Models.NewsComment", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("CommentTime");

                    b.Property<string>("Commenter");

                    b.Property<long>("NewsId");

                    b.Property<string>("Point");

                    b.Property<long>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("NewsId");

                    b.ToTable("NewsComments");
                });

            modelBuilder.Entity("AIService.Models.OptionalStock", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("AppointRate");

                    b.Property<string>("Diff_Money");

                    b.Property<string>("Diff_Rate");

                    b.Property<string>("NowPrice");

                    b.Property<string>("OpenPrice");

                    b.Property<string>("Pb");

                    b.Property<string>("Pe");

                    b.Property<string>("StockCode");

                    b.Property<int>("StockExchange");

                    b.Property<string>("StockName");

                    b.Property<string>("StockPinyin");

                    b.Property<int>("StockTendency");

                    b.Property<int>("StockType");

                    b.Property<string>("StockValue");

                    b.Property<string>("Swing");

                    b.Property<string>("TodayMax");

                    b.Property<string>("TodayMin");

                    b.Property<string>("TradeNum");

                    b.Property<string>("Turnover");

                    b.Property<long>("UserId");

                    b.Property<string>("YesterdayClosePrice");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("OptionalStocks");
                });

            modelBuilder.Entity("AIService.Models.Picture", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<double>("FileSize");

                    b.Property<string>("FileType");

                    b.Property<string>("FileUrl");

                    b.Property<long>("TalkId");

                    b.HasKey("Id");

                    b.HasIndex("TalkId");

                    b.ToTable("Pictures");
                });

            modelBuilder.Entity("AIService.Models.Question", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("AnswerId");

                    b.Property<string>("QuestionContent");

                    b.Property<DateTime>("QuestionTime");

                    b.Property<long>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Questions");
                });

            modelBuilder.Entity("AIService.Models.SearchHistory", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Answer");

                    b.Property<string>("HistoricalText");

                    b.Property<long>("KnowledgeId");

                    b.Property<DateTime>("SearchTime");

                    b.Property<long>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("KnowledgeId");

                    b.HasIndex("UserId");

                    b.ToTable("SearchHistories");
                });

            modelBuilder.Entity("AIService.Models.SellStock", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<double>("BuyPrice");

                    b.Property<double>("SellPrice");

                    b.Property<int>("SellStockNumber");

                    b.Property<DateTime>("SellTime");

                    b.Property<long>("StockAccountId");

                    b.Property<string>("StockCode");

                    b.Property<string>("StockName");

                    b.HasKey("Id");

                    b.HasIndex("StockAccountId");

                    b.ToTable("SellStocks");
                });

            modelBuilder.Entity("AIService.Models.SimulationStock", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<double>("BuyPrice");

                    b.Property<DateTime>("BuyTime");

                    b.Property<double>("NowPrice");

                    b.Property<long>("StockAccountId");

                    b.Property<string>("StockCode");

                    b.Property<string>("StockName");

                    b.Property<int>("StockNumber");

                    b.Property<bool>("Valid");

                    b.HasKey("Id");

                    b.HasIndex("StockAccountId");

                    b.ToTable("SimulationStocks");
                });

            modelBuilder.Entity("AIService.Models.Stock", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("BusinessScope");

                    b.Property<string>("CompanyId");

                    b.Property<string>("EstablishDate");

                    b.Property<string>("IndustryName");

                    b.Property<string>("OfficeAddress");

                    b.Property<string>("RegisterAddress");

                    b.Property<string>("StockCode");

                    b.Property<int>("StockExchangeName");

                    b.Property<string>("StockName");

                    b.Property<int>("StockType");

                    b.Property<string>("StockValue");

                    b.HasKey("Id");

                    b.ToTable("Stocks");
                });

            modelBuilder.Entity("AIService.Models.StockAccount", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<double>("Profit_or_Loss");

                    b.Property<long>("Rank");

                    b.Property<double>("SumMoney");

                    b.Property<double>("SumStockValue");

                    b.Property<long>("UserId");

                    b.Property<double>("ValidMoney");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("StockAccounts");
                });

            modelBuilder.Entity("AIService.Models.StockComment", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("CommentTime");

                    b.Property<string>("Point");

                    b.Property<long>("StockId");

                    b.Property<long>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("StockId");

                    b.HasIndex("UserId");

                    b.ToTable("StockComments");
                });

            modelBuilder.Entity("AIService.Models.StockSearch", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("SearchTime");

                    b.Property<int>("Status");

                    b.Property<string>("StockCode");

                    b.Property<string>("StockName");

                    b.Property<long>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("StockSearches");
                });

            modelBuilder.Entity("AIService.Models.Talk", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Content");

                    b.Property<DateTime>("TalkTime");

                    b.Property<int>("TalkType");

                    b.Property<long>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Talks");
                });

            modelBuilder.Entity("AIService.Models.TradeHistory", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<long>("StockAccountId");

                    b.Property<string>("StockCode");

                    b.Property<string>("StockName");

                    b.Property<DateTime>("TradeTime");

                    b.Property<int>("TransactionAmount");

                    b.Property<double>("TransactionPrice");

                    b.Property<int>("TransactionType");

                    b.Property<double>("TransactionValue");

                    b.HasKey("Id");

                    b.HasIndex("StockAccountId");

                    b.ToTable("TradeHistories");
                });

            modelBuilder.Entity("AIService.Models.User", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("CoinNumber");

                    b.Property<DateTime>("CreateTime");

                    b.Property<int>("FansNumber");

                    b.Property<int>("FollowNumber");

                    b.Property<int>("Gender");

                    b.Property<string>("ImageUrl");

                    b.Property<string>("Password");

                    b.Property<string>("Phonenumber");

                    b.Property<string>("Remark");

                    b.Property<int>("UserType");

                    b.Property<string>("Username");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("AIService.Models.WordsHistory", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Explain");

                    b.Property<DateTime>("SearchTime");

                    b.Property<long>("UserId");

                    b.Property<string>("Word");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("WordsHistories");
                });

            modelBuilder.Entity("AIService.Models.Answer", b =>
                {
                    b.HasOne("AIService.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("AIService.Models.AShareIndustry", b =>
                {
                    b.HasOne("AIService.Models.ASharePlate", "Parent")
                        .WithMany()
                        .HasForeignKey("ParentId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("AIService.Models.Comment", b =>
                {
                    b.HasOne("AIService.Models.Talk", "Talk")
                        .WithMany("Comments")
                        .HasForeignKey("TalkId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("AIService.Models.Feedback", b =>
                {
                    b.HasOne("AIService.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("AIService.Models.NewsComment", b =>
                {
                    b.HasOne("AIService.Models.News", "News")
                        .WithMany("NewsComments")
                        .HasForeignKey("NewsId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("AIService.Models.OptionalStock", b =>
                {
                    b.HasOne("AIService.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("AIService.Models.Picture", b =>
                {
                    b.HasOne("AIService.Models.Talk", "Talk")
                        .WithMany("Pictures")
                        .HasForeignKey("TalkId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("AIService.Models.Question", b =>
                {
                    b.HasOne("AIService.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("AIService.Models.SearchHistory", b =>
                {
                    b.HasOne("AIService.Models.Knowledge", "Knowledge")
                        .WithMany()
                        .HasForeignKey("KnowledgeId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("AIService.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("AIService.Models.SellStock", b =>
                {
                    b.HasOne("AIService.Models.StockAccount", "StockAccount")
                        .WithMany()
                        .HasForeignKey("StockAccountId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("AIService.Models.SimulationStock", b =>
                {
                    b.HasOne("AIService.Models.StockAccount", "StockAccount")
                        .WithMany()
                        .HasForeignKey("StockAccountId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("AIService.Models.StockAccount", b =>
                {
                    b.HasOne("AIService.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("AIService.Models.StockComment", b =>
                {
                    b.HasOne("AIService.Models.Stock", "Stock")
                        .WithMany()
                        .HasForeignKey("StockId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("AIService.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("AIService.Models.StockSearch", b =>
                {
                    b.HasOne("AIService.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("AIService.Models.Talk", b =>
                {
                    b.HasOne("AIService.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("AIService.Models.TradeHistory", b =>
                {
                    b.HasOne("AIService.Models.StockAccount", "StockAccount")
                        .WithMany()
                        .HasForeignKey("StockAccountId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("AIService.Models.WordsHistory", b =>
                {
                    b.HasOne("AIService.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
