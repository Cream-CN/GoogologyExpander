using System;

namespace GoogologyExpander
{
	public static class PrSS
	{
		/// <summary>
		/// 对 PrSS 序列进行一次展开。
		/// 末项为 0 时删去末项；末项大于 0 时删去末项，并将坏部复制一次。
		/// 因此末项大于 0 时结果包含两个坏部。
		/// </summary>
		public static int[] ExpandPrSS(int[] sequence)
		{
			if (sequence == null)
				throw new ArgumentNullException(nameof(sequence));

			if (sequence.Length == 0)
				return Array.Empty<int>();

			int lastIndex = sequence.Length - 1;
			int lastValue = sequence[lastIndex];

			// 末项为 0：直接删去末项
			if (lastValue == 0)
			{
				int[] result = new int[lastIndex];
				Array.Copy(sequence, 0, result, 0, lastIndex);
				return result;
			}

			// 末项大于 0：从右向左寻找第一个小于末项的元素作为坏根
			int badRoot = -1;
			for (int i = lastIndex - 1; i >= 0; i--)
			{
				if (sequence[i] < lastValue)
				{
					badRoot = i;
					break;
				}
			}

			if (badRoot == -1)
				throw new ArgumentException("非法 PrSS 序列：末项大于 0 但之前没有小于它的坏根。", nameof(sequence));

			int goodLength = badRoot;              // 好部：0 .. badRoot-1
			int badLength = lastIndex - badRoot;   // 坏部：badRoot .. lastIndex-1

			// 展开一次：好部 + 坏部 + 坏部
			int[] expanded = new int[goodLength + badLength * 2];

			if (goodLength > 0)
				Array.Copy(sequence, 0, expanded, 0, goodLength);

			Array.Copy(sequence, badRoot, expanded, goodLength, badLength);
			Array.Copy(sequence, badRoot, expanded, goodLength + badLength, badLength);

			return expanded;
		}
	}
}