using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonCollectionNet.Tests
{
    public class JsonCollectionNetNestedTests
    {
        private readonly JsonElement users;

        public JsonCollectionNetNestedTests()
        {
            var nestedUsersJson = @"[
                { ""id"": 1, ""name"": ""Alice"", ""age"": 30, ""contacts"": { ""email"": ""alice@example.com"", ""phone"": ""123-456-7890"" } },
                { ""id"": 2, ""name"": ""Bob"", ""age"": 25, ""contacts"": { ""email"": ""bob@example.com"", ""phone"": ""987-654-3210"" } },
                { ""id"": 3, ""name"": ""Charlie"", ""age"": 35, ""contacts"": { ""email"": ""charlie@example.com"", ""phone"": ""456-789-0123"" } },
                { ""id"": 4, ""name"": ""David"", ""age"": 40, ""contacts"": { ""email"": ""david@example.com"", ""phone"": ""321-654-9870"" } }
            ]";

            this.users = JsonSerializer.Deserialize<JsonElement>(nestedUsersJson);
        }


        [Fact]
        public void TestFilterUsersByEmail()
        {
            var aggregate = @"{ $match: { ""contacts.email"": { $eq: ""alice@example.com"" } } }";
            var collection = new JsonCollection(users);
            var results = collection.Aggregate(aggregate);

            var goles = new JsonArray
            {
                JsonNode.Parse(@"{ ""id"": 1, ""name"": ""Alice"", ""age"": 30, ""contacts"": { ""email"": ""alice@example.com"", ""phone"": ""123-456-7890"" } }")
            };

            Assert.Equal(results.GetRawText(), JsonDocument.Parse(goles.ToJsonString()).RootElement.GetRawText());
        }

        [Fact]
        public void TestGroupUsersByPhoneNumber()
        {
            var aggregate = @"{ $group: { _id: ""$contacts.phone"", count: { $sum: 1 } } }";
            var collection = new JsonCollection(users);
            var results = collection.Aggregate(aggregate);

            var goles = new JsonArray
            {
                JsonNode.Parse(@"{ ""_id"": ""123-456-7890"", ""count"": 1 }"),
                JsonNode.Parse(@"{ ""_id"": ""987-654-3210"", ""count"": 1 }"),
                JsonNode.Parse(@"{ ""_id"": ""456-789-0123"", ""count"": 1 }"),
                JsonNode.Parse(@"{ ""_id"": ""321-654-9870"", ""count"": 1 }")
            };

            foreach (var exp in goles)
            {
                Assert.Contains(exp.ToJsonString(), results.EnumerateArray().Select(x => x.GetRawText()));
            }
        }
    }
}