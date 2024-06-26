﻿using NetJsScriptBridge;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace JsonCollectionNet
{
    internal static class Helpers
    {
        /// <summary>
        /// JSON 문자열에서 모든 키를 쌍따옴표로 감싸고 유효한 JSON 형식으로 반환합니다.
        /// 이는 한글을 포함하여 다양한 유니코드 문자를 지원합니다.
        /// </summary>
        /// <param name="json">원본 JSON 문자열</param>
        /// <returns>유효한 JSON 문자열</returns>
        public static string EncapsulateJsonKeys(string json)
        {
            // 모든 유니코드 문자를 허용하고, 기존에 쌍따옴표로 감싸지 않은 키를 찾아 쌍따옴표로 감싸기
            string pattern = @"(?<=\{|\,|\[)\s*(?<!\"")([^\[\]\{\}:,""']\S*?[^\[\]\{\}:,""'\s])\s*(?=:)";
            string formattedJson = Regex.Replace(json, pattern, m => $"\"{m.Groups[1].Value.Trim()}\"");
            return formattedJson;
        }

        internal static object? GetDynamicValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.True or JsonValueKind.False => element.GetBoolean(),
                JsonValueKind.Undefined or JsonValueKind.Object or JsonValueKind.Array or JsonValueKind.Null => null,
                _ => null,
            };
        }

        internal static string ReplaceJson(string json)
        {
            while (BracketHelper.FindBracketContent(json, "new Date(") is string script)
            {
                var dateTime = JsScriptParser.ParseDateTime(script);
                json = json.Replace(script, $@"""{dateTime:o}""");
            }

            var replaced = Helpers.EncapsulateJsonKeys(json);
            return replaced;
        }
    }

    public class JsonCollection
    {
        private readonly JsonElement data;
        private readonly JsonSerializerOptions serializerOptions = new() { PropertyNameCaseInsensitive = true };

        public JsonCollection(JsonElement data)
        {
            if (data.ValueKind != JsonValueKind.Array) throw new InvalidOperationException("The input JSON is not an array.");
            this.data = data;
        }

        public int Count => data.GetArrayLength();

        public JsonElement Aggregate(string aggregate)
        {
            var jsonAggregate = Helpers.ReplaceJson(aggregate);
            JsonNode? parsedNode = JsonNode.Parse(jsonAggregate);

            // JsonNode를 JsonObject로 변환하거나, JsonArray 내의 JsonObject를 추출
            if (parsedNode is JsonObject aggregateObject)
            {
                return ProcessAggregateObject(aggregateObject);
            }
            else if (parsedNode is JsonArray aggregateArray && aggregateArray.Count > 0 && aggregateArray[0] is JsonObject)
            {
                // 배열의 첫 번째 요소가 JsonObject일 때만 처리
                JsonObject firstObject = (JsonObject)aggregateArray[0];
                return ProcessAggregateObject(firstObject);
            }
            else
            {
                throw new JsonException("Invalid JSON format for aggregation.");
            }
        }

        private JsonElement ProcessAggregateObject(JsonObject aggregateObject)
        {
            IEnumerable<JsonElement> results = data.EnumerateArray();

            // $match 처리
            if (aggregateObject.ContainsKey("$match"))
            {
                var matchConditions = aggregateObject["$match"]?.AsObject();
                results = ApplyMatch(results, matchConditions);
            }

            // $group 처리
            if (aggregateObject.ContainsKey("$group"))
            {
                var groupConditions = aggregateObject["$group"]?.AsObject();
                results = ApplyGroup(results, groupConditions);
            }

            // $sort 처리
            if (aggregateObject.ContainsKey("$sort"))
            {
                var sortConditions = aggregateObject["$sort"]?.AsObject();
                results = ApplySort(results, sortConditions);
            }

            // $limit 처리
            if (aggregateObject.ContainsKey("$limit"))
            {
                var limit = aggregateObject["$limit"]?.GetValue<int>() ?? 0;
                results = results.Take(limit);
            }

            var resultArray = new JsonArray(results.Select(result => JsonNode.Parse(result.GetRawText())).ToArray());
            return JsonDocument.Parse(resultArray.ToJsonString()).RootElement;
        }

        private static IEnumerable<JsonElement> ApplyMatch(IEnumerable<JsonElement> source, JsonObject matchConditions)
        {
            foreach (var item in source)
            {
                bool matches = true;
                foreach (var condition in matchConditions)
                {
                    var keys = condition.Key.ToLower().Split('.');  // 키를 소문자로 변환
                    JsonElement itemElement = item;
                    foreach (var key in keys)
                    {
                        if (itemElement.TryGetPropertyIgnoreCase(key, out var nextElement))
                        {
                            itemElement = nextElement;
                        }
                        else
                        {
                            matches = false;
                            break;
                        }
                    }

                    if (!matches)
                    {
                        break;
                    }

                    var valueConditions = condition.Value.AsObject();
                    matches = EvaluateCondition(itemElement, valueConditions);

                    if (!matches)
                    {
                        break;
                    }
                }

                if (matches)
                {
                    yield return item;
                }
            }
        }

        private static bool EvaluateCondition(JsonElement itemElement, JsonObject valueConditions)
        {
            foreach (var condition in valueConditions)
            {
                string key = condition.Key;
                JsonNode valueNode = condition.Value;

                // JsonNode를 JsonElement로 변환
                string jsonValueString = valueNode.ToJsonString();
                JsonElement value;
                using (var jsonDoc = JsonDocument.Parse(jsonValueString))
                {
                    value = jsonDoc.RootElement.Clone();
                }

                switch (itemElement.ValueKind)
                {
                    case JsonValueKind.String:
                        var stringValue = itemElement.GetString();
                        // 날짜 또는 문자열 조건
                        if (DateTime.TryParse(stringValue, out DateTime dateValue))
                        {
                            // 날짜 조건 검사
                            if (!EvaluateDateTimeCondition(dateValue, key, value)) return false;
                        }
                        else
                        {
                            // 문자열 조건 검사
                            if (!EvaluateStringCondition(stringValue, key, value)) return false;
                        }
                        break;

                    case JsonValueKind.Number:
                        var numberValue = itemElement.GetDouble();
                        // 숫자 조건 검사
                        if (!EvaluateNumberCondition(numberValue, key, value)) return false;
                        break;

                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        var boolValue = itemElement.GetBoolean();
                        // 불린 조건 검사
                        if (!EvaluateBooleanCondition(boolValue, key, value)) return false;
                        break;
                }
            }

            return true;
        }

        private static bool EvaluateDateTimeCondition(DateTime dateValue, string conditionKey, JsonElement conditionValue)
        {
            if (DateTime.TryParse(conditionValue.GetString(), out DateTime targetDate))
            {
                switch (conditionKey)
                {
                    case "$gte": return dateValue >= targetDate;
                    case "$lte": return dateValue <= targetDate;
                    case "$gt": return dateValue > targetDate;
                    case "$lt": return dateValue < targetDate;
                }
            }
            return true; // 조건 값이 날짜가 아니면 건너뛰기
        }

        private static bool EvaluateStringCondition(string stringValue, string conditionKey, JsonElement conditionValue)
        {
            switch (conditionKey)
            {
                case "$eq": return stringValue == conditionValue.GetString();
                case "$ne": return stringValue != conditionValue.GetString();
            }
            return true;
        }

        private static bool EvaluateNumberCondition(double numberValue, string conditionKey, JsonElement conditionValue)
        {
            switch (conditionKey)
            {
                case "$gt": return numberValue > conditionValue.GetDouble();
                case "$lt": return numberValue < conditionValue.GetDouble();
                case "$gte": return numberValue >= conditionValue.GetDouble();
                case "$lte": return numberValue <= conditionValue.GetDouble();
                case "$eq": return numberValue == conditionValue.GetDouble();
                case "$ne": return numberValue != conditionValue.GetDouble();
            }
            return true;
        }

        private static bool EvaluateBooleanCondition(bool boolValue, string conditionKey, JsonElement conditionValue)
        {
            switch (conditionKey)
            {
                case "$eq": return boolValue == conditionValue.GetBoolean();
                case "$ne": return boolValue != conditionValue.GetBoolean();
            }
            return true;
        }

        private static IEnumerable<JsonElement> ApplyGroup(IEnumerable<JsonElement> source, JsonObject groupConditions)
        {
            JsonNode? idNode = groupConditions["_id"];
            Func<JsonElement, object> keySelector;

            if (idNode == null)
            {
                // 모든 요소를 같은 그룹으로 처리
                keySelector = elem => 1;
            }
            else if (idNode is JsonValue valueNode)
            {
                // _id가 단순 값이면 해당 값을 키로 사용
                string keyPath = valueNode.GetValue<string>().Trim('$');
                keySelector = elem =>
                {
                    string[] parts = keyPath.Split('.');
                    JsonElement propertyElement = elem;
                    foreach (var part in parts)
                    {
                        if (propertyElement.TryGetPropertyIgnoreCase(part, out var nextElement))
                        {
                            propertyElement = nextElement;
                        }
                        else
                        {
                            return null; // 키가 없을 경우 null을 반환하여 이 항목을 무시
                        }
                    }
                    return Helpers.GetDynamicValue(propertyElement);
                };
            }
            else if (idNode is JsonObject idObject)
            {
                // _id가 객체면 각 필드를 키로 사용
                keySelector = elem =>
                {
                    var keyValues = new Dictionary<string, object>();
                    foreach (var kvp in idObject)
                    {
                        string[] path = kvp.Value.GetValue<string>().Trim('$').Split('.');
                        JsonElement propertyElement = elem;
                        foreach (var part in path)
                        {
                            if (propertyElement.TryGetPropertyIgnoreCase(part, out var nextElement))
                            {
                                propertyElement = nextElement;
                            }
                            else
                            {
                                return null; // 키가 없을 경우 null을 반환하여 이 항목을 무시
                            }
                        }
                        keyValues.Add(kvp.Key, Helpers.GetDynamicValue(propertyElement));
                    }
                    return keyValues;
                };
            }
            else
            {
                throw new InvalidOperationException("Unsupported type for _id in $group operation.");
            }

            var groupedResults = source.GroupBy(
                keySelector,
                elem => elem,
                (key, elems) => new
                {
                    _id = key,
                    count = elems.Count()
                });

            return groupedResults.Select(group => JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                group._id,
                group.count
            })).RootElement);
        }

        private static IEnumerable<JsonElement> ApplySort(IEnumerable<JsonElement> source, JsonObject sortConditions)
        {
            // 정렬을 위한 키와 방향 쌍을 리스트로 변환
            var sortList = new List<KeyValuePair<string, int>>();
            foreach (var prop in sortConditions)
            {
                sortList.Add(new KeyValuePair<string, int>(prop.Key, prop.Value.GetValue<int>()));
            }

            // 다중 필드 정렬을 위한 비교자 구현
            IOrderedEnumerable<JsonElement>? orderedQuery = null;
            bool firstSort = true;
            foreach (var sort in sortList)
            {
                if (firstSort)
                {
                    orderedQuery = ApplyOrder(source, sort.Key, sort.Value);
                    firstSort = false;
                }
                else
                {
                    orderedQuery = ApplyThenOrder(orderedQuery, sort.Key, sort.Value);
                }
            }

            return orderedQuery ?? source;
        }

        private static IOrderedEnumerable<JsonElement> ApplyOrder(IEnumerable<JsonElement> source, string key, int direction)
        {
            return direction == 1
                ? source.OrderBy(item => Helpers.GetDynamicValue(item.GetProperty(key)))
                : source.OrderByDescending(item => Helpers.GetDynamicValue(item.GetProperty(key)));
        }

        private static IOrderedEnumerable<JsonElement> ApplyThenOrder(IOrderedEnumerable<JsonElement> source, string key, int direction)
        {
            return direction == 1
                ? source.ThenBy(item => Helpers.GetDynamicValue(item.GetProperty(key)))
                : source.ThenByDescending(item => Helpers.GetDynamicValue(item.GetProperty(key)));
        }

        public static JsonCollection Parse(string json, JsonSerializerOptions? options = null)
        {
            var el = JsonSerializer.Deserialize<JsonElement>(json, options);
            return new JsonCollection(el);
        }
    }
}