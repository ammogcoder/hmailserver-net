using System;
using System.Collections.Generic;

namespace hMailServer.Repository.RelationalShared
{
    public class TypeColumnMapping
    {
        public Type Type { get; set; }
        public string TableName { get; set; }
        public Dictionary<string, string> FieldNameByColumnName { get; set; }
    }
}
