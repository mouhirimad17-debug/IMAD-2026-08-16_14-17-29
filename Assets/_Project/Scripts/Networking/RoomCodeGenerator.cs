using System.Text;
using UnityEngine;

namespace PrankMansion.Networking
{
    /// <summary>
    /// Part 10.2 step 2: "كود عشوائي محلي فريد مكوّن من ست خانات، مزيج من أحرف كبيرة
    /// (باستثناء O وI) وأرقام (باستثناء 0 و1)" - six characters, uppercase letters
    /// minus the visually-confusable O/I, digits minus 0/1.
    /// </summary>
    public static class RoomCodeGenerator
    {
        public const int CodeLength = 6;
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no O, I, 0, 1

        public static string Generate()
        {
            var sb = new StringBuilder(CodeLength);
            for (int i = 0; i < CodeLength; i++)
                sb.Append(Alphabet[Random.Range(0, Alphabet.Length)]);
            return sb.ToString();
        }
    }
}
