using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Text
{
    internal static class StringBuilderExtensions
    {
        #region Private structures
        [StructLayout(LayoutKind.Sequential)]
        private struct ChunkData
        {
            static readonly Func<StringBuilder, ChunkData> _chunkDataFactory;
            internal delegate StringBuilder FindChunkForIndex(StringBuilder source, int index);
            internal static readonly FindChunkForIndex _findChunkForIndexImpl;
            internal static readonly Func<StringBuilder, StringBuilder, StringBuilder> _nextChunkImpl;
            delegate void UpdateChunkData(StringBuilder source, char[] chars, int offset, int length, StringBuilder previous);

            static ChunkData()
            {
                var chunkCharsField = typeof(StringBuilder).GetField("m_ChunkChars", BindingFlags.Instance | BindingFlags.NonPublic);
                var chunkOffsetField = typeof(StringBuilder).GetField("m_ChunkOffset", BindingFlags.Instance | BindingFlags.NonPublic);
                var chunkLengthField = typeof(StringBuilder).GetField("m_ChunkLength", BindingFlags.Instance | BindingFlags.NonPublic);
                var chunkPreviousField = typeof(StringBuilder).GetField("m_ChunkPrevious", BindingFlags.Instance | BindingFlags.NonPublic);
                var findChunkForIndexMethod = typeof(StringBuilder).GetMethod("FindChunkForIndex", BindingFlags.Instance | BindingFlags.NonPublic);
                var nextChunkMethod = typeof(StringBuilder).GetMethod("Next", BindingFlags.Instance | BindingFlags.NonPublic);

                var stringBuilderArg = Expression.Parameter(typeof(StringBuilder));
                var chunkDataFactoryExpression = Expression.Lambda<Func<StringBuilder, ChunkData>>(
                    Expression.New(_constructor,
                        stringBuilderArg,
                        Expression.Field(stringBuilderArg, chunkCharsField),
                        Expression.Field(stringBuilderArg, chunkOffsetField),
                        Expression.Field(stringBuilderArg, chunkLengthField),
                        Expression.Field(stringBuilderArg, chunkPreviousField)),
                    stringBuilderArg);
                _chunkDataFactory = chunkDataFactoryExpression.Compile();

                var indexArg = Expression.Parameter(typeof(int));
                var findChunkForIndexExpression = Expression.Lambda<FindChunkForIndex>(
                    Expression.Call(stringBuilderArg, findChunkForIndexMethod, indexArg),
                    stringBuilderArg, indexArg);
                _findChunkForIndexImpl = findChunkForIndexExpression.Compile();

                var chunkArg = Expression.Parameter(typeof(StringBuilder));
                var nextChunkExpression = Expression.Lambda<Func<StringBuilder, StringBuilder, StringBuilder>>(
                    Expression.Call(stringBuilderArg, nextChunkMethod, chunkArg),
                    stringBuilderArg, chunkArg);
                _nextChunkImpl = nextChunkExpression.Compile();
            }

            internal static ChunkData Get(StringBuilder chunk)
            {
                return _chunkDataFactory(chunk);
            }

            public StringBuilder Chunk;
            public char[] Chars;
            public int Offset;
            public int Length;
            public StringBuilder Previous;

            private static readonly ConstructorInfo _constructor = typeof(ChunkData).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Single();
            private ChunkData(StringBuilder chunk, char[] chars, int offset, int length, StringBuilder previous)
            {
                this.Chunk = chunk;
                this.Chars = chars;
                this.Offset = offset;
                this.Length = length;
                this.Previous = previous;
            }
        }
        //[DebuggerDisplay("Data={Data.Chunk}")]
        [StructLayout(LayoutKind.Sequential)]
        private struct ManageSliceActionItem
        {
            public ChunkData Data; public int IndexInChunk; public int Count;
            public ManageSliceActionItem(ChunkData data, int indexInChunk, int count)
            {
                this.Data = data;
                this.IndexInChunk = indexInChunk;
                this.Count = count;
            }
        }
        #endregion

        internal struct Slice
        {
            public readonly char[] Values;
            public readonly int IndexInChunk;
            public readonly int Count;

            public Slice(char[] values, int indexInChunk, int count)
            {
                this.Values = values;
                this.IndexInChunk = indexInChunk;
                this.Count = count;
            }
        }
        internal static IEnumerable<Slice> GetSlices(this StringBuilder source, int startIndex = 0, int count = -1)
        {
            if (startIndex < 0) startIndex = 0;
            if (startIndex > source.Length) yield break;
            if (count < 0 || count > source.Length + startIndex)
                count = source.Length - startIndex;

            var chunks = ForEachSliceChunk(source, startIndex, count);
            foreach (var chunk in chunks)
                yield return new Slice(chunk.Data.Chars, chunk.IndexInChunk, chunk.Count);
        }

        /// <summary>
        /// Returns the reversed (natural order) list of chunks for the given range of values.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="count">The count.</param>
        /// <param name="reversedEnumerable">enumerate in reverse order (by default the enumerable of chunk is in reversed order)</param>
        /// <returns></returns>
        static List<ManageSliceActionItem> ForEachSliceChunk(StringBuilder source, int startIndex, int count, bool reversedEnumerable = true)
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
                        chunkEndIndex = data.Length;

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
                    break;
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

        internal static int CopyTo(this StringBuilder source, byte[] buffer, int bufferOffset, int count, ref int positionInStringBuilder, long remainingByteToRead, Encoding encoding)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (bufferOffset < 0) throw new ArgumentOutOfRangeException(nameof(bufferOffset));
            if (count < 0 || buffer.Length - bufferOffset < count) throw new ArgumentOutOfRangeException(nameof(count));
            var num = remainingByteToRead;
            if (num > count) num = count;
            if (num <= 0) return 0;

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
    }
}