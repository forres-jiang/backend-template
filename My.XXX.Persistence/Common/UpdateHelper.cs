using LinqToDB;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Expression = System.Linq.Expressions.Expression;

namespace My.XXX.Persistence.Common
{
    public class UpdateHelper<T>
    {
        private readonly List<string> ExcludeFields = new() { "id" };

        private IUpdatable<T> Condition;

        /// <summary>
        ///
        /// </summary>
        /// <param name="condition"></param>
        public UpdateHelper(IUpdatable<T> condition)
        {
            Condition = condition;
        }

        /// <summary>
        /// 不需要更新的字段
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="excludeFields"></param>
        public UpdateHelper(IUpdatable<T> condition, List<string> excludeFields)
        {
            if (null != excludeFields && excludeFields.Count > 0)
            {
                ExcludeFields.AddRange(excludeFields);
            }
            Condition = condition;
        }

        public IUpdatable<T> GetCondition(object requestParams)
        {
            var requestProps = requestParams.GetType().GetProperties();
            var dbProps = typeof(T).GetProperties().Where(m => !ExcludeFields.Contains(m.Name.ToLower()));

            foreach (var prop in requestProps)
            {
                var field = dbProps.Where(m => m.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();
                if (null == field)
                {
                    continue;
                }
                //主键不构建表达式条件
                var primaryKey = field.GetCustomAttribute<PrimaryKeyAttribute>();
                if (null != primaryKey)
                {
                    continue;
                }
                //没有值的属性不构建表达式
                var value = prop.GetValue(requestParams);
                if (null == value)
                {
                    continue;
                }

                AppendField(field, value);
            }

            return Condition;
        }

        private void AppendField(PropertyInfo field, object value)
        {
            if (field.PropertyType == typeof(string))
            {
                BuildLambda<string>(field.Name, value.ToString().Trim());
            }
            else if (field.PropertyType == typeof(int))
            {
                BuildLambda<int>(field.Name, value);
            }
            else if (field.PropertyType == typeof(bool))
            {
                BuildLambda<bool>(field.Name, value);
            }
            else if (field.PropertyType == typeof(DateTime))
            {
                BuildLambda<DateTime>(field.Name, value);
            }
            else if (field.PropertyType == typeof(double))
            {
                BuildLambda<double>(field.Name, value);
            }
            else if (field.PropertyType == typeof(decimal))
            {
                BuildLambda<decimal>(field.Name, value);
            }
            else if (field.PropertyType == typeof(float))
            {
                BuildLambda<float>(field.Name, value);
            }
            else if (field.PropertyType == typeof(Guid))
            {
                BuildLambda<Guid>(field.Name, value);
            }
        }

        private void BuildLambda<TResult>(string propertyName, object value)
        {
            ParameterExpression parameterExpression = Expression.Parameter(typeof(T), "p");
            var property = Expression.Property(parameterExpression, propertyName);
            var lambda = Expression.Lambda<Func<T, TResult>>(property, parameterExpression);
            Condition = Condition.Set(lambda, (TResult)value);
        }
    }
}