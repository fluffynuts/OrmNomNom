using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentMigrator;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Announcers;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.Processors.SQLite;
using FluentMigrator.Runner.Processors.SqlServer;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using OrmNomNom.DB.Migrations;
using OrmNomNom.Domain;
using PeanutButter.FluentMigrator;
using PeanutButter.TestUtils.Generic;
using PeanutButter.Utils;

namespace OrmNomNom
{
    [TestFixture]
    public class NHibernateExample
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            System.Diagnostics.Debug.WriteLine("--- spinning up ---");
        }
        
        [SetUp]
        public void Setup()
        {
        }
        [TearDown]
        public void TearDown()
        {
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            System.Diagnostics.Debug.WriteLine("--- tearing down ---");
        }

        [Test]
        public void AAA_PrimerMethod_EnsuresThatTheTimeWastedToSpinUpTheFirstTest_IsNotAttributedToAnActualTest()
        {
            //---------------Set up test pack-------------------
            
            //---------------Assert Precondition----------------
            try
            {
                BBB_GetPage_UsingSqlCETempDB_GivenEmptyParameters_ShouldReturnAllLogs(0);
                CCC_GetPage_UsingSqliteActual_GivenEmptyParameters_ShouldReturnAllLogs(0);
                DDD_GetPage_UsingSqliteInMemory_GivenEmptyParameters_ShouldReturnAllLogs(0);
            }
            catch
            {
            }

            //---------------Execute Test ----------------------

            //---------------Test Result -----------------------
        }

        private IEnumerable<int> Cases()
        {
            return Enumerable.Range(1, 100);
        }


        [TestCaseSource("Cases")]
        public void DDD_GetPage_UsingSqliteInMemory_GivenEmptyParameters_ShouldReturnAllLogs(int i)
        {
            using (var disposer = new AutoDisposer())
            {
                //---------------Set up test pack-------------------
                var connectionString = "FullUri=file:foodb" + i + "?mode=memory&cache=shared";
                Debug.WriteLine("connectionString: " + connectionString);

                var sessionFactory = disposer.Add(CreateSessionBuilderFactoryForSqliteWith(connectionString));
                MigrateToLatestFor<SqliteProcessorFactory>(connectionString);

                var session = disposer.Add(sessionFactory.OpenSession());
                var tran = disposer.Add(session.BeginTransaction());
                //---------------Assert Precondition----------------

                //---------------Execute Test ----------------------
                var log = CreateLogWithData();
                session.Save(log);
                tran.Commit();
                WriteLog("Saved new log with id: " + log.Id);

                //---------------Test Result -----------------------
                var anotherSession = disposer.Add(sessionFactory.OpenSession());
                var retr = anotherSession.Query<Log>().ToList();
                CollectionAssert.IsNotEmpty(retr);
            }
        }


        [TestCaseSource("Cases")]
        public void CCC_GetPage_UsingSqliteActual_GivenEmptyParameters_ShouldReturnAllLogs(int i)
        {
            using (var disposer = new AutoDisposer())
            {
                //---------------Set up test pack-------------------
                var db = disposer.Add(new TempDBSqlite());
                {
                    var migrationsRunner = new DBMigrationsRunner<SqliteProcessorFactory>(typeof(Migration_201411031716_CreateLogTable).Assembly, db.ConnectionString, Console.WriteLine);
                    migrationsRunner.MigrateToLatest();

                    var sessionFactory = disposer.Add(CreateSessionBuilderFactoryForSqliteWith(db.ConnectionString));
                    var session = disposer.Add(sessionFactory.OpenSession());
                    {
                        var tran = disposer.Add(session.BeginTransaction());
                        {
                            //---------------Assert Precondition----------------

                            //---------------Execute Test ----------------------
                            var log = CreateLogWithData();
                            session.Save(log);
                            tran.Commit();
                            WriteLog("Saved new log with id: " + log.Id);
                        }
                    }

                    //---------------Test Result -----------------------
                    var anotherSession = disposer.Add(sessionFactory.OpenSession());
                    {
                        var retr = anotherSession.Query<Log>().ToList();
                        CollectionAssert.IsNotEmpty(retr);
                    }
                }
            }
        }


        [TestCaseSource("Cases")]
        public void BBB_GetPage_UsingSqlCETempDB_GivenEmptyParameters_ShouldReturnAllLogs(int i)
        {
            //---------------Set up test pack-------------------
            using (var disposer = new AutoDisposer())
            {
                var db = disposer.Add(new TempDBSqlCe());
                MigrateToLatestFor<SqlServerCeProcessorFactory>(db.ConnectionString);

                var sessionFactory = disposer.Add(CreateSessionBuilderForSqlCeWith(db.ConnectionString));    // important! if the session builder is not
                                                                            // disposed then some connection to the
                                                                            // database is maintained (probably opportunistically
                                                                            // for another session) and you can't delete the tempdb
                var session = disposer.Add(sessionFactory.OpenSession());
                var tran = disposer.Add(session.BeginTransaction());
                //---------------Assert Precondition----------------

                //---------------Execute Test ----------------------
                var log = CreateLogWithData();
                session.Save(log);
                tran.Commit(); // important: sqlce has a default policy to rollback if the transaction is not explicitly committed
                WriteLog("Saved new log with id: " + log.Id);

                //---------------Test Result -----------------------

                var anotherSession = disposer.Add(sessionFactory.OpenSession());
                var retr = anotherSession.Query<Log>().ToList();
                CollectionAssert.IsNotEmpty(retr);

            }
        }

        private static Log CreateLogWithData()
        {
            return new Log()
                   {
                       Date = DateTime.Now,
                       Exception = "Some Exception, yo",
                       LogLevel = "WARN",
                       Logger = "Default",
                       Message = "Trippin' ballz",
                       Thread = "1234"
                   };
        }

        private void WriteLog(string s)
        {
            Console.WriteLine(s);
        }

        private readonly Semaphore _migrationLock = new Semaphore(1, 1);
        private void MigrateToLatestFor<TProcessorFactory>(string withConnectionString) where TProcessorFactory : MigrationProcessorFactory, new()
        {
            using (new AutoLocker(_migrationLock))
            {
                var runner = new DBMigrationsRunner<TProcessorFactory>(typeof(Migration_201411031716_CreateLogTable).Assembly, withConnectionString);
                runner.MigrateToLatest();
            }
        }

        private static ISessionFactory CreateSessionBuilderFactoryForSqliteWith(string connectionString)
        {
            return Fluently.Configure().Database(SQLiteConfiguration.Standard
                                                                    .ConnectionString(connectionString)
                                                     //.InMemory() // DON'T DO THIS: the shared in-memory database is forgotten
                                                                    .ShowSql())
                           .Mappings(m => m.AutoMappings.Add(AutoMap.AssemblyOf<Log>()
                                                                    .UseOverridesFromAssemblyOf<Log>()
                                                                    .Where(t => t != typeof(SomeNonEntity))))
                           .BuildSessionFactory();
        }

        private static ISessionFactory CreateSessionBuilderForSqlCeWith(string connectionString)
        {
            return Fluently.Configure().Database(MsSqlCeConfiguration.MsSqlCe40
                                                                     .ConnectionString(connectionString)
                                                                     .ShowSql())
                           .Mappings(m => m.AutoMappings.Add(AutoMap.AssemblyOf<Log>()
                                                                    .UseOverridesFromAssemblyOf<Log>()
                                                                    .Where(t => t != typeof(SomeNonEntity))))
                           .BuildSessionFactory();
        }
    }

}
