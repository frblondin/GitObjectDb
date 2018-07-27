using GitObjectDb.Attributes;
using GitObjectDb.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Text
{
    /// <summary>
    /// A set of methods for instances of <see cref="StringBuilder"/>.
    /// </summary>
    internal static class StringBuilderExtensions
    {
        /// <summary>
        /// Gets the slices included in the source according to the <paramref name="startIndex"/> and <paramref name="count"/> parameters.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="count">The count.</param>
        /// <returns>All the slices matching providing <paramref name="startIndex"/> and <paramref name="count"/> parameters.</returns>
        internal static IEnumerable<Slice> GetSlices(this StringBuilder source, int startIndex = 0, int count = -1)
        {
            if (startIndex < 0)
            {
                startIndex = 0;
            }

            if (startIndex > source.Length)
            {
                yield break;
            }

            if (count < 0 || count > source.Length + startIndex)
            {
                count = source.Length - startIndex;
            }

            var chunks = ForEachSliceChunk(source, startIndex, count);
            foreach (var chunk in chunks)
            {
                yield return new Slice(chunk.Data.Chars, chunk.IndexInChunk, chunk.Count);
            }
        }

        /// <summary>
        /// Returns the reversed (natural order) list of chunks for the given range of values.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="count">The count.</param>
        /// <returns>All chunks in natural order.</returns>
        static List<ManageSliceActionItem> ForEachSliceChunk(StringBuilder source, int startIndex, int count )
        {
            var result = new List<ManageSliceActionItem>();
            var data = ChunkData.Get(source);
            int sourceEndIndex = startIndex + count;
            int curDestIndex = count;
            while (curDestIndex > 0)
            {
                int chunkEndIndex = sourceEndIndex - data.Offset;
                if (chunkEndIndex >= 0)
                {
                    if (chunkEndIndex > data.Length)
                    {
                        chunkEndIndex = data.Length;
                    }

                    int countLeft = curDestIndex;
                    int chunkCount = countLeft;
                    int chunkStartIndex = chunkEndIndex - countLeft;
                    if (chunkStartIndex < 0)
                    {
                        chunkCount += chunkStartIndex;
                        chunkStartIndex = 0;
                    }
                    curDestIndex -= chunkCount;

                    if (chunkCount > 0)
                    {
                        result.Insert(0, new ManageSliceActionItem(data, chunkStartIndex, chunkCount));
                    }
                }
                if (curDestIndex == 0)
                {
                    break;
                }

                data = ChunkData.Get(data.Previous);
            }
            return result;
        }

        static ChunkData FindChunkForIndex(this StringBuilder source, int index)
        {
            var chunk = ChunkData._findChunkForIndexImpl(source, index);
            return ChunkData.Get(chunk);
        }

        static ChunkData NextChunk(this StringBuilder source, StringBuilder chunk)
        {
            var result = ChunkData._nextChunkImpl(source, chunk);
            return ChunkData.Get(result);
        }

        /// <summary>
        /// Copies the content of the stringbuilder to an array.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="bufferOffset">The buffer offset.</param>
        /// <param name="count">The count.</param>
        /// <param name="positionInStringBuilder">The position in string builder.</param>
        /// <param name="remainingByteToRead">The remaining byte to read.</param>
        /// <param name="encoding">The encoding.</param>
        /// <returns>Number of copied values.</returns>
        /// <exception cref="ArgumentNullException">buffer</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// bufferOffset
        /// or
        /// count
        /// </exception>
        internal static int CopyTo(this StringBuilder source, byte[] buffer, int bufferOffset, int count, ref int positionInStringBuilder, long remainingByteToRead, Encoding encoding)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (bufferOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferOffset));
            }
            if (count < 0 || buffer.Length - bufferOffset < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var num = remainingByteToRead;
            if (num > count)
            {
                num = count;
            }
            if (num <= 0)
            {
                return 0;
            }

            var updatedOffset = bufferOffset;
            var remainingBytes = num;
            var remainingMaxChars = encoding.GetMaxCharCount((int)num);
            while (remainingBytes > 0)
            {
                var chunk = source.FindChunkForIndex(positionInStringBuilder);
                var indexInChunk = positionInStringBuilder - chunk.Offset;
                if (chunk.Length - indexInChunk <= 0)
                {
                    chunk = source.NextChunk(chunk.Chunk);
                    indexInChunk = 0;
                }
                var countInChunk = Math.Min(remainingMaxChars, chunk.Length - indexInChunk);
                var byteLength = encoding.GetByteCount(chunk.Chars, indexInChunk, countInChunk);
                while (byteLength > remainingBytes)
                {
                    byteLength = encoding.GetByteCount(chunk.Chars, indexInChunk, --countInChunk);
                }

                var byteCount = encoding.GetBytes(chunk.Chars, indexInChunk, countInChunk, buffer, updatedOffset);
                updatedOffset += byteCount;
                remainingBytes -= byteCount;
                positionInStringBuilder += countInChunk;
                remainingMaxChars -= countInChunk;
            }
            return (int)num;
        }

        #region Private structures
#pragma warning disable SA1600 // Elements must be documented
        [StructLayout(LayoutKind.Sequential)]
        [ExcludeFromGuardForNull]
        private struct ChunkData
        {
            static readonly ConstructorInfo _constructor = ExpressionReflector.GetConstructor(() => new ChunkData(default, default, default, default, default));

            static readonly FieldInfo _chunkCharsField = typeof(StringBuilder).GetField("m_ChunkChars", BindingFlags.Instance | BindingFlags.NonPublic);
            static readonly FieldInfo _chunkOffsetField = typeof(StringBuilder).GetField("m_ChunkOffset", BindingFlags.Instance | BindingFlags.NonPublic);
            static readonly FieldInfo _chunkLengthField = typeof(StringBuilder).GetField("m_ChunkLength", BindingFlags.Instance | BindingFlags.NonPublic);
            static readonly FieldInfo _chunkPreviousField = typeof(StringBuilder).GetField("m_ChunkPrevious", BindingFlags.Instance | BindingFlags.NonPublic);
            static readonly MethodInfo _findChunkForIndexMethod = typeof(StringBuilder).GetMethod("FindChunkForIndex", BindingFlags.Instance | BindingFlags.NonPublic);
            static readonly MethodInfo _nextChunkMethod = typeof(StringBuilder).GetMethod("Next", BindingFlags.Instance | BindingFlags.NonPublic);

            static readonly ParameterExpression _stringBuilderArg = Expression.Parameter(typeof(StringBuilder));
            static readonly ParameterExpression _indexArg = Expression.Parameter(typeof(int));
            static readonly ParameterExpression _chunkArg = Expression.Parameter(typeof(StringBuilder));

            static readonly Func<StringBuilder, ChunkData> _chunkDataFactory =
                Expression.Lambda<Func<StringBuilder, ChunkData>>(
                    Expression.New(_constructor,
                        _stringBuilderArg,
                        Expression.Field(_stringBuilderArg, _chunkCharsField),
                        Expression.Field(_stringBuilderArg, _chunkOffsetField),
                        Expression.Field(_stringBuilderArg, _chunkLengthField),
                        Expression.Field(_stringBuilderArg, _chunkPreviousField)),
                    _stringBuilderArg).Compile();

            internal static readonly FindChunkForIndex _findChunkForIndexImpl =
                Expression.Lambda<FindChunkForIndex>(
                    Expression.Call(_stringBuilderArg, _findChunkForIndexMethod, _indexArg),
                    _stringBuilderArg, _indexArg).Compile();

            internal static readonly Func<StringBuilder, StringBuilder, StringBuilder> _nextChunkImpl =
                Expression.Lambda<Func<StringBuilder, StringBuilder, StringBuilder>>(
                    Expression.Call(_stringBuilderArg, _nextChunkMethod, _chunkArg),
                    _stringBuilderArg, _chunkArg).Compile();

#pragma warning disable S1144 // Unused private types or members should be removed
            ChunkData(StringBuilder chunk, char[] chars, int offset, int length, StringBuilder previous)
            {
                Chunk = chunk;
                Chars = chars;
                Offset = offset;
                Length = length;
                Previous = previous;
            }
#pragma warning restore S1144 // Unused private types or members should be removed

            internal delegate StringBuilder FindChunkForIndex(StringBuilder source, int index);

            public StringBuilder Chunk { get; }

            public char[] Chars { get; }

            public int Offset { get; }

            public int Length { get; }

            public StringBuilder Previous { get; }

            internal static ChunkData Get(StringBuilder chunk)
            {
                return _chunkDataFactory(chunk);
            }
        }

        [DebuggerDisplay("Data={Data.Chunk}")]
        [StructLayout(LayoutKind.Sequential)]
        [ExcludeFromGuardForNull]
        private struct ManageSliceActionItem
        {
            public readonly ChunkData Data;
            public readonly int IndexInChunk;
            public readonly int Count;

            public ManageSliceActionItem(ChunkData data, int indexInChunk, int count)
            {
                Data = data;
                IndexInChunk = indexInChunk;
                Count = count;
            }
        }

        /// <summary>
        /// Contains a slice of a <see cref="StringBuilder"/>.
        /// </summary>
        [ExcludeFromGuardForNull]
        internal struct Slice
        {
            internal readonly char[] _values;
            internal readonly int _indexInChunk;
            internal readonly int _count;

            internal Slice(char[] values, int indexInChunk, int count)
            {
                _values = values;
                _indexInChunk = indexInChunk;
                _count = count;
            }
#pragma warning restore SA1600 // Elements must be documented
        }
        #endregion
    }
}