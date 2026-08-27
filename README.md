# GoogologyExpander
使用C#重新编写的googology展开器
部分代码由AI生成
使用GPLv3
## 接口规范
### 统一记法展开接口规范

**版本：** v1.0  
**日期：** 2026-08-27

---

## 通用

| 约定项 | 规则 |
|--------|------|
| 命名空间 | 全部位于 `GoogologyExpander` |
| 类形态 | 全部为 `public static class`，方法为静态方法，禁止需要实例化的入口 |
| 类名 | 与记法名保持一致（如 `PrSS`、`BMS`、`UPMS`、`WY`、`EY`） |
| 方法名 | 统一为 `Expand<记法名>`（如 `ExpandPrSS`、`ExpandBMS`） |
| 展开语义 | 每次调用展开一步（一维）或 n 步（二维），不改变调用方传入之外的全局状态 |
| 参数名 | 一维统一为 `sequence`，二维统一为 `matrix`、步数统一为 `n` |

---

## 一维接口规范

适用于输入为一维整数数组的记法：PrSS、HPrSS、LPrSS、0-Y、Y、WY、EY。

### 标准签名

```csharp
public static int[] Expand<记法名>(int[] sequence)
