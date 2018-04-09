using System.Collections.Generic;
using System.Reflection;
using Xandernate;

namespace System.Linq.Expressions
{
    public static class ExpressionExtensions
    {
        public static string ExpressionToString<TLambdaFunctions>(this BinaryExpression body)
            where TLambdaFunctions : IExpressionFunctions, new()
        {
            Expression left = body.Left;
            BinaryExpression leftB = (left as BinaryExpression);
            TLambdaFunctions lambdaFunctions = new TLambdaFunctions();

            string result = (leftB == null) ?
                (left as MemberExpression).Member.Name + lambdaFunctions.GetOperatorNode(body.NodeType) :
                leftB.ExpressionToString<TLambdaFunctions>() + lambdaFunctions.GetOperatorNode(body.NodeType) + " ";

            Expression right = body.Right;
            BinaryExpression rightB = (right as BinaryExpression);

            return (rightB == null) ?
                result + right.ToString().Replace("'", string.Empty).Replace("\"", "'").Replace(",", ".") + " " :
                result + rightB.ExpressionToString<TLambdaFunctions>().Replace("'", string.Empty).Replace("\"", "'").Replace(",", ".");
        }

        public static IEnumerable<PropertyInfo> GetProperties<TClass>(this Expression<Func<TClass, object>> IdentifierExpression)
        {
            NewExpression newExpression = (IdentifierExpression.Body as NewExpression);
            if (newExpression == null)
            {
                UnaryExpression unaryExpression = (IdentifierExpression.Body as UnaryExpression);
                PropertyInfo property;
                if (unaryExpression == null)
                    property = (IdentifierExpression.Body as MemberExpression).Member as PropertyInfo;
                else
                    property = (unaryExpression.Operand as MemberExpression).Member as PropertyInfo;

                yield return property;
            }
            else
                foreach (MemberInfo member in newExpression.Members)
                    yield return typeof(TClass).GetProperty(member.Name);
        }
    }
}
