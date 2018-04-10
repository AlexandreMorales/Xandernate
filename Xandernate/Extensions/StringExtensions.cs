using System.Text;

namespace System
{
    public static class StringExtensions
    {
        public static string SubstringLast(this string str, int cont = 2)
            => str.Substring(0, (str.Length - cont));

        public static string SubstringLast(this StringBuilder str, int cont = 2)
            => str.ToString().SubstringLast(cont);
    }
}
