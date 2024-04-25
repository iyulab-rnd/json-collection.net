using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonCollectionNet.Tests
{
    public class JsonCollectionNetWithDateTest
    {
        private readonly JsonElement users;

        public JsonCollectionNetWithDateTest()
        {
            var usersJson = @"[
                { ""id"": 1, ""name"": ""Alice"", ""CreatedAt"": ""2023-03-17T12:34:56Z"" },
                { ""id"": 2, ""name"": ""Bob"", ""CreatedAt"": ""2024-04-18T12:34:56Z"" },
                { ""id"": 3, ""name"": ""Charlie"", ""CreatedAt"": ""2024-04-24T12:34:56Z"" },
                { ""id"": 4, ""name"": ""David"", ""CreatedAt"": ""2024-04-15T12:34:56Z"" }
              ]";

            this.users = JsonSerializer.Deserialize<JsonElement>(usersJson);
        }

        [Fact]
        public void TestFilterByDate()
        {
            var collection = new JsonCollection(users);

            string testDate = "2024-04-16T00:00:00Z";
            DateTime testDateTime = DateTime.Parse(testDate).ToUniversalTime();
            string dateFilterJson = $@"{{ $match: {{ CreatedAt: {{ $gte: ""{testDateTime:yyyy-MM-ddTHH:mm:ssZ}"" }} }} }}";

            var results = collection.Aggregate(dateFilterJson);

            var expectedJson = new JsonArray
            {
                JsonNode.Parse(@"{ ""id"": 2, ""name"": ""Bob"", ""CreatedAt"": ""2024-04-18T12:34:56Z"" }"),
                JsonNode.Parse(@"{ ""id"": 3, ""name"": ""Charlie"", ""CreatedAt"": ""2024-04-24T12:34:56Z"" }")
            };

            Assert.Equal(results.GetRawText(), JsonDocument.Parse(expectedJson.ToJsonString()).RootElement.GetRawText());
        }

        [Fact]
        public void TestFilterAndCountRecentDocuments()
        {
            var collection = new JsonCollection(users);

            string dateFilterJson = $@"{{ $match: {{ CreatedAt: {{ $gte: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000) }} }} }}";

            var results = collection.Aggregate(dateFilterJson);

            Assert.True(results.ValueKind == JsonValueKind.Array);
        }

        [Fact]
        public void TestFilterAndCountByDateRange()
        {
            var collection = new JsonCollection(users);

            string dateRangeJson = @"
            [
                { $match: { CreatedAt: { $gte: new Date(""2023-01-01T00:00:00Z""), $lt: new Date(""2024-01-01T00:00:00Z"") } } },
                { $group: { _id: null, count: { $sum: 1 } } }
            ]";

            var results = collection.Aggregate(dateRangeJson);
            var expectedJson = new JsonArray
            {
                JsonNode.Parse(@"{ ""id"": 1, ""name"": ""Alice"", ""CreatedAt"": ""2023-03-17T12:34:56Z"" }")
            };

            Assert.Equal(results.GetRawText(), JsonDocument.Parse(expectedJson.ToJsonString()).RootElement.GetRawText());
        }

    }
}