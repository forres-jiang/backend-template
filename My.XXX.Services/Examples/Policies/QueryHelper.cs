using System;
using System.Linq;
using System.Linq.Expressions;

namespace My.XXX.Services.Examples.Policies
{
    public class QueryHelper
    {
        public static IQueryable<T> Build<T>(IQueryable<T> search, object requestParams)
        {
            var requestProps = requestParams.GetType().GetProperties();

            var dbProps = typeof(T).GetProperties();

            foreach (var property in requestProps)
            {
                var field = dbProps.Where(m =>
                m.Name.Equals(property.Name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                var value = property.GetValue(requestParams);

                if (null == field || null == value)
                {
                    continue;
                }

                ParameterExpression parameterExpression = Expression.Parameter(typeof(T), "m");
                MemberExpression memberExpression = Expression.Property(parameterExpression, field.Name);
                ConstantExpression constant = Expression.Constant(value, field.PropertyType);
                BinaryExpression equal = Expression.Equal(memberExpression, constant);
                var lambda = Expression.Lambda<Func<T, bool>>(equal, parameterExpression);
                search = search.Where(lambda);
            }
            return search;
        }
    }
}
