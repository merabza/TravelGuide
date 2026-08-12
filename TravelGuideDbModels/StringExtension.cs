namespace TravelGuideDbModels;

public static class StringExtension
{
    //Url-ის დეტერმინისტული ხეშ-კოდი Places.UrlHashCode სვეტში ინახება და ჩანაწერის ხეშით მოსაძებნად გამოიყენება.
    //ალგორითმი Crawler პროექტის GetDeterministicHashCode-ის იდენტურია — string.GetHashCode-ისგან განსხვავებით
    //პროცესის ხელახალი გაშვებისას მნიშვნელობა არ იცვლება
    public static int GetDeterministicHashCode(this string str)
    {
        unchecked
        {
            int hash1 = (5381 << 16) + 5381;
            int hash2 = hash1;

            for (int i = 0; i < str.Length; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ str[i];
                if (i == str.Length - 1)
                {
                    break;
                }

                hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
            }

            return hash1 + hash2 * 1566083941;
        }
    }
}
