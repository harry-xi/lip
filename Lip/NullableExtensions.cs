namespace Lip;

public static class NullableExtensions
{
    public static T DefaultIfNull<T>(this T? value) where T : class
    {
        return value ?? default!;
    }

    public static T DefaultIfNull<T>(this T? value) where T : struct
    {
        return value ?? default;
    }
}
