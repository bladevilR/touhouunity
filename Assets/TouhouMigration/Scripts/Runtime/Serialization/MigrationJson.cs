using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TouhouMigration.Runtime.Serialization
{
    public static class MigrationJson
    {
        public static object Parse(string json)
        {
            Parser parser = new Parser(json);
            return parser.ParseValue();
        }

        private sealed class Parser
        {
            private readonly string json;
            private int index;

            public Parser(string json)
            {
                this.json = json ?? string.Empty;
            }

            public object ParseValue()
            {
                SkipWhitespace();
                if (index >= json.Length)
                {
                    throw new FormatException("Unexpected end of JSON.");
                }

                char current = json[index];
                return current switch
                {
                    '{' => ParseObject(),
                    '[' => ParseArray(),
                    '"' => ParseString(),
                    't' => ParseLiteral("true", true),
                    'f' => ParseLiteral("false", false),
                    'n' => ParseLiteral("null", null),
                    _ => ParseNumber()
                };
            }

            private Dictionary<string, object> ParseObject()
            {
                Expect('{');
                Dictionary<string, object> result = new Dictionary<string, object>();
                SkipWhitespace();
                if (TryConsume('}'))
                {
                    return result;
                }

                while (true)
                {
                    string key = ParseString();
                    SkipWhitespace();
                    Expect(':');
                    object value = ParseValue();
                    result[key] = value;
                    SkipWhitespace();
                    if (TryConsume('}'))
                    {
                        return result;
                    }

                    Expect(',');
                }
            }

            private List<object> ParseArray()
            {
                Expect('[');
                List<object> result = new List<object>();
                SkipWhitespace();
                if (TryConsume(']'))
                {
                    return result;
                }

                while (true)
                {
                    result.Add(ParseValue());
                    SkipWhitespace();
                    if (TryConsume(']'))
                    {
                        return result;
                    }

                    Expect(',');
                }
            }

            private string ParseString()
            {
                Expect('"');
                StringBuilder builder = new StringBuilder();
                while (index < json.Length)
                {
                    char current = json[index++];
                    if (current == '"')
                    {
                        return builder.ToString();
                    }

                    if (current != '\\')
                    {
                        builder.Append(current);
                        continue;
                    }

                    if (index >= json.Length)
                    {
                        throw new FormatException("Unterminated JSON escape sequence.");
                    }

                    char escaped = json[index++];
                    switch (escaped)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            builder.Append(escaped);
                            break;
                        case 'b':
                            builder.Append('\b');
                            break;
                        case 'f':
                            builder.Append('\f');
                            break;
                        case 'n':
                            builder.Append('\n');
                            break;
                        case 'r':
                            builder.Append('\r');
                            break;
                        case 't':
                            builder.Append('\t');
                            break;
                        case 'u':
                            builder.Append(ParseUnicodeEscape());
                            break;
                        default:
                            throw new FormatException($"Unsupported JSON escape: \\{escaped}");
                    }
                }

                throw new FormatException("Unterminated JSON string.");
            }

            private char ParseUnicodeEscape()
            {
                if (index + 4 > json.Length)
                {
                    throw new FormatException("Incomplete unicode escape.");
                }

                string hex = json.Substring(index, 4);
                index += 4;
                return (char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            private object ParseNumber()
            {
                int start = index;
                if (json[index] == '-')
                {
                    index++;
                }

                while (index < json.Length && char.IsDigit(json[index]))
                {
                    index++;
                }

                bool floatingPoint = false;
                if (index < json.Length && json[index] == '.')
                {
                    floatingPoint = true;
                    index++;
                    while (index < json.Length && char.IsDigit(json[index]))
                    {
                        index++;
                    }
                }

                if (index < json.Length && (json[index] == 'e' || json[index] == 'E'))
                {
                    floatingPoint = true;
                    index++;
                    if (index < json.Length && (json[index] == '+' || json[index] == '-'))
                    {
                        index++;
                    }

                    while (index < json.Length && char.IsDigit(json[index]))
                    {
                        index++;
                    }
                }

                string token = json.Substring(start, index - start);
                if (floatingPoint)
                {
                    return double.Parse(token, CultureInfo.InvariantCulture);
                }

                return long.Parse(token, CultureInfo.InvariantCulture);
            }

            private object ParseLiteral(string literal, object value)
            {
                if (index + literal.Length > json.Length ||
                    string.CompareOrdinal(json, index, literal, 0, literal.Length) != 0)
                {
                    throw new FormatException($"Expected JSON literal {literal}.");
                }

                index += literal.Length;
                return value;
            }

            private void SkipWhitespace()
            {
                while (index < json.Length && char.IsWhiteSpace(json[index]))
                {
                    index++;
                }
            }

            private void Expect(char expected)
            {
                SkipWhitespace();
                if (index >= json.Length || json[index] != expected)
                {
                    throw new FormatException($"Expected '{expected}' at JSON offset {index}.");
                }

                index++;
            }

            private bool TryConsume(char expected)
            {
                SkipWhitespace();
                if (index < json.Length && json[index] == expected)
                {
                    index++;
                    return true;
                }

                return false;
            }
        }
    }
}
