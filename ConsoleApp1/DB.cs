namespace ConsoleApp1
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    public class DB : DbContext
    {
        //您的上下文已配置为从您的应用程序的配置文件(App.config 或 Web.config)
        //使用“DB”连接字符串。默认情况下，此连接字符串针对您的 LocalDb 实例上的
        //“ConsoleApp1.DB”数据库。
        // 
        //如果您想要针对其他数据库和/或数据库提供程序，请在应用程序配置文件中修改“DB”
        //连接字符串。
        public DB()
            : base("name=DB")
        {
            Database.SetInitializer(new DbInitialier());
        }

        //为您要在模型中包含的每种实体类型都添加 DbSet。有关配置和使用 Code First  模型
        //的详细信息，请参阅 http://go.microsoft.com/fwlink/?LinkId=390109。

        public virtual DbSet<User> Users { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public bool Sex { get; set; }
        public string Des { get; set; }
    }

    class DbInitialier : MigrateDatabaseToLatestVersion<DB, InitializerConfiguration> { }
    class InitializerConfiguration : DbMigrationsConfiguration<DB>
    {
        public InitializerConfiguration()
        {
            this.AutomaticMigrationsEnabled = true;
            this.AutomaticMigrationDataLossAllowed = true;
        }
        protected override void Seed(DB context)
        {
            var random = new Random();
            var users = new List<User>();
            for (int i = 0; i < 100; i++)
            {
                users.Add(new User() { Id = i + 1, Name = $"Name{i + 1}", Age = random.Next(1, 100), Sex = random.Next(0, 3) > 1 });
            }
            context.Users.AddOrUpdate(users.ToArray());
            context.SaveChanges();
            base.Seed(context);
        }
    }
}