using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GWYF_NewClothing;

internal static class TinyJson
{
    public static List<Dictionary<string, object>> ParseArray(string json)
    {
        var list = new List<Dictionary<string, object>>();
        json = json.Trim();
        if (!json.StartsWith("[") || !json.EndsWith("]")) return list;

        int depth = 0, start = 0;
        bool inString = false;
        for (int i = 1; i < json.Length - 1; i++)
        {
            char c = json[i];
            if (c == '"' && (i == 0 || json[i - 1] != '\\')) inString = !inString;
            if (!inString)
            {
                if (c == '{') depth++;
                else if (c == '}') depth--;
                else if (c == ',' && depth == 0)
                {
                    var item = ParseObject(json.Substring(start + 1, i - start - 1));
                    if (item != null) list.Add(item);
                    start = i;
                }
            }
        }
        // Last item
        if (start + 1 < json.Length - 1)
        {
            var item = ParseObject(json.Substring(start + 1, json.Length - start - 2));
            if (item != null) list.Add(item);
        }
        return list;
    }

    public static Dictionary<string, object> ParseObject(string segment)
    {
        var dict = new Dictionary<string, object>();
        segment = segment.Trim();
        if (!segment.StartsWith("{") || !segment.EndsWith("}")) return dict;

        int i = 1;
        while (i < segment.Length - 1)
        {
            SkipWhitespace(segment, ref i);
            if (i >= segment.Length - 1) break;

            // Read key
            var key = ReadString(segment, ref i);
            SkipWhitespace(segment, ref i);
            if (i >= segment.Length - 1 || segment[i] != ':') break;
            i++; // skip colon
            SkipWhitespace(segment, ref i);

            // Read value
            var value = ReadValue(segment, ref i);
            if (key != null) dict[key] = value;

            SkipWhitespace(segment, ref i);
            if (i < segment.Length - 1 && segment[i] == ',') i++;
        }
        return dict;
    }

    private static object ReadValue(string s, ref int i)
    {
        SkipWhitespace(s, ref i);
        if (i >= s.Length) return null!;

        if (s[i] == '"') return ReadString(s, ref i);
        if (s[i] == '{') return ParseObject(ReadBraced(s, ref i));
        if (s[i] == '[') return ReadArray(s, ref i);
        return ReadLiteral(s, ref i);
    }

    private static string ReadString(string s, ref int i)
    {
        if (i >= s.Length || s[i] != '"') return "";
        int start = ++i;
        while (i < s.Length)
        {
            if (s[i] == '\\') { i += 2; continue; }
            if (s[i] == '"') break;
            i++;
        }
        var result = s.Substring(start, i - start);
        i++; // skip closing quote
        return result;
    }

    private static string ReadBraced(string s, ref int i)
    {
        if (i >= s.Length || s[i] != '{') return "{}";
        int start = i;
        int depth = 0;
        bool inStr = false;
        while (i < s.Length)
        {
            var c = s[i];
            if (c == '"' && (i == 0 || s[i - 1] != '\\')) inStr = !inStr;
            if (!inStr)
            {
                if (c == '{') depth++;
                else if (c == '}') { depth--; i++; if (depth == 0) break; continue; }
            }
            i++;
        }
        return s.Substring(start, i - start);
    }

    private static object ReadArray(string s, ref int i)
    {
        if (i >= s.Length || s[i] != '[') return Array.Empty<float>();
        var floats = new List<float>();
        i++; // skip [
        while (i < s.Length && s[i] != ']')
        {
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] != ']')
            {
                var lit = ReadLiteral(s, ref i);
                if (lit is float f) floats.Add(f);
            }
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] == ',') i++;
        }
        if (i < s.Length && s[i] == ']') i++;
        return floats.ToArray();
    }

    private static object ReadLiteral(string s, ref int i)
    {
        int start = i;
        while (i < s.Length && !IsDelimiter(s[i])) i++;
        var token = s.Substring(start, i - start).Trim().ToLowerInvariant();

        if (token == "null") return null!;
        if (token == "true") return true;
        if (token == "false") return false;
        if (float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) return f;
        return token;
    }

    private static bool IsDelimiter(char c) => c == ',' || c == '}' || c == ']' || c == ':' || char.IsWhiteSpace(c);

    private static void SkipWhitespace(string s, ref int i)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
    }
}
