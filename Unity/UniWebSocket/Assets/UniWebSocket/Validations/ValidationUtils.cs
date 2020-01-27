using System;
using System.Runtime.CompilerServices;
using UniWebSocket.Exceptions;

namespace UniWebSocket.Validations
{
    internal static class ValidationUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNullOrEmpty<T>(this T[] array)
        {
            return array == null || array.Length == 0;
        }

        /// <summary>
        /// It throws <exception cref="WebSocketBadInputException"></exception> if value is null or empty/white spaces
        /// </summary>
        /// <param name="value">The value to be validated</param>
        /// <param name="name">Input parameter name</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateInput(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new WebSocketBadInputException($"Input string parameter '{name}' is null or empty. Please correct it.");
            }
        }

        /// <summary>
        /// It throws <exception cref="WebSocketBadInputException"></exception> if value is null or 0 Length.
        /// </summary>
        /// <param name="value">The value to be validated</param>
        /// <param name="name">Input parameter name</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateInput(byte[] value, string name)
        {
            if (value.IsNullOrEmpty())
            {
                throw new WebSocketBadInputException($"Input parameter '{name}' is null or 0 Length. Please correct it.");
            }
        }

        /// <summary>
        /// It throws <exception cref="WebSocketBadInputException"></exception> if value is null
        /// </summary>
        /// <param name="value">The value to be validated</param>
        /// <param name="name">Input parameter name</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateInput<T>(T value, string name)
        {
            if (Equals(value, default(T)))
            {
                throw new WebSocketBadInputException($"Input parameter '{name}' is null. Please correct it.");
            }
        }
    }
}