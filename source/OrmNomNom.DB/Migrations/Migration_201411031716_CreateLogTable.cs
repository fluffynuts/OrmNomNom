using FluentMigrator;

namespace OrmNomNom.DB.Migrations
{
    [Migration(201411031716)]
    public class Migration_201411031716_CreateLogTable: ForwardOnlyMigration
    {
        public override void Up()
        {
            Create.Table(DataConstants.Tables.Log.NAME)
                    .WithColumn(DataConstants.Tables.Log.Columns.ID)
                        .AsInt64()      // NB: Sqlite ints are stored as int64 (esp via ADO) so you MUST use 64-bit ints otherwise you'll get read errors converting from int64 to int32
                        .PrimaryKey()
                        .Identity()
                    .WithColumn(DataConstants.Tables.Log.Columns.DATE)
                        .AsDateTime()
                        .NotNullable()
                    .WithColumn(DataConstants.Tables.Log.Columns.THREAD)
                        .AsString(255)
                        .NotNullable()
                    .WithColumn(DataConstants.Tables.Log.Columns.LEVEL)
                        .AsString(50)
                        .NotNullable()
                    .WithColumn(DataConstants.Tables.Log.Columns.LOGGER)
                        .AsString(255)
                        .NotNullable()
                    .WithColumn(DataConstants.Tables.Log.Columns.MESSAGE)
                        .AsString(4000)
                        .NotNullable()
                    .WithColumn(DataConstants.Tables.Log.Columns.EXCEPTION)
                        .AsString(2000)
                        .Nullable();

        }
    }
}
