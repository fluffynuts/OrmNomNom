OrmNomNom
=========

Simple comparison between Entity and NHibernate with a FluentMigrator spin-up, using SQLCE and Sqlite for testing purposes
OrmNomNom notes:

1) When installing NHibernate, you probably want FluentNHibernate. Whilst
    FluentNHibernate depends on NHibernate, it depends on version 3.3.something
    or better, so nuget goes off and gets an old NHibernate. So install
    NHibernate FIRST and then FluentNHibernate

2) FluentNHibernate requires that you exlicitly exclude classes which are not entities

3) NHibernate entities must have VIRTUAL properties

4) NHibernate sessions and session builders MUST be disposed to release
    connections on the databases at hand

5) Side-note: SqlCe has a default policy on transaction disposal to ROLL BACK,
    not COMMIT, so you MUST commit transactions if you ever get into SqlCe
    land

6) If you are using SqlCe with Entity, make sure that the following entry has
    been added to your (app|web).config:
      <provider invariantName="System.Data.SQLite.EF6" type="System.Data.SQLite.EF6.SQLiteProviderServices, System.Data.SQLite.EF6" />
    the Nuget package *should* do it for you, but I've had it *not* do it for
    me on occasion

7) if you do everything fluently with NHibernate, you shouldn't have to diddle
    with the config

8) don't forget to reference system.data.sqlserverce & set to copy local

7) Entity packages: you need EF (duh) and the provider for your test db
    backend (SqlCe or Sqlite); ensure that you have the relevant EF provider
    confing in your (web|app).config (this config discovered through trial,
    error, weeping, gnashing of teeth, StackOverflow, more trial, more error
    and finally, a wild guess which worked:

  <system.data>
    <DbProviderFactories>
      <remove invariant="System.Data.SQLite" />
      <add name="SQLite Data Provider" invariant="System.Data.SQLite" description=".Net Framework Data Provider for SQLite" type="System.Data.SQLite.SQLiteFactory, System.Data.SQLite" />
      <remove invariant="System.Data.SQLite.EF6" />
      <add name="SQLite Data Provider (Entity Framework 6)" invariant="System.Data.SQLite.EF6" description=".Net Framework Data Provider for SQLite (Entity Framework 6)" type="System.Data.SQLite.EF6.SQLiteProviderFactory, System.Data.SQLite.EF6" />
    </DbProviderFactories>
  </system.data>
  <entityFramework>
    <defaultConnectionFactory type="System.Data.Entity.Infrastructure.SqlCeConnectionFactory, EntityFramework">
      <parameters>
        <parameter value="System.Data.SqlServerCe.4.0" />
      </parameters>
    </defaultConnectionFactory>
    <providers>
      <provider invariantName="System.Data.SqlClient" type="System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer" />
      <provider invariantName="System.Data.SQLite" type="System.Data.SQLite.EF6.SQLiteProviderServices, System.Data.SQLite.EF6" />
      <provider invariantName="System.Data.SQLite.EF6" type="System.Data.SQLite.EF6.SQLiteProviderServices, System.Data.SQLite.EF6" />
      <provider invariantName="System.Data.SqlServerCe.4.0" type="System.Data.Entity.SqlServerCompact.SqlCeProviderServices, EntityFramework.SqlServerCompact" />
    </providers>
  </entityFramework>


    provider packages:
    System.Data.Sqlite
    EntityFramework.SqlServerCompact

8) Note that I provide a DbConnection for contexts within my tests because
otherwise entity gets confused about which provider to use, since I have
several defined in the config file. You may not have to follow this approach
if you only use one provider type

9) If you're using Sqlite for backing (file or in-memory) against Entity, you will have to
    store integer values as LONGs since otherwise retrieval results in a failure as the
    stored values are pulled up as int64 and then can't be applied to your int32 properties.
