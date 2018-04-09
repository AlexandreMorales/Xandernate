using System.Collections.Generic;
using System.Reflection;
using Xandernate;

namespace System.Linq.Expressions
{
    public static class ExpressionExtensions
    {
        public static string ExpressionToString(this BinaryExpression body, IExpressionFunctions expressionFunctions)
        {
            Expression left = body.Left;
            Expression right = body.Right;
            string nodeString = expressionFunctions.GetOperatorNode(body.NodeType);

            string leftString = (left is BinaryExpression leftB) ?
                leftB.ExpressionToString(expressionFunctions) + nodeString + " " :
                (left as MemberExpression).Member.Name + nodeString;

            return (right is BinaryExpression rightB) ?
                leftString + rightB.ExpressionToString(expressionFunctions).NormalizeString() :
                leftString + right.ToString().NormalizeString() + " ";
        }

        private static string NormalizeString(this string str)
            => str.Replace("'", string.Empty).Replace("\"", "'").Replace(",", ".");

        public static IEnumerable<PropertyInfo> GetProperties<TClass>(this Expression<Func<TClass, object>> IdentifierExpression)
        {
            if (IdentifierExpression.Body is NewExpression newExpression)
            {
                Type type = typeof(TClass);

                foreach (MemberInfo member in newExpression.Members)
                    yield return type.GetProperty(member.Name);
            }
            else
            {
                MemberExpression member = (IdentifierExpression.Body is UnaryExpression unaryExpression) ?
                    unaryExpression.Operand as MemberExpression :
                    IdentifierExpression.Body as MemberExpression;

                yield return member.Member as PropertyInfo;
            }
        }
    }
}
