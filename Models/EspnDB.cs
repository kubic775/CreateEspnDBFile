using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace CreateEspnDBFile.Models
{
    public partial class EspnDB : DbContext
    {
        public EspnDB()
        {
        }

        public EspnDB(DbContextOptions<EspnDB> options)
            : base(options)
        {
        }

        public virtual DbSet<Game> Games { get; set; }
        public virtual DbSet<GlobalParam> GlobalParams { get; set; }
        public virtual DbSet<LeagueTeam> LeagueTeams { get; set; }
        public virtual DbSet<Player> Players { get; set; }
        public virtual DbSet<YahooTeam> YahooTeams { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("DataSource=" + Path.Combine(Environment.CurrentDirectory, "espn.sqlite"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Game>(entity =>
            {
                entity.HasKey(e => e.Pk);

                entity.HasIndex(e => e.GameDate, "GameDateIndex");

                entity.Property(e => e.Pk)
                    .HasColumnType("int")
                    .ValueGeneratedNever();

                entity.Property(e => e.Ast).HasColumnType("double");

                entity.Property(e => e.Blk).HasColumnType("double");

                entity.Property(e => e.FgPer).HasColumnType("double");

                entity.Property(e => e.Fga).HasColumnType("double");

                entity.Property(e => e.Fgm).HasColumnType("double");

                entity.Property(e => e.FtPer).HasColumnType("double");

                entity.Property(e => e.Fta).HasColumnType("double");

                entity.Property(e => e.Ftm).HasColumnType("double");

                entity.Property(e => e.GameDate).HasColumnType("datetime");

                entity.Property(e => e.Gp).HasColumnType("int");

                entity.Property(e => e.Min).HasColumnType("double");

                entity.Property(e => e.Opp).HasColumnType("varchar(32)");

                entity.Property(e => e.Pf).HasColumnType("double");

                entity.Property(e => e.PlayerId).HasColumnType("int");

                entity.Property(e => e.Pts).HasColumnType("double");

                entity.Property(e => e.Reb).HasColumnType("double");

                entity.Property(e => e.Score).HasColumnType("double");

                entity.Property(e => e.Stl).HasColumnType("double");

                entity.Property(e => e.To).HasColumnType("double");

                entity.Property(e => e.TpPer).HasColumnType("double");

                entity.Property(e => e.Tpa).HasColumnType("double");

                entity.Property(e => e.Tpm).HasColumnType("double");
            });

            modelBuilder.Entity<GlobalParam>(entity =>
            {
                entity.HasKey(e => e.Pk);

                entity.Property(e => e.Pk)
                    .ValueGeneratedNever()
                    .HasColumnName("pk");

                entity.Property(e => e.LastUpdateTime)
                    .IsRequired()
                    .HasColumnType("datetime");
            });

            modelBuilder.Entity<LeagueTeam>(entity =>
            {
                entity.HasKey(e => e.Pk);

                entity.Property(e => e.Pk)
                    .HasColumnType("int")
                    .ValueGeneratedNever();

                entity.Property(e => e.Abbreviation).HasColumnType("nvarchar(32)");

                entity.Property(e => e.Name).HasColumnType("nvarchar(32)");

                entity.Property(e => e.TeamId)
                    .HasColumnType("int")
                    .HasColumnName("TeamID");
            });

            modelBuilder.Entity<Player>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnType("int")
                    .ValueGeneratedNever()
                    .HasColumnName("ID");

                entity.Property(e => e.Age).HasColumnType("int");

                entity.Property(e => e.LastUpdateTime).HasColumnType("datetime");

                entity.Property(e => e.Misc).HasColumnType("varchar(32)");

                entity.Property(e => e.Name).HasColumnType("varchar(32)");

                entity.Property(e => e.Team).HasColumnType("varchar(32)");
            });

            modelBuilder.Entity<YahooTeam>(entity =>
            {
                entity.HasKey(e => e.Pk);

                entity.HasIndex(e => e.Pk, "IX_YahooTeams_pk")
                    .IsUnique();

                entity.HasIndex(e => e.TeamId, "IX_YahooTeams_TeamId")
                    .IsUnique();

                entity.Property(e => e.Pk)
                    .ValueGeneratedNever()
                    .HasColumnName("pk");

                entity.Property(e => e.TeamName).IsRequired();
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
