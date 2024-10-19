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
    }
}
