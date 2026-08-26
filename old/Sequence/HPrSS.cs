using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogologyExpander
{
	public static class GoogologyExpander
	{
		/// <summary>
		/// PrSS 的一次展开。
		/// </summary>
		public static List<int> ExpandPrSS(List<int> sequence)
		{
			if (sequence == null || sequence.Count == 0)
				return new List<int>();

			int n = sequence.Count;
			int last = sequence[n - 1];

			// 寻找末项的父项：从右往左第一个小于末项的元素。
			int root = -1;
			for (int i = n - 2; i >= 0; i--)
			{
				if (sequence[i] < last)
				{
					root = i;
					break;
				}
			}

			if (root < 0)
				return sequence.Take(n - 1).ToList();

			var good = sequence.Take(root).ToList();
			var bad = sequence.Skip(root).Take(n - 1 - root).ToList();
			int delta = last - sequence[root] - 1;

			var result = new List<int>(good);
			for (int c = 0; c <= delta; c++)
				result.AddRange(bad);

			return result;
		}

		/// <summary>
		/// HPrSS 的一次展开。
		/// </summary>
		public static List<int> ExpandHPrSS(List<int> sequence)
		{
			if (sequence == null || sequence.Count == 0)
				return new List<int>();

			int n = sequence.Count;

			// 规则 (2)：原序列末项为 1。
			if (sequence[n - 1] == 1)
				return sequence.Take(n - 1).ToList();

			// 计算原序列父项。
			int[] parents0 = ComputeParents(sequence);

			// 计算阶差序列。
			var diffValues = new List<int>(n);
			for (int i = 0; i < n; i++)
			{
				int p = parents0[i];
				diffValues.Add(p >= 0 ? sequence[i] - sequence[p] : sequence[i]);
			}

			// 计算阶差序列的父项。
			int[] diffParents = ComputeDiffParents(diffValues, parents0);

			// 规则 (3)：阶差序列末项为 1。
			if (diffValues[n - 1] == 1)
				return ExpandPrSS(sequence);

			// 规则 (4)：按山脉图展开。
			int root = diffParents[n - 1];
			if (root < 0)
				throw new InvalidOperationException("山脉图的根元素不存在。");

			int badLen = n - 1 - root;
			if (badLen <= 0)
				throw new InvalidOperationException("山脉图的坏部为空。");

			// 顶部阶差序列：末项减一。
			var top = new int[n];
			for (int i = 0; i < n; i++)
				top[i] = diffValues[i];
			top[n - 1] -= 1;

			int topLen = n + badLen;
			var topValues = new int[topLen];

			// 保留原顶部。
			for (int i = 0; i < n; i++)
				topValues[i] = top[i];

			// 复制坏部到末尾。
			for (int offset = 0; offset < badLen; offset++)
				topValues[n + offset] = top[root + 1 + offset];

			// 原序列最后一项被删除，所以底部长度为顶部长度减一。
			int bottomLen = topLen - 1;
			var newParents = new int[bottomLen];

			// 原序列前 n-1 项父项不变。
			for (int i = 0; i < n - 1; i++)
				newParents[i] = parents0[i];

			// 复制的坏部各项父项。
			for (int offset = 0; offset < badLen; offset++)
			{
				int newCol = n - 1 + offset;
				int source = root + 1 + offset;
				int p = parents0[source];

				if (p < 0)
				{
					newParents[newCol] = -1;
				}
				else if (p <= root)
				{
					// 左腿连接到好部，保持不变。
					newParents[newCol] = p;
				}
				else
				{
					// 左腿在坏部内部，随坏部平移。
					newParents[newCol] = n - 1 + (p - root - 1);
				}
			}

			// 从左至右计算底部序列。
			var bottomValues = new int[bottomLen];
			for (int col = 0; col < bottomLen; col++)
			{
				// 原序列保留列与顶部同列对齐；
				// 新增列从底部索引 n-1 开始，对应顶部索引 col+1。
				int topIndex = col < n - 1 ? col : col + 1;
				int p = newParents[col];
				int parentValue = p >= 0 ? bottomValues[p] : 0;
				bottomValues[col] = topValues[topIndex] + parentValue;
			}

			return bottomValues.ToList();
		}

		/// <summary>
		/// 计算每个元素在原始序列中的父项：左边第一个小于它的项。
		/// </summary>
		private static int[] ComputeParents(List<int> values)
		{
			var parents = new int[values.Count];

			for (int i = 0; i < values.Count; i++)
			{
				parents[i] = -1;
				for (int j = i - 1; j >= 0; j--)
				{
					if (values[j] < values[i])
					{
						parents[i] = j;
						break;
					}
				}
			}

			return parents;
		}

		/// <summary>
		/// 计算阶差序列中每个元素的父项。
		/// </summary>
		private static int[] ComputeDiffParents(List<int> diffValues, int[] lowerParents)
		{
			int n = diffValues.Count;
			var parents = new int[n];

			for (int i = 0; i < n; i++)
			{
				parents[i] = -1;
				for (int j = i - 1; j >= 0; j--)
				{
					if (diffValues[j] < diffValues[i] && IsAncestor(lowerParents, j, i))
					{
						parents[i] = j;
						break;
					}
				}
			}

			return parents;
		}

		/// <summary>
		/// 判断 candidate 是否为 target 在给定父项关系下的祖先项。
		/// </summary>
		private static bool IsAncestor(int[] parents, int candidate, int target)
		{
			int current = target;
			while (current >= 0)
			{
				if (current == candidate)
					return true;

				current = parents[current];
			}

			return false;
		}
	}
}