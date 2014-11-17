using FluentNHibernate.Automapping;
using FluentNHibernate.Automapping.Alterations;
using OrmNomNom.Domain;

namespace MuSeed.Domain
{
    public class LogMappingOverride : IAutoMappingOverride<Log>
    {
        public void Override(AutoMapping<Log> mapping)
        {
            mapping.Map(x => x.LogLevel, "Level");
            mapping.IgnoreProperty(x => x.IgnoreMe);
            mapping.Id(x => x.Id);
        }
    }
}