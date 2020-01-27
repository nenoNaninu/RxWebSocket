using System.Runtime.CompilerServices;

namespace UniWebSocket.Validations
{
    public static class ValidationUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrEmpty<T>(this T[] array) => array == null || array.Length == 0;

        /// <summary>
        /// returns false for bad input.
        /// </summary>
        /// <param name="value">The value to be validated</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ValidateInput(string value) =>!string.IsNullOrWhiteSpace(value);

        /// <summary>
        /// returns false for bad input.
        /// </summary>
        /// <param name="value">The value to be validated</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ValidateInput(byte[] value) => !value.IsNullOrEmpty();

        /// <summary>
        /// returns false for bad input.
        /// </summary>
        /// <param name="value">The value to be validated</param>
        /// <param name="name">Input parameter name</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ValidateInput<T>(T value) => !Equals(value, default(T));
    }
}