using Newtonsoft.Json;
using System.Reflection;

namespace AXERP.API.Domain.Util
{
    public static class SheetHelperMethods
    {
        /// <summary>
        /// Filtering range by EOD
        /// </summary>
        /// <param name="sheet_rows"></param>
        /// <returns></returns>
        public static List<IList<object>> UntilEndOfData(List<IList<object>> sheet_rows, out int eodRowIndex)
        {
            eodRowIndex = sheet_rows.FindIndex(row =>
            {
                return row.Any(x => x != null &&
                       (x.ToString() ?? string.Empty)
                            .ToLower()
                            .Contains(EnvironmentHelper.TryGetOptionalParameter("SheetEndOfDataMarker") ?? "#end"));
            });

            sheet_rows = sheet_rows.GetRange(0, eodRowIndex);

            return sheet_rows;
        }

        public static Dictionary<string, int> GetFieldNamesWithOrder<T>(IList<object> headers)
        {
            var field_names = new Dictionary<string, int>();
            foreach (var property in typeof(T).GetProperties())
            {
                var jsonAttribute = property.GetCustomAttribute<JsonPropertyAttribute>(true);
                if (jsonAttribute != null)
                {
                    var propertyName = jsonAttribute.PropertyName;
                    field_names[property.Name] = headers.IndexOf(propertyName ?? property.Name);
                }
            }
            return field_names;
        }

        public static int GetFieldNameIndex<T>(string fieldName)
        {
            var prop = typeof(T).GetProperties().First(x => x.Name == fieldName);
            return typeof(T).GetProperties().ToList().IndexOf(prop);
        }

        /// <summary>
        /// 1 -> A
        /// 2 -> B
        /// ...
        /// ? -> GV
        /// ...
        /// </summary>
        /// <param name="columnNumber"></param>
        /// <returns></returns>
        public static string GetExcelColumnName(int columnNumber)
        {
            string columnName = "";

            while (columnNumber > 0)
            {
                int modulo = (columnNumber - 1) % 26;
                columnName = Convert.ToChar('A' + modulo) + columnName;
                columnNumber = (columnNumber - modulo) / 26;
            }

            return columnName;
        }
    }
}
