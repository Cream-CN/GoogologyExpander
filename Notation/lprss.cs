using System;
using System.Linq;
namespace GoogologyExpander
{
	public static class LPrSS
	{
		/// <summary>
		/// 对 LPrSS 序列展开 1 次。
		/// 输入/输出均为一维整数数组。
		/// </summary>
		public static int[] ExpandLPrSS(int[] sequence)
		{
			ArgumentNullException.ThrowIfNull(sequence);

			// 规则(1)：空序列
			if (sequence.Length == 0)
				return [];

			int last = sequence[^1];

			// 规则(2)：(# , 1) = (#) + 1
			if (last == 1)
			{
				// # 是去掉末尾 1 后的前缀；整体每个元素 + 1
				return sequence[..^1]
					.Select(x => x + 1)
					.ToArray();
			}

			// 规则(3)
			// 从右向左找到第一个小于末项的元素作为坏根
			int badRootIndex = -1;
			for (int i = sequence.Length - 2; i >= 0; i--)
			{
				if (sequence[i] < last)
				{
					badRootIndex = i;
					break;
				}
			}

			if (badRootIndex == -1)
				throw new InvalidOperationException("序列不满足展开条件：未找到坏根。");

			int badRoot = sequence[badRootIndex];
			int constant = last - badRoot - 1; // 阶差减一

			int[] goodPart = sequence[..badRootIndex];
			int[] badPart = sequence[badRootIndex..^1];
			int[] copiedBadPart = badPart
				.Select(x => x + constant)
				.ToArray();

			return [.. goodPart, .. badPart, .. copiedBadPart];
		}
	}
}