using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonCollectionNet.Tests
{
    public class JsonCollectionNetTests
    {
        private readonly JsonElement users;

        public JsonCollectionNetTests()
        {
            var usersJson = @"[
                { ""id"": 1, ""name"": ""Alice"", ""age"": 30 },
                { ""id"": 2, ""name"": ""Bob"", ""age"": 25 },
                { ""id"": 3, ""name"": ""Charlie"", ""age"": 35 },
                { ""id"": 4, ""name"": ""David"", ""age"": 40 }
              ]";

            this.users = JsonSerializer.Deserialize<JsonElement>(usersJson);
        }

        [Fact]
        public void TestFilterUsersByAgeOverThirty()
        {
            var aggregate = @"{ $match: { age: { $gt: 30 } } }";
            var collection = new JsonCollection(users);
            var results = collection.Aggregate(aggregate);

            var goles = new JsonArray();
            foreach (var user in users.EnumerateArray())
            {
                if (user.GetProperty("age").GetInt32() > 30)
                {
                    goles.Add(JsonNode.Parse(user.GetRawText()));
                }
            }

            Assert.Equal(results.GetRawText(), JsonDocument.Parse(goles.ToJsonString()).RootElement.GetRawText());
        }

        [Fact]
        public void TestGroupUsersByAgeAndCount()
        {
            var aggregate = @"{ $group: { _id: ""$age"", count: { $sum: 1 } } }";
            var collection = new JsonCollection(users);
            var results = collection.Aggregate(aggregate);

            var goles = new JsonArray
            {
                JsonNode.Parse(@"{ ""_id"": 25, ""count"": 1 }"),
                JsonNode.Parse(@"{ ""_id"": 30, ""count"": 1 }"),
                JsonNode.Parse(@"{ ""_id"": 35, ""count"": 1 }"),
                JsonNode.Parse(@"{ ""_id"": 40, ""count"": 1 }")
            };

            // 순서가 달라질 수 있으므로 각 결과를 확인
            foreach (var exp in goles)
            {
                Assert.Contains(exp.ToJsonString(), results.EnumerateArray().Select(x => x.GetRawText()));
            }
        }

        [Fact]
        public void TestSortUsersByAge()
        {
            var aggregate = @"{ $sort: { age: 1 } }";
            var collection = new JsonCollection(users);
            var results = collection.Aggregate(aggregate);

            var goles = new JsonArray
            {
                JsonNode.Parse(@"{ ""id"": 2, ""name"": ""Bob"", ""age"": 25 }"),
                JsonNode.Parse(@"{ ""id"": 1, ""name"": ""Alice"", ""age"": 30 }"),
                JsonNode.Parse(@"{ ""id"": 3, ""name"": ""Charlie"", ""age"": 35 }"),
                JsonNode.Parse(@"{ ""id"": 4, ""name"": ""David"", ""age"": 40 }")
            };

            Assert.Equal(results.GetRawText(), JsonDocument.Parse(goles.ToJsonString()).RootElement.GetRawText());
        }

        [Fact]
        public void TestLimitUsers()
        {
            var aggregate = @"{ $limit: 2 }";
            var collection = new JsonCollection(users);
            var results = collection.Aggregate(aggregate);

            Assert.Equal(2, results.EnumerateArray().Count());
        }

        [Fact]
        public void TestFilterAndGroupAndSortAndLimit()
        {
            var aggregate = @"{ $match: { age: { $gt: 30 } }, $group: { _id: ""$age"", count: { $sum: 1 } }, $sort: { _id: 1 }, $limit: 2 }";
            var collection = new JsonCollection(users);
            var results = collection.Aggregate(aggregate);

            var goles = new JsonArray
            {
                JsonNode.Parse(@"{ ""_id"": 35, ""count"": 1 }"),
                JsonNode.Parse(@"{ ""_id"": 40, ""count"": 1 }")
            };

            Assert.Equal(results.GetRawText(), JsonDocument.Parse(goles.ToJsonString()).RootElement.GetRawText());
        }

        [Fact]
        public void TestFilterUsersByNameAndAge()
        {
            var aggregate = @"{ $match: { age: { $gt: 25 }, name: { $eq: ""Alice"" } } }";
            var collection = new JsonCollection(users);
            var results = collection.Aggregate(aggregate);

            var goles = new JsonArray
            {
                JsonNode.Parse(@"{ ""id"": 1, ""name"": ""Alice"", ""age"": 30 }")
            };

            Assert.Equal(results.GetRawText(), JsonDocument.Parse(goles.ToJsonString()).RootElement.GetRawText());
        }

        [Fact]
        public void TestSortUsersByNameThenAge()
        {
            var aggregate = @"{ $sort: { name: 1, age: -1 } }";
            var collection = new JsonCollection(users);
            var results = collection.Aggregate(aggregate);

            var goles = new JsonArray
            {
                JsonNode.Parse(@"{ ""id"": 1, ""name"": ""Alice"", ""age"": 30 }"),
                JsonNode.Parse(@"{ ""id"": 2, ""name"": ""Bob"", ""age"": 25 }"),
                JsonNode.Parse(@"{ ""id"": 3, ""name"": ""Charlie"", ""age"": 35 }"),
                JsonNode.Parse(@"{ ""id"": 4, ""name"": ""David"", ""age"": 40 }")
            };

            Assert.Equal(results.GetRawText(), JsonDocument.Parse(goles.ToJsonString()).RootElement.GetRawText());
        }

        [Fact]
        public void TestGroupUsersByNameAndAge()
        {
            var aggregate = @"{ $group: { _id: { name: ""$name"", age: ""$age"" }, count: { $sum: 1 } } }";
            var collection = new JsonCollection(users);
            var results = collection.Aggregate(aggregate);

            var goles = new JsonArray
            {
                JsonNode.Parse(@"{ ""_id"": { ""name"": ""Alice"", ""age"": 30 }, ""count"": 1 }"),
                JsonNode.Parse(@"{ ""_id"": { ""name"": ""Bob"", ""age"": 25 }, ""count"": 1 }"),
                JsonNode.Parse(@"{ ""_id"": { ""name"": ""Charlie"", ""age"": 35 }, ""count"": 1 }"),
                JsonNode.Parse(@"{ ""_id"": { ""name"": ""David"", ""age"": 40 }, ""count"": 1 }")
            };

            // 순서가 달라질 수 있으므로 각 결과를 확인
            foreach (var exp in goles)
            {
                Assert.Contains(exp.ToJsonString(), results.EnumerateArray().Select(x => x.GetRawText()));
            }
        }

        [Fact]
        public void TestLimitUsersWithCondition()
        {
            var aggregate = @"{ $match: { age: { $gt: 25 } }, $limit: 2 }";
            var collection = new JsonCollection(users);
            var results = collection.Aggregate(aggregate);

            Assert.Equal(2, results.EnumerateArray().Count());
        }
    }
}