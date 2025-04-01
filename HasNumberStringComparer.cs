using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Example
{
    public class HasNumberStringComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == null && y == null)
                return 0;
            if (x == null)
                return -1;
            if (y == null)
                return 1;

            var segmentsX = GetSegments(x);
            var segmentsY = GetSegments(y);
            return segmentsX.CompareTo(segmentsY);
        }

        private static CompareSegmentCollection GetSegments(string source)
        {
            var tmpStr = new StringBuilder();
            var tmpNumStr = new StringBuilder();
            bool gettingNumber = false;
            bool gettingNumberDouble = false;

            CompareSegmentCollection segments = new CompareSegmentCollection();
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                if (char.IsDigit(c))
                {
                    if (tmpStr.Length > 0)
                    {
                        segments.Add(new CompareSegment(tmpStr.ToString()));
                        tmpStr.Clear();
                    }

                    gettingNumber = true;
                    tmpNumStr.Append(ToHalfWidthIfDigitCharacterIsFullWidth(c));
                    continue;
                }

                if (gettingNumber)
                {
                    if (c == '.' && !gettingNumberDouble)
                    {
                        gettingNumberDouble = true;
                        tmpNumStr.Append('.');
                        continue;
                    }

                    segments.Add(new CompareSegment(decimal.Parse(tmpNumStr.ToString())));
                    tmpNumStr.Clear();
                    gettingNumber = false;
                    gettingNumberDouble = false;
                }

                tmpStr.Append(c);
            }

            if (tmpStr.Length > 0)
            { segments.Add(new CompareSegment(tmpStr.ToString())); }

            if (tmpNumStr.Length > 0)
            { segments.Add(new CompareSegment(decimal.Parse(tmpNumStr.ToString()))); }

            return segments;
        }

        private static char ToHalfWidthIfDigitCharacterIsFullWidth(char c)
        {
            int codePoint = c;
            if (codePoint > 65248)
            {
                return (char)(codePoint - 65248);
            }
            return c;
        }


        private class CompareSegment : IComparable<CompareSegment>, IEquatable<CompareSegment>
        {
            public string StringValue { get; set; }
            public decimal NumberValue { get; set; }
            public bool IsNumber { get; set; }

            public CompareSegment(string value)
            {
                StringValue = value;
                IsNumber = false;
            }

            public CompareSegment(decimal value)
            {
                NumberValue = value;
                IsNumber = true;
            }

            public int CompareTo(CompareSegment other)
            {
                if (other == null)
                    return -1;

                if (IsNumber && other.IsNumber)
                    return Convert.ToInt32(Math.Ceiling(NumberValue - other.NumberValue));
                if (IsNumber && !other.IsNumber)
                    return -1;
                if (!IsNumber && other.IsNumber)
                    return 1;

                return string.Compare(StringValue, other.StringValue, StringComparison.InvariantCultureIgnoreCase);
            }

            public bool Equals(CompareSegment other)
            {
                if (other == null)
                    return false;

                if (IsNumber != other.IsNumber)
                    return false;

                return IsNumber ? NumberValue == other.NumberValue : StringValue == other.StringValue;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as CompareSegment);
            }

            public override int GetHashCode() => base.GetHashCode();
        }

        private class CompareSegmentCollection : ICollection<CompareSegment>, IComparable<CompareSegmentCollection>
        {
            private readonly List<CompareSegment> segments = new List<CompareSegment>();

            public CompareSegment this[int index]
            {
                get => segments[index];
                set => segments[index] = value;
            }

            public int Count => segments.Count;
            public bool IsReadOnly => false;

            public void Add(CompareSegment item) => segments.Add(item);
            public bool Remove(CompareSegment item) => segments.Remove(item);
            public void Clear() => segments.Clear();
            public bool Contains(CompareSegment item) => segments.Contains(item);
            public void CopyTo(CompareSegment[] array, int arrayIndex) => segments.CopyTo(array, arrayIndex);
            public IEnumerator<CompareSegment> GetEnumerator() => segments.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => segments.GetEnumerator();


            public int CompareTo(CompareSegmentCollection other)
            {
                if (other == null)
                    return -1;

                for (int i = 0; i < Math.Min(this.Count, other.Count); i++)
                {
                    var thisItem = this.segments[i];
                    var otherItem = other[i];

                    int diff = thisItem.CompareTo(otherItem);
                    if (diff != 0)
                        return diff;
                }

                return 0;
            }
        }

        /*
using System;
using System.Text.RegularExpressions;
 
public class CharactersConverter
{
    // 全角转半角
    public static string ToHalfWidth(string input)
    {
        return Regex.Replace(input, @"[\u3000-\u303F\u3040-\u309F\u30A0-\u30FF\uff00-\uffef]", m =>
        {
            char c = m.Value[0];
            if (c >= '\u3000' && c <= '\u303F')
            {
                return (char)(c - 0x3000 + 0x2000) + "";
            }
            else if (c >= '\u3040' && c <= '\u309F')
            {
                return (char)(c - 0x3040 + 0x2040) + "";
            }
            else if (c >= '\u30A0' && c <= '\u30FF')
            {
                return (char)(c - 0x30A0 + 0x20A0) + "";
            }
            else if (c >= '\uff00' && c <= '\uffef')
            {
                return (char)(c - 0xff00 + 0x2000) + "";
            }
            return m.Value;
        });
    }
 
    // 半角转全角
    public static string ToFullWidth(string input)
    {
        return Regex.Replace(input, @"[\u0020-\u007F]", m =>
        {
            char c = m.Value[0];
            if (c >= '\u0020' && c <= '\u0040')
            {
                return (char)(c - 0x0020 + 0x3000) + "";
            }
            else if (c >= '\u005B' && c <= '\u0060')
            {
                return (char)(c - 0x005B + 0x309B) + "";
            }
            else if (c >= '\u007B' && c <= '\u007E')
            {
                return (char)(c - 0x007B + 0x30FB) + "";
            }
            return m.Value;
        });
    }
}
 
// 使用示例
class Program
{
    static void Main()
    {
        string halfWidth = "ａｂｃ１２３";
        string fullWidth = "abc123";
 
        string halfWidthConverted = CharactersConverter.ToHalfWidth(fullWidth);
        string fullWidthConverted = CharactersConverter.ToFullWidth(halfWidth);
 
        Console.WriteLine(halfWidthConverted); // 输出: abc123
        Console.WriteLine(fullWidthConverted); // 输出: ａｂｃ１２３
    }
}
         */
    }
}
