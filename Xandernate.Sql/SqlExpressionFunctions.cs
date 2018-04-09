using System.Linq.Expressions;

namespace Xandernate.Sql
{
    internal class SqlExpressionFunctions : IExpressionFunctions
    {
        public string GetOperatorNode(ExpressionType nodo)
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
                default: return string.Empty;
            }
        }
    }
}
