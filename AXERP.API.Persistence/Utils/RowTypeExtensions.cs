using AXERP.API.Persistence.ServiceContracts.Models;
using Newtonsoft.Json;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

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

        public static List<string> GetColumnNames(this Type t, List<string>? columnList)
        {
            var columns = new List<string>();

            foreach (var property in t.GetProperties())
            {
                if (columnList == null || !columnList.Any() || columnList.Contains(property.Name))
                {
                    var jsonName = property.Name;

                    columns.Add(property.Name);
                }
            }

            return columns;
        }

        public static string FilterValidColumn(this Type t, string column, bool includeJsonAttributes = false)
        {
            if (string.IsNullOrWhiteSpace(column))
            {
                return null;
            }

            foreach (var property in t.GetProperties())
            {
                if (column == property.Name)
                {
                    return property.Name;
                }

                if (includeJsonAttributes)
                {
                    var jsonAttribute = property.GetCustomAttribute<JsonPropertyAttribute>(true);
                    if (jsonAttribute != null && !string.IsNullOrWhiteSpace(jsonAttribute.PropertyName) && column == jsonAttribute.PropertyName)
                    {
                        return property.Name;
                    }
                }
            }

            return null;
        }

        public static List<string> FilterValidColumns(this Type t, List<string> columnList, bool includeJsonAttributes = false)
        {
            var columns = new List<string>();

            foreach (var property in t.GetProperties())
            {
                if (columnList != null && columnList.Any() && columnList.Contains(property.Name))
                {
                    columns.Add(property.Name);
                }

                if (columnList != null && columnList.Any() && includeJsonAttributes)
                {
                    var jsonAttribute = property.GetCustomAttribute<JsonPropertyAttribute>(true);
                    if (jsonAttribute != null && columnList.Contains(jsonAttribute.PropertyName ?? ""))
                    {
                        columns.Add(property.Name);
                    }
                }
            }

            return columns;
        }

        /// <summary>
        /// Example:
        /// object { FieldA, FieldB } -> "selectAlias.FieldA, selectAlias.FieldB"
        /// </summary>
        /// <param name="t"></param>
        /// <param name="columnList"></param>
        /// <param name="selectAlias">optional alias for select, if not provided, column names will be listed in their own eg. FieldA, FieldB, ...</param>
        /// <returns></returns>
        public static string GetColumnNamesAsSqlColumnList(this Type t, List<string>? columnList, string? selectAlias)
        {
            var columns = new List<string>();

            foreach (var property in t.GetProperties())
            {
                if (columnList == null || !columnList.Any() || columnList.Contains(property.Name))
                {
                    var jsonName = property.Name;

                    if (!string.IsNullOrWhiteSpace(selectAlias))
                    {
                        columns.Add($"{selectAlias}.{property.Name}");
                    }
                    else
                    {
                        columns.Add(property.Name);
                    }
                }
            }

            return string.Join(",", columns);
        }

        public static string GetSqlSearchExpressionForColumns(this Type t, List<string>? columnList, string? parameterName, Type parameterType, string? selectAlias)
        {
            var columns = new List<string>();

            foreach (var property in t.GetProperties())
            {
                if (columnList == null || !columnList.Any() || columnList.Contains(property.Name))
                {
                    if (property.PropertyType == typeof(float) ||
                        property.PropertyType == typeof(double) ||
                        property.PropertyType == typeof(int) ||
                        property.PropertyType == typeof(decimal) ||
                        property.PropertyType == typeof(float?) ||
                        property.PropertyType == typeof(double?) ||
                        property.PropertyType == typeof(int?) ||
                        property.PropertyType == typeof(decimal?))
                    {
                        if (parameterType == typeof(double))
                        {
                            if (!string.IsNullOrWhiteSpace(selectAlias))
                            {
                                columns.Add($"{selectAlias}.{property.Name} = {parameterName}");
                            }
                            else
                            {
                                columns.Add($"{property.Name} = {parameterName}");
                            }
                        }
                    }
                    else if (property.PropertyType == typeof(DateTime) ||
                        property.PropertyType == typeof(DateOnly) ||
                        property.PropertyType == typeof(DateTimeOffset) ||
                        property.PropertyType == typeof(DateTime?) ||
                        property.PropertyType == typeof(DateOnly?) ||
                        property.PropertyType == typeof(DateTimeOffset?))
                    {
                        if (parameterType == typeof(DateTime))
                        {
                            if (!string.IsNullOrWhiteSpace(selectAlias))
                            {
                                columns.Add($"{selectAlias}.{property.Name} = {parameterName}");
                            }
                            else
                            {
                                columns.Add($"{property.Name} = {parameterName}");
                            }
                        }
                    }
                    else// if (parameterType == typeof(string))
                    {
                        if (!string.IsNullOrWhiteSpace(selectAlias))
                        {
                            columns.Add($"{selectAlias}.{property.Name} LIKE {parameterName}");
                        }
                        else
                        {
                            columns.Add($"{property.Name} LIKE {parameterName}");
                        }
                    }
                }
            }

            return string.Join(" or ", columns);
        }

        public static Type GetValueType(this string value)
        {
            if (DateTime.TryParse(value, out var _))
            {
                return typeof(DateTime);
            }
            if (double.TryParse(value.Replace(".", ",").Replace(" ", ""), out var _))
            {
                return typeof(double);
            }
            return typeof(string);
        }
    }
}
