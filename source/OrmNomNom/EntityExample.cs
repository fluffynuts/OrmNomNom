using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.Processors.SQLite;
using FluentMigrator.Runner.Processors.SqlServer;
using FluentNHibernate.Cfg;
using NHibernate;
using NUnit.Framework;
using OrmNomNom.DB.Migrations;
using PeanutButter.FluentMigrator;
using PeanutButter.TestUtils.Generic;
using PeanutButter.Utils;
using OrmNomNom.Domain;

namespace OrmNomNom
{
    [TestFixture]
    public class EntityExample
    {
        
        [SetUp]
        public void Setup()
        {
        }
        [TearDown]
        public void TearDown()
        {
            // required for proper handling of shared SQLite databases
            // System.Data.Sqlite stupidly does resource clearing in the Finalizer instead of the
            //  Dispose() method. So if you don't force finalizers, you will find that (randomly)
            //  a few (say 2-6 / 100) of your tests will fail with errors that seem to report that
            //  your migrated tables don't exist.
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        [Test]
        public void AAA_PrimerMethod_EnsuresThatTheTimeWastedToSpinUpTheFirstTest_IsNotAttributedToAnActualTest()
        {
            //---------------Set up test pack-------------------
            
            //---------------Assert Precondition----------------
            try
            {
                // run all tests exactly once so that timings for spinup isn't scored against the tests themselves
                // much of the time taking in the first test run can be attributed to dynamic assembly loading
                //  :: the time taken to do this is machine-dependent
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
                var connection = new SQLiteConnection(connectionString);

                MigrateToLatestFor<SqliteProcessorFactory>(connectionString);

                //---------------Assert Precondition----------------
                AssertLogTableExistsOn(connection);
                var ctx1 = disposer.Add(new NomContext(connection));
                CollectionAssert.IsEmpty(ctx1.Logs.ToList());

                //---------------Execute Test ----------------------
                var log = CreateLogWithData();

                ctx1.Logs.Add(log);
                ctx1.SaveChanges();
                
                WriteLog("Saved new log with id: " + log.Id);

                //---------------Test Result -----------------------

                // TODO: pull log back up again
                var ctx2 = disposer.Add(new NomContext(connection));
                CollectionAssert.IsNotEmpty(ctx2.Logs.ToList());
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
                    MigrateToLatestFor<SqliteProcessorFactory>(db.ConnectionString);

                    var connection = new SQLiteConnection(db.ConnectionString);

                    MigrateToLatestFor<SqliteProcessorFactory>(db.ConnectionString);

                    //---------------Assert Precondition----------------
                    AssertLogTableExistsOn(connection);
                    var ctx1 = disposer.Add(new NomContext(connection));
                    CollectionAssert.IsEmpty(ctx1.Logs.ToList());

                    //---------------Execute Test ----------------------
                    var log = CreateLogWithData();

                    ctx1.Logs.Add(log);
                    ctx1.SaveChanges();

                    WriteLog("Saved new log with id: " + log.Id);

                    //---------------Test Result -----------------------

                    // TODO: pull log back up again
                    var ctx2 = disposer.Add(new NomContext(connection));
                    CollectionAssert.IsNotEmpty(ctx2.Logs.ToList());

                }
            }
        }

        private static void AssertLogTableExistsOn(DbConnection connection)
        {
            Assert.DoesNotThrow(() =>
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        if (connection.State != ConnectionState.Open)
                            connection.Open();
                        cmd.CommandText = "select * from Log";
                        using (var rdr = cmd.ExecuteReader())
                        {
                            rdr.Read();
                        }
                    }
                });
        }


        [TestCaseSource("Cases")]
        public void BBB_GetPage_UsingSqlCETempDB_GivenEmptyParameters_ShouldReturnAllLogs(int i)
        {
            //---------------Set up test pack-------------------
            using (var disposer = new AutoDisposer())
            {
                var db = disposer.Add(new TempDBSqlCe());
                MigrateToLatestFor<SqlServerCeProcessorFactory>(db.ConnectionString);

                //---------------Assert Precondition----------------
                AssertLogTableExistsOn(db.CreateConnection());

                //---------------Execute Test ----------------------
                var ctx1 = disposer.Add(new NomContext(db.CreateConnection()));
                CollectionAssert.IsEmpty(ctx1.Logs.ToList());
                var log = CreateLogWithData();
                ctx1.Logs.Add(log);
                ctx1.SaveChanges();

                Assert.AreNotEqual(0, log.Id);
                WriteLog("Saved new log with id: " + log.Id);

                //---------------Test Result -----------------------

                var ctx2 = disposer.Add(new NomContext(db.CreateConnection()));
                CollectionAssert.IsNotEmpty(ctx2.Logs.ToList());
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

    }
}
