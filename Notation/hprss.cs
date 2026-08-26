using System;

namespace GoogologyExpander
{
	public static class HPrSS
	{
		/// <summary>
		/// 对 HPrSS 序列进行一次展开。
		/// </summary>
		public static int[] ExpandHPrSS(int[] sequence)
		{
			if (sequence == null)
				throw new ArgumentNullException(nameof(sequence));

			int n = sequence.Length;
			if (n == 0)
				return Array.Empty<int>();

			// 规则 (2)：末项为 1 时，删去末项（后继展开）
			if (sequence[n - 1] == 1)
			{
				int[] res = new int[n - 1];
				Array.Copy(sequence, 0, res, 0, n - 1);
				return res;
			}

			// 1. 计算原序列父项（左侧第一个小于当前项的位置）
			int[] parent = new int[n];
			for (int i = 0; i < n; i++)
			{
				parent[i] = -1;
				for (int j = i - 1; j >= 0; j--)
				{
					if (sequence[j] < sequence[i])
					{
						parent[i] = j;
						break;
					}
				}
			}

			// 2. 计算阶差序列
			int[] diff = new int[n];
			for (int i = 0; i < n; i++)
			{
				diff[i] = sequence[i] - (parent[i] != -1 ? sequence[parent[i]] : 0);
			}

			// 规则 (3)：阶差末项为 1 时，按 PrSS 展开原序列
			if (diff[n - 1] == 1)
			{
				return PrSS.ExpandPrSS(sequence);
			}

			// 3. 计算阶差序列的父项（用于找根元素）
			int[] dp = new int[n]; // dp[i] = 阶差序列中第 i 项的父项索引
			for (int i = 0; i < n; i++)
			{
				dp[i] = -1;
				for (int j = 0; j < i; j++)
				{
					if (diff[j] < diff[i] && IsAncestor(j, i, parent))
					{
						dp[i] = j;
						break;
					}
				}
			}

			int root = dp[n - 1]; // 根元素 = 阶差末项的父项
			if (root == -1)
				throw new InvalidOperationException("HPrSS 序列无法找到根元素，无法展开。");

			// 4. 山脉图展开
			// 将阶差末项减一
			diff[n - 1] -= 1;

			// 展开后长度：好部 + (坏部-最后一项) + 坏部
			int m = 2 * n - root - 2;
			int[] result = new int[m];

			// 按从左到右的顺序计算每一项的值
			for (int i = 0; i < m; i++)
			{
				int origIndex;   // 对应原序列中的索引
				int diffVal;     // 该项的阶差值
				int parentNew;   // 在新序列中的父项索引

				if (i <= root)
				{
					// 好部
					origIndex = i;
					diffVal = diff[origIndex];
					parentNew = parent[origIndex]; // 父项映射不变
				}
				else if (i <= n - 2)
				{
					// 第一个坏部副本（去掉最后一项）
					origIndex = i;
					diffVal = diff[origIndex];
					int pOrig = parent[origIndex];
					parentNew = pOrig; // 因映射关系保持不变
				}
				else
				{
					// 第二个坏部副本（完整复制）
					int k = i - (n - 1); // 副本中的偏移
					origIndex = root + 1 + k;
					diffVal = diff[origIndex]; // 若 origIndex == n-1，则 diff 已减一

					int pOrig = parent[origIndex];
					if (pOrig == -1)
					{
						parentNew = -1;
					}
					else if (pOrig <= root)
					{
						parentNew = pOrig; // 指向好部
					}
					else
					{
						// 指向第二个副本中的对应位置
						parentNew = n - 1 + (pOrig - (root + 1));
					}
				}

				// 当前项的值 = 阶差项 + 父项的值
				int parentValue = parentNew == -1 ? 0 : result[parentNew];
				result[i] = diffVal + parentValue;
			}

			return result;
		}

		/// <summary>
		/// 判断 ancestor 是否为 descendant 的祖先（通过原序列父项链）。
		/// </summary>
		private static bool IsAncestor(int ancestor, int descendant, int[] parent)
		{
			int cur = descendant;
			while (cur != -1)
			{
				if (cur == ancestor)
					return true;
				cur = parent[cur];
			}
			return false;
		}
	}
}