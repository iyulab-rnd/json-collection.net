using System.Text.Json;

namespace JsonCollectionNet
{
    public static class JsonElementExtensions
    {
        public static bool TryGetPropertyIgnoreCase(this JsonElement element, string propertyName, out JsonElement value)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }

}
