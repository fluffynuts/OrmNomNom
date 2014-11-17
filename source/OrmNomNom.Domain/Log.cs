using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrmNomNom.Domain
{
    public class Log
    {
    	public virtual long Id { get; set; }  // NB: Sqlite ints are stored as int64 (esp via ADO) so you MUST use 64-bit ints otherwise you'll get read errors converting from int64 to int32
    	public virtual DateTime Date { get; set; }
    	public virtual string Thread { get; set; }
    	public virtual string LogLevel { get; set; }
    	public virtual string Logger { get; set; }
    	public virtual string Message { get; set; }
    	public virtual string Exception { get; set; }
        public virtual string IgnoreMe { get; set; }
    }
}
