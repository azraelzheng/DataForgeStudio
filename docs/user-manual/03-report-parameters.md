# 报表参数配置指南

## 概述

报表参数是报表设计中的重要功能，允许用户在运行时动态输入查询条件，从而生成不同的报表结果。本文档介绍如何在报表设计中配置和使用参数。

## 参数语法

在 SQL 查询语句中，使用 `@` 符号定义参数：

```sql
-- 示例：带参数的查询
SELECT * FROM Orders
WHERE OrderDate >= @StartDate
  AND OrderDate <= @EndDate
  AND CustomerId = @CustomerId
```

**规则：**
- 参数名以 `@` 开头
- 参数名只能包含字母、数字和下划线
- 参数名不区分大小写（SQL Server）

## 参数配置界面

### 字段说明

| 字段 | 说明 | 必填 |
|------|------|------|
| 参数名 | SQL 中使用的参数标识符（如 `StartDate`） | 是 |
| 显示名称 | 在查询界面显示的标签文字 | 是 |
| 数据类型 | 参数的数据类型 | 是 |
| 默认值 | 可选的默认值 | 否 |

### 数据类型

| 类型 | 说明 | 示例 |
|------|------|------|
| 字符串 (String) | 文本类型参数 | `'ABC123'` |
| 数字 (Number) | 数值类型参数 | `100`, `3.14` |
| 日期 (DateTime) | 日期时间类型 | `2024-01-01` |
| 下拉选择 (Select) | 预定义选项列表 | 见下方说明 |

## 使用步骤

### 1. 编写带参数的 SQL

在 SQL 编辑器中编写包含参数的查询语句：

```sql
SELECT
    OrderId,
    OrderDate,
    CustomerName,
    TotalAmount
FROM Orders
WHERE OrderDate BETWEEN @StartDate AND @EndDate
ORDER BY OrderDate DESC
```

### 2. 解析参数

点击 **"解析SQL"** 按钮，系统会自动识别 SQL 中的所有参数并添加到参数列表中。

### 3. 配置参数属性

对每个参数设置：

- **显示名称**：用户友好的标签（如 "开始日期"、"结束日期"）
- **数据类型**：选择合适的类型
- **默认值**：可选，设置常用默认值

### 4. 测试查询

点击 **"测试查询"** 按钮验证参数配置是否正确。如果设置了默认值，测试将使用默认值执行。

## 高级用法

### 下拉选择参数

当参数类型为 "下拉选择" 时，用户可以从预定义的选项中选择值。

**配置方式：**
1. 设置数据类型为 "下拉选择"
2. 在默认值中填写选项，每行一个选项：
```
选项1
选项2
选项3
```

### 日期参数格式

日期参数支持以下格式：
- `YYYY-MM-DD`（推荐）
- `YYYY-MM-DD HH:mm:ss`
- `YYYY/MM/DD`

### 空值处理

如果参数可能为空，建议在 SQL 中使用 `ISNULL` 或 `COALESCE` 处理：

```sql
SELECT * FROM Products
WHERE CategoryId = ISNULL(@CategoryId, CategoryId)
```

或使用 `OR` 条件：

```sql
SELECT * FROM Products
WHERE (@CategoryId IS NULL OR CategoryId = @CategoryId)
```

## 示例场景

### 场景1：日期范围查询

**SQL：**
```sql
SELECT * FROM Sales
WHERE SaleDate BETWEEN @StartDate AND @EndDate
```

**参数配置：**
| 参数名 | 显示名称 | 数据类型 | 默认值 |
|--------|----------|----------|--------|
| StartDate | 开始日期 | DateTime | 当月1日 |
| EndDate | 结束日期 | DateTime | 今天 |

### 场景2：多条件筛选

**SQL：**
```sql
SELECT * FROM Employees
WHERE Department = @Department
  AND Status = @Status
  AND HireDate >= @HireDate
```

**参数配置：**
| 参数名 | 显示名称 | 数据类型 | 默认值 |
|--------|----------|----------|--------|
| Department | 部门 | String | 全部 |
| Status | 状态 | Select | 在职 |
| HireDate | 入职日期 | DateTime | |

### 场景3：模糊搜索

**SQL：**
```sql
SELECT * FROM Customers
WHERE CustomerName LIKE '%' + @Keyword + '%'
   OR ContactPhone LIKE '%' + @Keyword + '%'
```

**参数配置：**
| 参数名 | 显示名称 | 数据类型 | 默认值 |
|--------|----------|----------|--------|
| Keyword | 搜索关键词 | String | |

## 常见问题

### Q: 参数解析后没有出现在列表中？

**A:** 确保：
1. 参数名格式正确（`@参数名`）
2. 参数名只包含字母、数字和下划线
3. 点击了 "解析SQL" 按钮

### Q: 日期参数查询报错？

**A:** 检查：
1. 日期格式是否正确
2. 数据库中的日期字段类型是否匹配
3. 尝试使用标准格式 `YYYY-MM-DD`

### Q: 数字参数被当成字符串处理？

**A:** 确保：
1. 参数的数据类型设置为 "数字"
2. SQL 中不要给数字参数加引号

## 注意事项

1. **SQL 注入防护**：系统会自动对参数值进行转义处理，防止 SQL 注入攻击
2. **参数数量限制**：建议单个报表参数不超过 10 个，以保持查询界面简洁
3. **性能考虑**：参数过多可能影响查询性能，请合理设计查询条件
4. **测试验证**：发布报表前务必使用 "测试查询" 功能验证参数配置

## 相关文档

- [报表设计指南](./02-user-guide.md)
- [SQL 查询优化建议](./04-sql-optimization.md)
