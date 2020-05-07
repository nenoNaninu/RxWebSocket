using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RxWebSocket.Validations
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
        public static bool ValidateInput(string value) => !string.IsNullOrWhiteSpace(value);

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ValidateInput(ref ArraySegment<byte> value) => value.Count != 0;

        /// <summary>
        /// returns false for bad input.
        /// </summary>
        /// <param name="value">The value to be validated</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ValidateInput(ArraySegment<byte> value) => value.Count != 0;

        /// <summary>
        /// returns false for bad input.
        /// </summary>
        /// <param name="value">The value to be validated</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ValidateInput<T>(T value) => !EqualityComparer<T>.Default.Equals(value, default);
    }
}