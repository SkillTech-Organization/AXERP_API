using AXERP.API.Domain.Attributes;
using AXERP.API.Domain.Models;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace AXERP.API.Persistence.Utils
{
    public static class RowTypeExtensions
    {
        public static List<ColumnData> GetColumnDatas(this Type t, List<string>? columnList = null)
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

                    int? order = null;
                    int? minW = null;
                    int? maxW = null;

                    var gridPropsAttribute = property.GetCustomAttribute<GridPropsAttribute>(true);
                    if (gridPropsAttribute != null)
                    {
                        order = gridPropsAttribute.Order == 0 ? null : gridPropsAttribute.Order;
                        minW = gridPropsAttribute.MinWidth == 0 ? null : gridPropsAttribute.MinWidth;
                        maxW = gridPropsAttribute.MaxWidth == 0 ? null : gridPropsAttribute.MaxWidth;
                    }

                    columns.Add(new ColumnData
                    {
                        Name = property.Name,
                        Title = jsonName,
                        Type = property.PropertyType.ToString(),
                        Order = order,
                        MinWidth = minW,
                        MaxWidth = maxW
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
                    if (jsonAttribute != null && !string.IsNullOrWhiteSpace(jsonAttribute.PropertyName) && column.ToLower() == jsonAttribute.PropertyName.ToLower())
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
        /// <param name="excludeKey"></param>
        /// <returns></returns>
        public static string GetColumnNamesAsSqlColumnList(this Type t, List<string>? columnList, string? selectAlias, bool excludeKey = false)
        {
            var columns = new List<string>();

            var key = t.GetKeyColumnName();

            foreach (var property in t.GetProperties())
            {
                if (columnList == null || !columnList.Any() || columnList.Contains(property.Name))
                {
                    if (excludeKey && key == property.Name)
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(selectAlias))
                    {
                        columns.Add($"{selectAlias}.[{property.Name}]");
                    }
                    else
                    {
                        columns.Add($"[{property.Name}]");
                    }
                }
            }

            return string.Join(",", columns);
        }

        /// <summary>
        /// Example:
        /// object { FieldA, FieldB } -> "selectAlias.FieldA, selectAlias.FieldB"
        /// </summary>
        /// <param name="t"></param>
        /// <param name="columnList"></param>
        /// <param name="selectAlias">optional alias for select, if not provided, column names will be listed in their own eg. FieldA, FieldB, ...</param>
        /// <param name="excludeKey"></param>
        /// <returns></returns>
        public static string GetColumnNamesAsSqlParamList(this Type t, List<string>? columnList, bool excludeKey = false)
        {
            var columns = new List<string>();

            var key = t.GetKeyColumnName();

            foreach (var property in t.GetProperties())
            {
                if (columnList == null || !columnList.Any() || columnList.Contains(property.Name))
                {
                    if (excludeKey && key == property.Name)
                    {
                        continue;
                    }

                    columns.Add($"@{property.Name}");
                }
            }

            return string.Join(",", columns);
        }

        /// <summary>
        /// Example:
        /// object { FieldA, FieldB } -> "selectAlias.FieldA, selectAlias.FieldB"
        /// </summary>
        /// <param name="t"></param>
        /// <param name="columnList"></param>
        /// <param name="selectAlias">optional alias for select, if not provided, column names will be listed in their own eg. FieldA, FieldB, ...</param>
        /// <param name="excludeKey"></param>
        /// <returns></returns>
        public static string GetColumnNamesAsSqlAssignmentList(this Type t, List<string>? columnList, bool excludeKey = false)
        {
            var columns = new List<string>();

            var key = t.GetKeyColumnName();

            foreach (var property in t.GetProperties())
            {
                if (columnList == null || !columnList.Any() || columnList.Contains(property.Name))
                {
                    if (excludeKey && key == property.Name)
                    {
                        continue;
                    }

                    columns.Add($"[{property.Name}] = @{property.Name}");
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
                                columns.Add($"{selectAlias}.[{property.Name}] = {parameterName}");
                            }
                            else
                            {
                                columns.Add($"[{property.Name}] = {parameterName}");
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
                                columns.Add($"{selectAlias}.[{property.Name}] = {parameterName}");
                            }
                            else
                            {
                                columns.Add($"[{property.Name}] = {parameterName}");
                            }
                        }
                    }
                    else// if (parameterType == typeof(string))
                    {
                        if (!string.IsNullOrWhiteSpace(selectAlias))
                        {
                            columns.Add($"{selectAlias}.[{property.Name}] LIKE {parameterName}");
                        }
                        else
                        {
                            columns.Add($"[{property.Name}] LIKE {parameterName}");
                        }
                    }
                }
            }

            return string.Join(" or ", columns);
        }

        public static string GetSqlMultiSearchExpressionForSpecificColumns(this Type t, List<string>? columnList, string? baseParameterName, List<Type> parameterTypes, string? selectAlias)
        {
            var columns = new List<string>();

            int parameterIdx = 0;

            foreach (var property in t.GetProperties())
            {
                if (columnList == null || !columnList.Any() || columnList.Contains(property.Name))
                {
                    var parameterType = parameterTypes[parameterIdx];

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
                                columns.Add($"{selectAlias}.[{property.Name}] = {baseParameterName}{parameterIdx}");
                            }
                            else
                            {
                                columns.Add($"[{property.Name}] = {baseParameterName}{parameterIdx}");
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
                                columns.Add($"{selectAlias}.[{property.Name}] = {baseParameterName}{parameterIdx}");
                            }
                            else
                            {
                                columns.Add($"[{property.Name}] = {baseParameterName}{parameterIdx}");
                            }
                        }
                    }
                    else// if (parameterType == typeof(string))
                    {
                        if (!string.IsNullOrWhiteSpace(selectAlias))
                        {
                            columns.Add($"{selectAlias}.[{property.Name}] LIKE {baseParameterName}{parameterIdx}");
                        }
                        else
                        {
                            columns.Add($"[{property.Name}] LIKE {baseParameterName}{parameterIdx}");
                        }
                    }

                    parameterIdx++;
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

        public static bool CheckSqlModifier(this Type t, string propertyName, SqlModifiers sqlModifier)
        {
            foreach (var property in t.GetProperties())
            {
                var jsonAttributes = property.GetCustomAttributes<SqlModifierAttribute>(true);
                foreach (var attribute in jsonAttributes)
                {
                    if (attribute.Modifier == sqlModifier)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static string GetTableName(this Type t)
        {
            var tableAttrib = t.GetCustomAttribute<TableAttribute>();
            return tableAttrib?.Name ?? t.Name + "s";
        }

        public static string GetKeyColumnName(this Type t)
        {
            PropertyInfo[] properties = t.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                object[] keyAttributes = property.GetCustomAttributes(typeof(KeyAttribute), true);

                if (keyAttributes != null && keyAttributes.Length > 0)
                {
                    object[] columnAttributes = property.GetCustomAttributes(typeof(ColumnAttribute), true);

                    if (columnAttributes != null && columnAttributes.Length > 0)
                    {
                        ColumnAttribute columnAttribute = (ColumnAttribute)columnAttributes[0];
                        return columnAttribute.Name;
                    }
                    else
                    {
                        return property.Name;
                    }
                }
            }

            return null;
        }
    }
}
