#if DEBUG
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonCollectionNet.Tests
{
    public class LocalTests
    {
        [Fact]
        public void ParseAndCollectionCountTest()
        {
            var json = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "files", "data.json"));
            var aggregate = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "files", "aggregate.json"));
            // json: "[ {...}, {...}, {...} ]"
            // aggregate: "[ { $group:{ _id:null, count:{$sum:1} } } ]"

            var collection = JsonCollection.Parse(json);
            var aggregatedData = collection.Aggregate(aggregate);

            // aggregatedData: [{ "id": 1, "count": 3 }]
            int aggregatedCount = aggregatedData[0].GetProperty("count").GetInt32();
            Assert.Equal(collection.Count, aggregatedCount);
        }
    }
}
#endif