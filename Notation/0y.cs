using System;
using System.Collections.Generic;

namespace GoogologyExpander
{
	public static class ZeroY
	{
		/// <summary>
		/// 对 0-Y 序列进行一次展开。
		/// 约定：
		/// - 空序列展开为空数组。
		/// - 末项为 0 时删除末项。
		/// - 一阶阶差序列末项为 1 时按 PrSS 展开。
		/// - 否则按照山脉图展开（要求最高阶末项与其父项差值为 1，否则视为非法输入）。
		/// </summary>
		public static int[] Expand0Y(int[] sequence)
		{
			if (sequence == null)
				throw new ArgumentNullException(nameof(sequence));

			if (sequence.Length == 0)
				return Array.Empty<int>();

			int lastIndex = sequence.Length - 1;
			int lastValue = sequence[lastIndex];

			// 末项为 0：删除末项
			if (lastValue == 0)
			{
				int[] result = new int[lastIndex];
				Array.Copy(sequence, 0, result, 0, lastIndex);
				return result;
			}

			// 计算一阶阶差序列，若其末项为 1，则按 PrSS 展开
			int[] firstDiff = ComputeFirstDiff(sequence);
			if (firstDiff[lastIndex] == 1)
			{
				return PrSS.ExpandPrSS(sequence);
			}

			// 否则进入山脉图展开
			return ExpandByMountain(sequence);
		}

		/// <summary>
		/// 计算一阶阶差序列（只用于判断是否需要 PrSS 展开）。
		/// 对于原序列，元素的父项为左边第一个小于它的项；阶差为元素值减去父项值（无父项则为自身）。
		/// </summary>
		private static int[] ComputeFirstDiff(int[] seq)
		{
			int n = seq.Length;
			int[] diff = new int[n];
			for (int i = 0; i < n; i++)
			{
				int parent = FindParent(seq, i);
				diff[i] = (parent == -1) ? seq[i] : seq[i] - seq[parent];
			}
			return diff;
		}

		/// <summary>
		/// 在原序列中查找索引 i 的父项：左边第一个小于它的项，无则返回 -1。
		/// </summary>
		private static int FindParent(int[] seq, int i)
		{
			int value = seq[i];
			for (int j = i - 1; j >= 0; j--)
			{
				if (seq[j] < value)
					return j;
			}
			return -1;
		}

		/// <summary>
		/// 判断在祖先链（包括自身）中，ancestor 是否为 descendant 的祖先。
		/// parents 为对应行的父项索引数组。
		/// </summary>
		private static bool IsAncestor(int[] parents, int ancestor, int descendant)
		{
			int current = descendant;
			while (current != -1)
			{
				if (current == ancestor)
					return true;
				current = parents[current];
			}
			return false;
		}

		/// <summary>
		/// 根据当前行的值和父项，计算下一行的值和父项。
		/// 下一行的值：当前值 - 父项值（无父项则为当前值）。
		/// 下一行的父项：下一行中左边第一个值小于当前元素，且其正下方项（当前行同列）是当前行当前元素祖先的项。
		/// </summary>
		private static void ComputeNextRow(int[] currentValues, int[] currentParents,
										   out int[] nextValues, out int[] nextParents)
		{
			int n = currentValues.Length;
			nextValues = new int[n];
			nextParents = new int[n];

			// 先计算下一行的值
			for (int c = 0; c < n; c++)
			{
				int p = currentParents[c];
				nextValues[c] = (p == -1) ? currentValues[c] : currentValues[c] - currentValues[p];
			}

			// 计算下一行的父项
			for (int c = 0; c < n; c++)
			{
				int parent = -1;
				int value = nextValues[c];
				// 在下一行中从右向左找第一个满足条件的（“第一个在它左边”即从左向右第一个，等价于从右向左最后一个但需谨慎，这里从左向右遍历到 c-1 即可）
				for (int p = c - 1; p >= 0; p--)
				{
					if (nextValues[p] < value &&
						IsAncestor(currentParents, p, c))
					{
						parent = p;
						break;
					}
				}
				nextParents[c] = parent;
			}
		}

		/// <summary>
		/// 山脉图展开。
		/// </summary>
		private static int[] ExpandByMountain(int[] seq)
		{
			int n = seq.Length;

			// 构建各阶阶差序列，直到某一阶末项与其父项差值为 1
			var rows = new List<int[]>();
			var parents = new List<int[]>();

			// 第 0 行
			int[] row0 = (int[])seq.Clone();
			int[] par0 = new int[n];
			for (int c = 0; c < n; c++)
				par0[c] = FindParent(seq, c);

			rows.Add(row0);
			parents.Add(par0);

			int H = -1;
			while (true)
			{
				int r = rows.Count - 1;
				int lastVal = rows[r][n - 1];
				int lastParent = parents[r][n - 1];

				if (lastParent == -1)
					throw new ArgumentException("非法 0-Y 序列：最高阶末项不存在父项，无法展开。", nameof(seq));

				if (lastVal - rows[r][lastParent] == 1)
				{
					H = r;
					break;
				}

				ComputeNextRow(rows[r], parents[r], out int[] nextVals, out int[] nextPars);
				rows.Add(nextVals);
				parents.Add(nextPars);
			}

			// 根元素列：最高阶末项的父项
			int rootCol = parents[H][n - 1];
			int goodLen = rootCol + 1;          // 好部列数（包含根列）
			int badLen = n - goodLen;           // 坏部列数
			int newLen = goodLen + 2 * badLen;  // 展开后总列数（未删除末项前）

			// 构建最高阶 H 行的新值（保留全部列）
			int[] newH = new int[newLen];
			// 好部
			Array.Copy(rows[H], 0, newH, 0, goodLen);
			// 原始坏部
			Array.Copy(rows[H], goodLen, newH, goodLen, badLen);
			// 最高阶末项减 1
			newH[n - 1] = rows[H][n - 1] - 1;
			// 复制坏部（复制减 1 后的原始坏部）
			Array.Copy(newH, goodLen, newH, goodLen + badLen, badLen);

			// 存储所有行的新值（行数 H+1）
			var newRows = new List<int[]>(H + 1);
			newRows.Add(newH);

			// 从 H-1 行向下逐行计算
			for (int r = H - 1; r >= 0; r--)
			{
				int[] newRow = new int[newLen];
				// 复制好部和原始坏部
				Array.Copy(rows[r], 0, newRow, 0, n);
				// 计算复制坏部（列 goodLen + badLen 到 goodLen + 2*badLen - 1）
				int[] upperRow = newRows[H - r - 1]; // 上一行（r+1 行）的新值，注意 newRows 索引：newRows[0] 是 H 行，newRows[1] 是 H-1 行，以此类推
													 // 实际上 newRows 列表顺序是 H, H-1, H-2, ..., 0，所以上一行对应 newRows[H - r - 1] 即 newRows[H - r - 1] 是 r+1 行
				for (int offset = 0; offset < badLen; offset++)
				{
					int cOrig = goodLen + offset;
					int cNew = goodLen + badLen + offset;

					int parentOrig = parents[r][cOrig];
					int leftParentCol;
					if (parentOrig == -1)
					{
						leftParentCol = -1;
					}
					else if (parentOrig < goodLen)
					{
						leftParentCol = parentOrig;          // 父项在好部，固定不变
					}
					else
					{
						leftParentCol = parentOrig + badLen; // 父项在坏部，平移到复制坏部
					}

					int value = upperRow[cNew];
					if (leftParentCol != -1)
						value += newRow[leftParentCol];

					newRow[cNew] = value;
				}
				newRows.Add(newRow);
			}

			// 删除除最高阶外所有行的末项（即每行长度减 1）
			int finalLen = newLen - 1;
			int[] finalSeq = new int[finalLen];
			Array.Copy(newRows[newRows.Count - 1], 0, finalSeq, 0, finalLen); // 最后添加的是第 0 行

			return finalSeq;
		}
	}
}