﻿using System;
using System.IO;
using Reloaded.Memory.Streams;
using Reloaded.Memory.Streams.Readers;

namespace RidersArchiveTool.Utilities
{
    public class RidersArchiveGuesser
    {
        public bool TryGuess(EndianStreamReader data, int streamLength)
        {
            // This guesser works by comparing the item count in each group with running total embedded inside the file.
            var initialPos = data.Position();

            // Total Groups
            data.Read(out int binCount);

            // Safeguard against unlikely big files.
            if (binCount is > short.MaxValue or < 1)
                return false;

            // Total Items
            Span<byte> groups = stackalloc byte[binCount];
            for (int x = 0; x < binCount; x++)
                groups[x] = data.Read<byte>();

            // Alignment
            data.Seek(RoundUp((int)data.Position(), 4) - data.Position(), SeekOrigin.Current);

            // Now compare against total running file count.
            int currentCount = 0;
            int expectedCount = 0;

            for (int x = 0; x < binCount; x++)
            {
                expectedCount = data.Read<short>();
                if (currentCount != expectedCount)
                    return false;

                currentCount += groups[x];
            }

            // Skip group ids.
            data.Seek(sizeof(short) * binCount, SeekOrigin.Current);

            // Check offsets.
            var firstFileOffset = data.Peek<int>();

            if (streamLength != -1 && firstFileOffset > streamLength)
                return false;

            // Seek to expected first file position.
            data.Seek(sizeof(int) * currentCount, SeekOrigin.Current);
            data.Seek(RoundUp((int)data.Position(), 16), SeekOrigin.Begin); // Alignment

            // Try checking if first file is past expected header size, or empty 0.
            var currentOffset = data.Position() - initialPos;
            return firstFileOffset == 0 || firstFileOffset >= (currentOffset);
        }

        /// <summary>
        /// Rounds a number up to the next multiple unless the number is already a multiple.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="multiple">The multiple.</param>
        public static int RoundUp(int number, int multiple)
        {
            if (multiple == 0)
                return number;

            int remainder = number % multiple;
            if (remainder == 0)
                return number;

            return number + multiple - remainder;
        }
    }
}
