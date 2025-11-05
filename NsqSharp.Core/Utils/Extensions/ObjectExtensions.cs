using System;

namespace NsqSharp.Utils.Extensions
{
    /// <summary>
    /// <see cref="Object"/> extension methods.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Coerce a value to the specified type <typeparamref name="T"/>. Supports Duration and Bool string/int formats.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="value">The value to coerce.</param>
        /// <returns>The value as type <typeparamref name="T"/>.</returns>
        public static T Coerce<T>(this object value)
        {
            return (T)Coerce(value, typeof(T));
        }


        private static readonly Dictionary<string, double> _unitMap = new Dictionary<string, double>
                                                                     {
                                                                         {"ns", 1},
                                                                         {"us", 1_000},
                                                                         // U+00B5 = micro symbol
                                                                         {"µs", 1_000},
                                                                         // U+03BC = Greek letter mu
                                                                         {"μs", 1_000},
                                                                         {"ms", 1_000_000},
                                                                         {"s", 1_000_000_000},
                                                                         {"m", 60_000_000_000},
                                                                         {"h", 3_600_000_000_000},
                                                                     };
        private static long leadingInt(ref Slice<char> s)
        {
            int i = 0;
            long x = 0;
            for (; i < s.Len(); i++)
            {
                char c = s[i];
                if (c < '0' || c > '9')
                {
                    break;
                }
                if (x >= (long.MaxValue - 10) / 10)
                {
                    // overflow
                    throw new OverflowException(s.ToString());
                }
                x = x * 10 + (c - '0');
            }
            s = s.Slc(i);
            return x;
        }

        private static long ParseDuration(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            // [-+]?([0-9]*(\.[0-9]*)?[a-z]+)+
            string orig = value;
            long f = 0;
            bool neg = false;
            Slice<char> s = new Slice<char>(value);

            // Consume [-+]?
            if (s != "")
            {
                var c = s[0];
                if (c == '-' || c == '+')
                {
                    neg = (c == '-');
                    s = s.Slc(1);
                }
            }

            // Special case: if all that is left is "0", this is zero.
            if (s == "0")
            {
                return 0;
            }

            if (s == "")
            {
                throw new InvalidDataException("time: invalid duration " + orig);
            }

            while (s != "")
            {
                // The next character must be [0-9.]
                if (!(s[0] == '.' || ('0' <= s[0] && s[0] <= '9')))
                {
                    throw new InvalidDataException("time: invalid duration " + orig);
                }

                // Consume [0-9]*
                var pl1 = s.Len();
                long x = leadingInt(ref s);

                double g = x;
                bool pre = (pl1 != s.Len()); // whether we consumed anything before a period

                // Consume (\.[0-9]*)?
                bool post = false;
                if (s != "" && s[0] == '.')
                {
                    s = s.Slc(1);
                    int pl2 = s.Len();
                    x = leadingInt(ref s);
                    double scale = 1.0;
                    for (var n = pl2 - s.Len(); n > 0; n--)
                    {
                        scale *= 10;
                    }
                    g += x / scale;
                    post = (pl2 != s.Len());
                }
                if (!pre && !post)
                {
                    // no digits (e.g. ".s" or "-.s")
                    throw new InvalidDataException("time: invalid duration " + orig);
                }

                // Consume unit.
                int i = 0;
                for (; i < s.Len(); i++)
                {
                    char c = s[i];
                    if (c == '.' || ('0' <= c && c <= '9'))
                    {
                        break;
                    }
                }
                if (i == 0)
                {
                    throw new InvalidDataException("time: missing unit in duration " + orig);
                }
                var u = s.Slc(0, i);
                s = s.Slc(i);

                double unit;
                bool ok = _unitMap.TryGetValue(u.ToString(), out unit);
                if (!ok)
                {
                    throw new InvalidDataException("time: unknown unit " + u + " in duration " + orig);
                }

                checked
                {
                    f += (long)(g * unit);
                }
            }

            if (neg)
            {
                f = -f;
            }

            return f;
        }

        /// <summary>
        /// Coerce a value to the specified <paramref name="targetType"/>. Supports Duration and Bool string/int formats.
        /// </summary>
        /// <param name="value">The value to coerce.</param>
        /// <param name="targetType">The target type.</param>
        /// <returns>The value as the <paramref name="targetType"/>.</returns>
        public static object Coerce(this object value, Type targetType)
        {
            if (value == null)
                return null;

            var valueType = value.GetType();
            if (valueType == targetType)
                return value;

            if (targetType == typeof(ushort))
            {
                if (valueType == typeof(int))
                    return Convert.ToUInt16(value);
                if (valueType == typeof(string))
                    return ushort.Parse((string)value);
            }
            else if (targetType == typeof(int))
            {
                if (valueType == typeof(string))
                    return int.Parse((string)value);
            }
            else if (targetType == typeof(long))
            {
                if (valueType == typeof(string))
                    return long.Parse((string)value);
                if (valueType == typeof(int))
                    return Convert.ToInt64(value);
            }
            else if (targetType == typeof(double))
            {
                if (valueType == typeof(string))
                    return double.Parse((string)value);
                if (valueType == typeof(int))
                    return Convert.ToDouble(value);
            }
            else if (targetType == typeof(bool))
            {
                if (valueType == typeof(string))
                {
                    string strValue = (string)value;
                    if (strValue == "0")
                        return false;
                    else if (strValue == "1")
                        return true;
                    return bool.Parse(strValue);
                }
                if (valueType == typeof(int))
                {
                    int intValue = (int)value;
                    if (intValue == 0)
                        return false;
                    else if (intValue == 1)
                        return true;
                }
            }
            else if (targetType == typeof(TimeSpan))
            {
                if (valueType == typeof(string))
                {
                    string strValue = (string)value;

                    long ms;
                    if (long.TryParse(strValue, out ms))
                        return TimeSpan.FromMilliseconds(ms);

                    long ns = ParseDuration(strValue);
                    return new TimeSpan(ns / 100);
                }
                if (valueType == typeof(int) || valueType == typeof(long) || valueType == typeof(ulong))
                {
                    long ms = Convert.ToInt64(value);
                    return TimeSpan.FromMilliseconds(ms);
                }
            }
            else if (targetType == typeof(IBackoffStrategy))
            {
                if (valueType == typeof(string))
                {
                    string strValue = (string)value;
                    switch (strValue)
                    {
                        case "":
                        case "exponential":
                            return new ExponentialStrategy();
                        case "full_jitter":
                            return new FullJitterStrategy();
                    }
                }
                else if (value is IBackoffStrategy)
                {
                    return value;
                }
            }

            throw new Exception(string.Format("failed to coerce ({0} {1}) to {2}", value, valueType, targetType));
        }
    }
}
