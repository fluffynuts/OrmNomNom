using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Mapping;
using OrmNomNom.DB;
using PeanutButter.Utils;

namespace OrmNomNom.Domain
{
    public class NomContext: DbContext
    {
        public IDbSet<Log> Logs { get; set; }
        private static Semaphore _mappingLock = new Semaphore(1, 1);
        private static bool _mappingsDone = false;

        static NomContext()
        {
            Database.SetInitializer<NomContext>(null);  // so we can use our own migrations, ie FluentMigrator
        }

        public NomContext(string connectionString): base(connectionString)
        {
            
        }

        public NomContext(DbConnection connection)
            : base(connection, true)
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            using (new AutoLocker(_mappingLock))
            {
                if (AlreadyConfigured(modelBuilder))
                    return;
                modelBuilder.Entity<Log>().Property(l => l.LogLevel).HasColumnName(DataConstants.Tables.Log.Columns.LEVEL);
                modelBuilder.Entity<Log>().Ignore(l => l.IgnoreMe);
                modelBuilder.Entity<Log>().ToTable(DataConstants.Tables.Log.NAME); // otherwise Entity assumes the table is called "Logs"
                _mappingsDone = true;
            }
        }

        private static bool AlreadyConfigured(DbModelBuilder modelBuilder)
        {
            // this is VERY MUCH a hack to deal with what appears to be going on:
            //  it seems that model configuration is stored against the EF provider
            //  so when the tests in this project switch from SqlCE to Sqlite, model
            //  config isn't found and persistence fails. In production code, without switching
            //  contexts, you should just be able to use a static flag (like _mappingsDone)
            //  to ensure that your models are only configured once.

            // The code below delves into private properties on the modelBuilder and is not guaranteed to
            //  continue to be accurate with EF updates. It's just so that comparitive tests can
            //  be run with the two different engines (SqlCE & Sqlite)
            var existing = modelBuilder.Entity<Log>();
            var prop = existing.GetType().GetProperty("Configuration", BindingFlags.NonPublic | BindingFlags.Instance);
            var config = prop.GetValue(existing);
            var tableNameProp = config.GetType().GetProperty("TableName");
            var tableName = tableNameProp.GetValue(config);

            var alreadyConfigured = (tableName != null && _mappingsDone);
            return alreadyConfigured;
        }
    }
}
