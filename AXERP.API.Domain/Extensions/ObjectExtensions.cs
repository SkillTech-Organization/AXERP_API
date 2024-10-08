namespace AXERP.API.Domain.Extensions
{
    public static class ObjectExtensions
    {
        public static string GenerateHash(this IList<object> object_values)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(object_values);
            var jsonBytes = System.Text.Encoding.ASCII.GetBytes(json);
            var md5Bytes = System.Security.Cryptography.MD5.HashData(jsonBytes);
            var md5String = Convert.ToHexString(md5Bytes);
            return md5String;
        }
    }
}
