import { autocompletion, CompletionContext } from '@codemirror/autocomplete'

// 防抖延迟（毫秒）
const DEBOUNCE_DELAY = 250
// 防抖定时器
let debounceTimer = null
// 上次补全的上下文
let lastCompletionContext = null

// 预定义的常见 SQL 关键字
const commonKeywords = [
  'SELECT', 'FROM', 'WHERE', 'JOIN', 'INNER JOIN', 'LEFT JOIN', 'RIGHT JOIN', 'FULL JOIN',
  'ON', 'AND', 'OR', 'NOT', 'IN', 'LIKE', 'BETWEEN', 'IS NULL', 'IS NOT NULL',
  'GROUP BY', 'HAVING', 'ORDER BY', 'ASC', 'DESC', 'LIMIT', 'OFFSET',
  'INSERT INTO', 'VALUES', 'UPDATE', 'SET', 'DELETE FROM',
  'CREATE TABLE', 'ALTER TABLE', 'DROP TABLE',
  'UNION', 'UNION ALL', 'DISTINCT', 'AS', 'CASE WHEN THEN END',
  'COUNT', 'SUM', 'AVG', 'MIN', 'MAX', 'ROUND', 'CAST', 'CONVERT'
]

// 预定义的常见表名（系统表）
const commonTables = [
  { label: 'Users', columns: ['UserId', 'Username', 'Email', 'IsSystem', 'CreatedTime'] },
  { label: 'Roles', columns: ['RoleId', 'RoleName', 'RoleCode', 'IsSystem', 'CreatedTime'] },
  { label: 'Reports', columns: ['ReportId', 'ReportName', 'SqlStatement', 'CreatedTime'] },
  { label: 'DataSources', columns: ['DataSourceId', 'DataSourceName', 'ServerAddress', 'DatabaseName'] }
]

// 缓存已加载的表结构（使用 Promise 避免竞态条件）
const tableCache = new Map()
// 缓存正在进行的请求，避免重复请求
const pendingRequests = new Map()

/**
 * 创建 SQL 自动补全扩展
 * @param {Function} fetchTables - 异步函数，从后端获取表结构
 * @param {number} dataSourceId - 数据源 ID
 */
export function createSqlAutocomplete(fetchTables, dataSourceId = null) {
  return autocompletion({
    override: [async (context) => {
      // 确保在 SQL 上下文中
      if (!context.explicit && !isSQLContext(context)) {
        return null
      }

      const word = context.matchBefore(/\w*/)
      if (!word || (word.from === word.to && !context.explicit)) {
        return null
      }

      // 创建补全上下文的唯一标识
      const contextKey = `${context.pos}_${word.text}_${dataSourceId}`

      // 如果是相同的上下文，直接返回缓存的结果
      if (lastCompletionContext && lastCompletionContext.key === contextKey && lastCompletionContext.result) {
        return lastCompletionContext.result
      }

      // 清除之前的定时器
      if (debounceTimer) {
        clearTimeout(debounceTimer)
      }

      // 对于显式触发（Ctrl+Space），不需要防抖
      if (context.explicit) {
        const result = await fetchCompletionOptions(context, word, fetchTables, dataSourceId)
        lastCompletionContext = { key: contextKey, result }
        return result
      }

      // 返回一个 Promise，在延迟后执行
      return new Promise((resolve) => {
        debounceTimer = setTimeout(async () => {
          const result = await fetchCompletionOptions(context, word, fetchTables, dataSourceId)
          lastCompletionContext = { key: contextKey, result }
          resolve(result)
        }, DEBOUNCE_DELAY)
      })
    }]
  })
}

/**
 * 获取补全选项
 */
async function fetchCompletionOptions(context, word, fetchTables, dataSourceId) {
  // 获取当前词之前的文本，判断上下文
  const lineText = context.state.doc.lineAt(context.pos).text
  const beforeCursor = lineText.substring(0, context.pos - lineText.from)

  // 判断补全类型
  const completionType = detectCompletionType(beforeCursor)

  let options = []

  switch (completionType) {
    case 'keyword':
      options = getKeywordOptions(word)
      break
    case 'table':
      options = await getTableOptions(word, fetchTables, dataSourceId)
      break
    case 'column':
      options = await getColumnOptions(word, beforeCursor, fetchTables, dataSourceId)
      break
    default:
      // 混合模式：关键字 + 表名
      const keywordOpts = getKeywordOptions(word)
      const tableOpts = await getTableOptions(word, fetchTables, dataSourceId)
      options = [...keywordOpts, ...tableOpts]
  }

  if (options.length === 0) {
    return null
  }

  return {
    from: word.from,
    options: options
  }
}

/**
 * 检测补全类型
 */
function detectCompletionType(beforeCursor) {
  const upperText = beforeCursor.toUpperCase().trim()

  // 检测列名：FROM 或 JOIN 后面，且有点号
  if (/\b(FROM|JOIN)\s+(\w+)\.\w*$/.test(upperText)) {
    return 'column'
  }

  // 检测表名：FROM 或 JOIN 后面
  if (/\b(FROM|JOIN)\s*\w*$/.test(upperText)) {
    return 'table'
  }

  // 检测列名：WHERE 后，且之前有表名
  if (/\bWHERE\s+[\w\s=\.]+$/.test(upperText)) {
    return 'column'
  }

  // 默认为关键字
  if (/^\s*\w*$/.test(upperText) || /,\s*\w*$/.test(upperText)) {
    return 'keyword'
  }

  return 'mixed'
}

/**
 * 判断是否在 SQL 上下文中
 */
function isSQLContext(context) {
  const lineText = context.state.doc.lineAt(context.pos).text
  const upperText = lineText.toUpperCase()

  // 包含 SQL 关键字
  const sqlKeywords = ['SELECT', 'FROM', 'WHERE', 'INSERT', 'UPDATE', 'DELETE', 'CREATE', 'ALTER']
  return sqlKeywords.some(kw => upperText.includes(kw))
}

/**
 * 获取关键字补全选项
 */
function getKeywordOptions(word) {
  const filtered = commonKeywords.filter(kw =>
    kw.toUpperCase().startsWith(word.text.toUpperCase())
  )

  return filtered.map(kw => ({
    label: kw,
    type: 'keyword',
    detail: 'SQL 关键字',
    info: kw
  }))
}

/**
 * 获取表名补全选项
 */
async function getTableOptions(word, fetchTables, dataSourceId) {
  let tables = [...commonTables]

  // 如果有数据源 ID，尝试从后端获取表结构
  if (fetchTables && dataSourceId) {
    try {
      const cacheKey = `ds_${dataSourceId}`

      // 如果已有缓存，直接使用
      if (tableCache.has(cacheKey)) {
        tables = tableCache.get(cacheKey)
      } else if (pendingRequests.has(cacheKey)) {
        // 如果有正在进行的请求，等待其完成
        tables = await pendingRequests.get(cacheKey)
      } else {
        // 创建新请求并缓存 Promise
        const promise = fetchTables(dataSourceId).then(result => {
          const tables = result || []
          tableCache.set(cacheKey, tables)
          pendingRequests.delete(cacheKey)
          return tables
        }).catch(error => {
          pendingRequests.delete(cacheKey)
          console.warn('获取表结构失败，使用默认表:', error)
          return []
        })
        pendingRequests.set(cacheKey, promise)
        tables = await promise
      }
    } catch (error) {
      console.warn('获取表结构失败，使用默认表:', error)
    }
  }

  const filtered = tables.filter(t =>
    t.label.toUpperCase().startsWith(word.text.toUpperCase())
  )

  return filtered.map(t => ({
    label: t.label,
    type: 'class',
    detail: '表',
    info: t.columns ? `列: ${t.columns.join(', ')}` : ''
  }))
}

/**
 * 获取列名补全选项
 */
async function getColumnOptions(word, beforeCursor, fetchTables, dataSourceId) {
  // 从上下文中提取表名
  const tableMatch = beforeCursor.match(/(\w+)\.\w*$/i)
  if (!tableMatch) {
    return []
  }

  const tableName = tableMatch[1]
  let tables = commonTables

  if (fetchTables && dataSourceId) {
    try {
      const cacheKey = `ds_${dataSourceId}`

      // 如果已有缓存，直接使用
      if (tableCache.has(cacheKey)) {
        tables = tableCache.get(cacheKey)
      } else if (pendingRequests.has(cacheKey)) {
        // 如果有正在进行的请求，等待其完成
        tables = await pendingRequests.get(cacheKey)
      } else {
        // 创建新请求并缓存 Promise
        const promise = fetchTables(dataSourceId).then(result => {
          const tables = result || []
          tableCache.set(cacheKey, tables)
          pendingRequests.delete(cacheKey)
          return tables
        }).catch(error => {
          pendingRequests.delete(cacheKey)
          console.warn('获取表结构失败:', error)
          return []
        })
        pendingRequests.set(cacheKey, promise)
        tables = await promise
      }
    } catch (error) {
      console.warn('获取表结构失败:', error)
    }
  }

  const table = tables.find(t => t.label.toUpperCase() === tableName.toUpperCase())
  if (!table || !table.columns) {
    return []
  }

  const filtered = table.columns.filter(col =>
    col.toUpperCase().startsWith(word.text.toUpperCase())
  )

  return filtered.map(col => ({
    label: col,
    type: 'property',
    detail: `${tableName} 列`,
    info: col
  }))
}

/**
 * 清除缓存
 */
export function clearTableCache() {
  tableCache.clear()
  pendingRequests.clear()
}

/**
 * 预加载表结构
 */
export async function preloadTableStructure(dataSourceId, fetchTables) {
  const cacheKey = `ds_${dataSourceId}`
  if (!tableCache.has(cacheKey) && !pendingRequests.has(cacheKey)) {
    try {
      const promise = fetchTables(dataSourceId).then(result => {
        const tables = result || []
        tableCache.set(cacheKey, tables)
        pendingRequests.delete(cacheKey)
        return tables
      }).catch(error => {
        pendingRequests.delete(cacheKey)
        console.warn('预加载表结构失败:', error)
        return []
      })
      pendingRequests.set(cacheKey, promise)
      await promise
    } catch (error) {
      console.warn('预加载表结构失败:', error)
    }
  }
}
