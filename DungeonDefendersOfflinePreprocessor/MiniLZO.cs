using System;

namespace DungeonDefendersOfflinePreprocessor
{
	// MiniLZO decompression
	public static class MiniLZO
	{
		public const int LZO_E_OK = 0;
		public const int LZO_E_ERROR = -1;
		public const int LZO_E_OUT_OF_MEMORY = -2;
		public const int LZO_E_NOT_COMPRESSIBLE = -3;
		public const int LZO_E_INPUT_OVERRUN = -4;
		public const int LZO_E_OUTPUT_OVERRUN = -5;
		public const int LZO_E_LOOKBEHIND_OVERRUN = -6;
		public const int LZO_E_EOF_NOT_FOUND = -7;
		public const int LZO_E_INPUT_NOT_CONSUMED = -8;
		public const int LZO_E_NOT_YET_IMPLEMENTED = -9;

		public static int Decompress(byte[] input, int inputLength, byte[] output, ref int outputLength)
		{
			int inputPos = 0;
			int outputPos = 0;
			int maxOutputPos = output.Length;
			int matchPos = 0;
			int t = 0;

			// Check for empty input
			if (inputLength <= 0)
			{
				outputLength = 0;
				return LZO_E_OK;
			}

			// --- Initial Byte Handling ---
			// The first byte requires special handling to determine the initial state.
			// It determines if we start with a literal run or skip directly to trailing literals.
			bool skipToTrailing = false;

			if (input[inputPos] > 17)
			{
				t = input[inputPos++] - 17;
				if (t < 4)
				{
					// Special Case: Go directly to trailing literal handling
					skipToTrailing = true;
				}
				else
				{
					if (outputPos + t > maxOutputPos)
						return LZO_E_OUTPUT_OVERRUN;

					do
					{
						output[outputPos++] = input[inputPos++];
					} while (--t > 0);

					// Read first instruction byte for the loop
					t = input[inputPos++];
				}
			}
			else
			{
				t = input[inputPos++];
			}

			// --- Main Decompression Loop ---
			while (true)
			{
				// If we aren't in the special "skip" state, we process Literals and Matches
				if (!skipToTrailing)
				{
					// 1. Process Literal Run (if t < 16)
					if (t < 16)
					{
						// Zero run handling (long literal runs)
						if (t == 0)
						{
							while (input[inputPos] == 0)
							{
								t += 255;
								inputPos++;
							}
							t += 15 + input[inputPos++];
						}

						// Check bounds
						if (outputPos + t + 3 > maxOutputPos) return LZO_E_OUTPUT_OVERRUN;
						if (inputPos + t + 3 > inputLength) return LZO_E_INPUT_OVERRUN;

						// Copy literals
						// The loop is unrolled for 3 bytes then variable rest
						for (int i = 0; i < t + 3; i++)
						{
							output[outputPos++] = input[inputPos++];
						}

						// Read next instruction
						t = input[inputPos++];

						// 1b. Handle M2 Short Match (Specific case inside literal run)
						if (t < 16)
						{
							matchPos = outputPos - 1 - 0x0800;
							matchPos -= t >> 2;
							matchPos -= input[inputPos++] << 2;

							if (matchPos < 0) return LZO_E_LOOKBEHIND_OVERRUN;
							if (outputPos + 3 > maxOutputPos) return LZO_E_OUTPUT_OVERRUN;

							output[outputPos++] = output[matchPos++];
							output[outputPos++] = output[matchPos++];
							output[outputPos++] = output[matchPos];

							// After M2 Short Match, we are done with this cycle.
							// We explicitly skip the standard match block (t >= 16) 
							// and fall through to "Trailing Literals".
							goto MatchDone; // Internal jump to skip the `if (t >= 16)` block below
						}
					}

					// 2. Process Standard Match (t >= 16)
					// Note: If we performed M2 above, t is < 16, so this block is skipped naturally.
					if (t >= 16)
					{
						// M2 Match
						if (t >= 64)
						{
							matchPos = outputPos - 1;
							matchPos -= (t >> 2) & 7;
							matchPos -= input[inputPos++] << 3;
							t = (t >> 5) - 1;
						}
						// M3 Match
						else if (t >= 32)
						{
							t &= 31;
							if (t == 0)
							{
								while (input[inputPos] == 0)
								{
									t += 255;
									inputPos++;
								}
								t += 31 + input[inputPos++];
							}

							matchPos = outputPos - 1;
							matchPos -= (input[inputPos] >> 2) + (input[inputPos + 1] << 6);
							inputPos += 2;
						}
						// M4 Match (Potential EOF)
						else // t >= 16
						{
							matchPos = outputPos;
							matchPos -= (t & 8) << 11;
							t &= 7;

							if (t == 0)
							{
								while (input[inputPos] == 0)
								{
									t += 255;
									inputPos++;
								}
								t += 7 + input[inputPos++];
							}

							matchPos -= (input[inputPos] >> 2) + (input[inputPos + 1] << 6);
							inputPos += 2;

							if (matchPos == outputPos)
							{
								// EOF Found
								break; // Break the main while(true) loop
							}
							matchPos -= 0x4000;
						}

						// Validate Match
						if (matchPos < 0) return LZO_E_LOOKBEHIND_OVERRUN;
						if (outputPos + t + 2 > maxOutputPos) return LZO_E_OUTPUT_OVERRUN;

						// Copy Match
						output[outputPos++] = output[matchPos++];
						output[outputPos++] = output[matchPos++];

						do
						{
							output[outputPos++] = output[matchPos++];
						} while (--t > 0);
					}
				}

			MatchDone:
				// Reset skip flag if it was set
				skipToTrailing = false;

				// 3. Process Trailing Literals (match_next logic)
				// This extracts the number of trailing literals from the last instruction byte.
				// Note: inputPos - 2 here refers to the instruction byte index relative to current pos.
				t = input[inputPos - 2] & 3;

				if (t == 0)
				{
					// No trailing literals, read next command and loop
					t = input[inputPos++];
					continue;
				}

				// Copy trailing literals (1-3 bytes)
				if (outputPos + t > maxOutputPos) return LZO_E_OUTPUT_OVERRUN;
				if (inputPos + t > inputLength) return LZO_E_INPUT_OVERRUN;

				output[outputPos++] = input[inputPos++];
				if (t > 1)
				{
					output[outputPos++] = input[inputPos++];
					if (t > 2)
					{
						output[outputPos++] = input[inputPos++];
					}
				}

				// Read next command for next iteration
				t = input[inputPos++];
			}

			// EOF Found Exit Point
			outputLength = outputPos;

			return (inputPos == inputLength) ? LZO_E_OK :
				   (inputPos < inputLength) ? LZO_E_INPUT_NOT_CONSUMED : LZO_E_INPUT_OVERRUN;
		}
	}
}