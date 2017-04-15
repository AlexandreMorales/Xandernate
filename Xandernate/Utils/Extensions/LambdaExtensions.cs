using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Xandernate.Utils.Extensions
{
    public static class LambdaExtensions
    {
        public static string ExpressionToString(this BinaryExpression body)
        {
            Expression left = body.Left;
            BinaryExpression leftB = (left as BinaryExpression);

            string result = (leftB == null) ?
                (left as MemberExpression).Member.Name + body.NodeType.GetOperatorNode() :
                leftB.ExpressionToString() + body.NodeType.GetOperatorNode() + " ";

            Expression right = body.Right;
            BinaryExpression rightB = (right as BinaryExpression);

            return (rightB == null) ?
                result + right.ToString().Replace("'", "").Replace("\"", "'").Replace(",", ".") + " " :
                result + rightB.ExpressionToString().Replace("'", "").Replace("\"", "'").Replace(",", ".");
        }

        private static string GetOperatorNode(this ExpressionType nodo)
        {
            switch (nodo)
            {
                case ExpressionType.And: return "and";
                case ExpressionType.AndAlso: return "and";
                case ExpressionType.Or: return "or";
                case ExpressionType.OrElse: return "or";
                case ExpressionType.LessThan: return "<";
                case ExpressionType.LessThanOrEqual: return "<=";
                case ExpressionType.GreaterThan: return ">";
                case ExpressionType.GreaterThanOrEqual: return ">=";
                case ExpressionType.Equal: return "=";
                case ExpressionType.NotEqual: return "<>";
                default: return "";
            }
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
