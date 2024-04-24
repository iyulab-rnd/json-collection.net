namespace JsonCollectionNet
{
    public static class BracketHelper
    {
        /// <summary>
        /// input에서 keyword와 일치하는 괄호 내용을 찾아 반환합니다. (괄호의 쌍을 맞춰서 찾습니다.)
        /// keyword의 마지막 문자는 '(', '[', '{' 중 하나여야 합니다.
        /// includeKeyword: true일 경우 반환값에 keyword와 닫는 괄호를 포함합니다.
        /// </summary>
        public static string? FindBracketContent(string input, string keyword, bool includeKeyword = true)
        {
            char openingBracket = keyword[keyword.Length - 1];
            char closingBracket = GetClosingBracket(openingBracket);

            int startIndex = input.IndexOf(keyword);
            if (startIndex == -1)
            {
                return null; // 키워드가 없으면 null 반환
            }

            int bracketStartIndex = startIndex + keyword.Length - 1;
            int endIndex = FindMatchingBracket(input, bracketStartIndex, openingBracket, closingBracket);
            if (endIndex == -1)
            {
                return null; // 매칭되는 괄호가 없으면 null 반환
            }

            if (includeKeyword)
            {
                return input.Substring(startIndex, endIndex - startIndex + 1);
            }
            else
            {
                return input.Substring(bracketStartIndex + 1, endIndex - bracketStartIndex - 1);
            }
        }

        private static char GetClosingBracket(char openingBracket)
        {
            switch (openingBracket)
            {
                case '(':
                    return ')';
                case '[':
                    return ']';
                case '{':
                    return '}';
                default:
                    throw new ArgumentException("Keyword must end with '(', '[', or '{'.");
            }
        }

        private static int FindMatchingBracket(string input, int start, char openingBracket, char closingBracket)
        {
            int depth = 1;
            for (int i = start + 1; i < input.Length; i++)
            {
                if (input[i] == openingBracket)
                {
                    depth++;
                }
                else if (input[i] == closingBracket)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i; // 매칭되는 괄호의 위치 반환
                    }
                }
            }
            return -1; // 괄호의 쌍이 맞지 않는 경우
        }
    }

}
