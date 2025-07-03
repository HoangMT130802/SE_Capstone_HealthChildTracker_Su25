using System.Linq.Expressions;

namespace KidTracking.API.Extensions
{
    public static class ExpressionExtensions
    {
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            if (first == null)
                return second;
            if (second == null)
                return first;

            var parameter = first.Parameters[0];
            var body = Expression.AndAlso(first.Body, Expression.Invoke(second, parameter));
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }
    }
}
