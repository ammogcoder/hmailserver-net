using System;
using System.Collections.Generic;
using System.Reflection;
using Dapper;

namespace hMailServer.Repository.RelationalShared
{
    public class CustomSimpleCrudResolver : SimpleCRUD.ITableNameResolver, SimpleCRUD.IColumnNameResolver
    {
        private readonly Dictionary<string, string> _tableNameByType = new Dictionary<string, string>();
        private readonly Dictionary<string, Dictionary<string, string>> _columnNameByFieldNameByTableName = new Dictionary<string, Dictionary<string, string>>();

        public CustomSimpleCrudResolver(List<TypeColumnMapping> mappings)
        {
            foreach (var mapping in mappings)
            {
                _tableNameByType[mapping.Type.Name] = mapping.TableName;
            }

            foreach (var mapping in mappings)
            {
                var columnNameByFieldName = new Dictionary<string, string>();

                foreach (var column in mapping.FieldNameByColumnName)
                    columnNameByFieldName[column.Value] = column.Key;

                _columnNameByFieldNameByTableName[mapping.Type.Name] = columnNameByFieldName;
            }
        }

        public string ResolveTableName(Type type)
        {
            return _tableNameByType[type.Name];
        }

        public string ResolveColumnName(PropertyInfo propertyInfo)
        {
            return _columnNameByFieldNameByTableName[propertyInfo.DeclaringType.Name][propertyInfo.Name];
        }
    }
}