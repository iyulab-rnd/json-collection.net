using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonCollectionNet.Tests
{
    public class JsonCollectionNetUtilsTests
    {
        private readonly string usersJson;
        private readonly JsonElement users;

        public JsonCollectionNetUtilsTests()
        {
            this.usersJson = @"[
                { ""id"": 1, ""name"": ""Alice"", ""age"": 30 },
                { ""id"": 2, ""name"": ""Bob"", ""age"": 25 },
                { ""id"": 3, ""name"": ""Charlie"", ""age"": 35 },
                { ""id"": 4, ""name"": ""David"", ""age"": 40 }
              ]";

            this.users = JsonSerializer.Deserialize<JsonElement>(usersJson);
        }

        [Fact]
        public void ParseAndCollectionCountTest()
        {
            var collection = JsonCollection.Parse(this.usersJson);
            Assert.Equal(4, collection.Count);
        }

        [Fact]
        public void TestInvalidJsonThrowsException()
        {
            var invalidJson = @"{ ""id"": 1, ""name"": ""Alice"", ""age"": ""thirty"" }";  // 나이가 숫자가 아닌 문자열로 표기됨
            Assert.Throws<InvalidOperationException>(() => JsonCollection.Parse(invalidJson));
        }

        [Fact]
        public void TestEmptyJsonCollection()
        {
            var emptyJson = @"[]";
            var collection = JsonCollection.Parse(emptyJson);
            Assert.Equal(0, collection.Count);
        }

        [Fact]
        public void TestPropertyAccessWithCorrectType()
        {
            var collection = JsonCollection.Parse(this.usersJson);
            var result = collection.Aggregate(@"{ $match: { age: { $eq: 30 } } }");
            Assert.True(result.EnumerateArray().Any()); // 나이가 30인 사용자가 적어도 하나 존재하는지 확인
        }

        [Fact]
        public void TestJsonSerializerOptionsIgnoreCase()
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var collection = JsonCollection.Parse(this.usersJson, options);
            var result = collection.Aggregate(@"{ $match: { NAME: { $eq: ""Alice"" } } }"); // 대소문자 구분 없이 속성 이름을 검색
            Assert.True(result.EnumerateArray().Any());
        }

    }
}