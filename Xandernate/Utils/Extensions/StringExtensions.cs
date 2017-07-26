namespace Xandernate.Utils.Extensions
{
    public static class StringExtensions
    {
        public static string SubstringLast(this string str, int cont = 2)
            => str.Substring(0, (str.Length - cont));
    }
}
