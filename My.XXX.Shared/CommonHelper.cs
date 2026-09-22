using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace My.XXX.Shared
{
    public class CommonHelper
    {
        //我们使用 FluentValidation 中的 EmailValidator，因此请保持二者同步 - https://github.com/JeremySkinner/FluentValidation/blob/master/src/FluentValidation/Validators/EmailValidator.cs
        private const string EMAIL_EXPRESSION = @"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-||_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+([a-z]+|\d|-|\.{0,1}|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])?([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))$";

        private static readonly Regex _emailRegex;

        static CommonHelper()
        {
            _emailRegex = new Regex(EMAIL_EXPRESSION, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// 校验订阅者邮箱，无效则抛出异常。
        /// </summary>
        /// <param name="email">邮箱。</param>
        /// <returns></returns>
        public static string EnsureSubscriberEmailOrThrow(string email)
        {
            var output = EnsureNotNull(email);
            output = output.Trim();
            output = EnsureMaximumLength(output, 255);

            if (!IsValidEmail(output))
            {
                throw new Exception("Email is not valid.");
            }

            return output;
        }

        /// <summary>
        /// 验证字符串是否为有效的电子邮件格式
        /// </summary>
        /// <param name="email">要验证的邮箱</param>
        /// <returns>如果字符串是有效的电子邮件地址则返回 true，否则返回 false</returns>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return false;
            }
            email = email.Trim();

            return _emailRegex.IsMatch(email);
        }

        /// <summary>
        /// 验证字符串是否为有效的 IP 地址
        /// </summary>
        /// <param name="ipAddress">要验证的 IP 地址</param>
        /// <returns>如果字符串是有效的 IP 地址则返回 true，否则返回 false</returns>
        public static bool IsValidIpAddress(string ipAddress)
        {
            return IPAddress.TryParse(ipAddress, out var _);
        }

        /// <summary>
        /// 确保字符串不超过允许的最大长度
        /// </summary>
        /// <param name="str">输入字符串</param>
        /// <param name="maxLength">最大长度</param>
        /// <param name="postfix">当原字符串被截短时，添加到末尾的字符串</param>
        /// <returns>如果输入字符串长度符合要求则返回原字符串；否则返回截断后的字符串</returns>
        public static string EnsureMaximumLength(string str, int maxLength, string postfix = null)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            if (str.Length <= maxLength)
                return str;

            var pLen = postfix?.Length ?? 0;

            var result = str[0..(maxLength - pLen)];
            if (!string.IsNullOrEmpty(postfix))
            {
                result += postfix;
            }

            return result;
        }

        /// <summary>
        /// 确保字符串仅包含数字
        /// </summary>
        /// <param name="str">输入字符串</param>
        /// <returns>仅包含数字的字符串；如果输入为 null/空则返回空字符串</returns>
        public static string EnsureNumericOnly(string str)
        {
            return string.IsNullOrEmpty(str) ? string.Empty : new string(str.Where(char.IsDigit).ToArray());
        }

        /// <summary>
        /// 确保字符串不为 null
        /// </summary>
        /// <param name="str">输入字符串</param>
        /// <returns>结果</returns>
        public static string EnsureNotNull(string str)
        {
            return str ?? string.Empty;
        }

        /// <summary>
        /// 指示指定的字符串中是否存在 null 或空字符串
        /// </summary>
        /// <param name="stringsToValidate">要验证的字符串数组</param>
        /// <returns>布尔值</returns>
        public static bool AreNullOrEmpty(params string[] stringsToValidate)
        {
            return stringsToValidate.Any(string.IsNullOrEmpty);
        }

        /// <summary>
        /// 比较两个数组
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="a1">数组 1</param>
        /// <param name="a2">数组 2</param>
        /// <returns>结果</returns>
        public static bool ArraysEqual<T>(T[] a1, T[] a2)
        {
            //也可参见 Enumerable.SequenceEqual(a1, a2);
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Length != a2.Length)
                return false;

            var comparer = EqualityComparer<T>.Default;
            return !a1.Where((t, i) => !comparer.Equals(t, a2[i])).Any();
        }

        /// <summary>
        /// 将对象的某个属性设置为指定值。
        /// </summary>
        /// <param name="instance">要设置其属性的对象。</param>
        /// <param name="propertyName">要设置的属性名称。</param>
        /// <param name="value">要设置给属性的值。</param>
        public static void SetProperty(object instance, string propertyName, object value)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

            var instanceType = instance.GetType();
            var pi = instanceType.GetProperty(propertyName);
            if (pi == null)
                throw new Exception($"No property '{propertyName}' found on the instance of type '{instanceType}'.");
            if (!pi.CanWrite)
                throw new Exception($"The property '{propertyName}' on the instance of type '{instanceType}' does not have a setter.");

            if (value != null && !value.GetType().IsAssignableFrom(pi.PropertyType))
                value = To(value, pi.PropertyType);
            pi.SetValue(instance, value, Array.Empty<object>());
        }

        /// <summary>
        /// 将值转换为目标类型。
        /// </summary>
        /// <param name="value">要转换的值。</param>
        /// <param name="destinationType">要将值转换到的类型。</param>
        /// <returns>转换后的值。</returns>
        public static object To(object value, Type destinationType)
        {
            return To(value, destinationType, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 将值转换为目标类型。
        /// </summary>
        /// <param name="value">要转换的值。</param>
        /// <param name="destinationType">要将值转换到的类型。</param>
        /// <param name="culture">区域文化</param>
        /// <returns>转换后的值。</returns>
        public static object To(object value, Type destinationType, CultureInfo culture)
        {
            if (value == null)
                return null;

            var sourceType = value.GetType();

            var destinationConverter = TypeDescriptor.GetConverter(destinationType);
            if (destinationConverter.CanConvertFrom(value.GetType()))
                return destinationConverter.ConvertFrom(null, culture, value);

            var sourceConverter = TypeDescriptor.GetConverter(sourceType);
            if (sourceConverter.CanConvertTo(destinationType))
                return sourceConverter.ConvertTo(null, culture, value, destinationType);

            if (destinationType.IsEnum && value is int @int)
                return Enum.ToObject(destinationType, @int);

            if (!destinationType.IsInstanceOfType(value))
                return Convert.ChangeType(value, destinationType, culture);

            return value;
        }

        /// <summary>
        /// 将值转换为目标类型。
        /// </summary>
        /// <param name="value">要转换的值。</param>
        /// <typeparam name="T">要将值转换到的类型。</typeparam>
        /// <returns>转换后的值。</returns>
        public static T To<T>(object value)
        {
            return (T)To(value, typeof(T));
        }

        /// <summary>
        /// 为前端转换枚举
        /// </summary>
        /// <param name="str">输入字符串</param>
        /// <returns>转换后的字符串</returns>
        public static string ConvertEnum(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            var result = string.Empty;
            foreach (var c in str)
                if (c.ToString() != c.ToString().ToLower())
                    result += " " + c.ToString();
                else
                    result += c.ToString();

            //确保没有多余的空格（例如当首字母为大写时）
            result = result.TrimStart();
            return result;
        }

        /// <summary>
        /// 获取相差的年数
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public static int GetDifferenceInYears(DateTime startDate, DateTime endDate)
        {
            //来源：http://stackoverflow.com/questions/9/how-do-i-calculate-someones-age-in-c
            //此处假定采用西方对年龄的理解，而非东亚的计龄方式。
            var age = endDate.Year - startDate.Year;
            if (startDate > endDate.AddYears(-age))
                age--;
            return age;
        }
    }
}