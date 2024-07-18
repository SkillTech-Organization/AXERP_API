using AXERP.API.Persistence.ServiceContracts.Models;
using Newtonsoft.Json;
using System.Reflection;

namespace AXERP.API.Persistence.Utils
{
    public static class RowTypeExtensions
    {
        public static List<ColumnData> GetColumnDatas(this Type t, List<string> columnList)
        {
            var columns = new List<ColumnData>();

            foreach (var property in t.GetProperties())
            {
                if (columnList == null || !columnList.Any() || columnList.Contains(property.Name))
                {
                    var jsonName = property.Name;

                    var jsonAttribute = property.GetCustomAttribute<JsonPropertyAttribute>(true);
                    if (jsonAttribute != null && !string.IsNullOrWhiteSpace(jsonAttribute.PropertyName))
                    {
                        jsonName = jsonAttribute.PropertyName;
                    }

                    columns.Add(new ColumnData
                    {
                        Name = property.Name,
                        Title = jsonName,
                        Type = property.PropertyType.ToString()
                    });
                }
            }

            return columns;
        }
    }
}
