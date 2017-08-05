using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Xandernate.Utils.Extensions
{
    public static class LambdaExtensions
    {
        public static string ExpressionToString<TLambdaFunctions>(this BinaryExpression body)
            where TLambdaFunctions : ILambdaFunctions, new()
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
                result + right.ToString().Replace("'", "").Replace("\"", "'").Replace(",", ".") + " " :
                result + rightB.ExpressionToString<TLambdaFunctions>().Replace("'", "").Replace("\"", "'").Replace(",", ".");
        }

        public static PropertyInfo[] GetProperties<TClass>(this Expression<Func<TClass, object>> IdentifierExpression)
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

                return new PropertyInfo[] { property };
            }
            return newExpression.Members.Select(x => typeof(TClass).GetProperty(x.Name)).ToArray();
        }
    }
}
