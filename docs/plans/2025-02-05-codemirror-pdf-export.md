# CodeMirror SQL 编辑器集成与 PDF 导出功能实现计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**目标:** 实现专业的 SQL 代码编辑器（带语法高亮和自动补全）和 PDF 报表导出功能（支持图表导出）

**架构:**
- CodeMirror 6 作为 SQL 编辑器，替换现有的 textarea
- 混合补全方案：常用表预加载，其他表按需加载
- jsPDF + html2canvas 实现 PDF 导出
- 图表转图片嵌入 PDF

**技术栈:**
- CodeMirror 6 (MIT)
- @codemirror/lang-sql
- @codemirror/autocomplete
- @codemirror/language-data  (SQL 语言数据)
- @codemirror/theme-one-dark (主题)
- sql-formatter (SQL 格式化)
- jsPDF (MIT)
- html2canvas (MIT)

---

## 任务概述

本实现计划分为 4 个主要任务：

1. **前端依赖安装** - 安装 CodeMirror 和 PDF 相关 npm 包
2. **CodeMirror SQL 编辑器组件** - 创建封装的 SQL 编辑器组件
3. **自动补全功能** - 实现表/字段名智能补全
4. **PDF 导出功能** - 实现格式化的报表 PDF 导出

---

## 任务 1: 前端依赖安装

**文件:**
- 修改: `frontend/package.json`

### 步骤 1: 添加 CodeMirror 依赖

**文件:** `frontend/package.json`

添加以下依赖到 `dependencies` 或 `devDependencies`:

```json
{
  "dependencies": {
    "@codemirror/view": "^6.23.0",
    "@codemirror/state": "^6.4.1",
    "@codemirror/basic-setup": "^0.21.0",
    "@codemirror/lang-sql": "^6.0.0",
    "@codemirror/autocomplete": "^6.12.0",
    "@codemirror/language": "^6.10.0",
    "@codemirror/commands": "^6.3.3",
    "@codemirror/theme-one-dark": "^6.1.2",
    "@codemirror/language-data": "^6.4.0",
    "sql-formatter": "^15.1.2",
    "jspdf": "^2.5.1",
    "html2canvas": "^1.4.1"
  }
}
```

### 步骤 2: 安装依赖

**运行:**
```bash
cd frontend
npm install
```

**预期结果:** 所有包成功安装，无错误

### 步骤 3: 提交依赖更新

```bash
git add frontend/package.json frontend/package-lock.json
git commit -m "chore: add CodeMirror and PDF export dependencies"
```

---

## 任务 2: CodeMirror SQL 编辑器组件

**文件:**
- 创建: `frontend/src/components/SqlEditor.vue`
- 修改: `frontend/src/views/report/ReportDesign.vue`

### 步骤 1: 创建 SqlEditor 组件

**文件:** `frontend/src/components/SqlEditor.vue`

```vue
<template>
  <div class="sql-editor-container">
    <codemirror
      v-model="code"
      :style="{ height: height }"
      :extensions="extensions"
      :placeholder="placeholder"
      @change="handleChange"
    />
    <div class="editor-toolbar" v-if="showToolbar">
      <el-button-group>
        <el-button size="small" @click="formatSQL" title="格式化SQL">
          <el-icon><Document /></el-icon>
          格式化
        </el-button>
        <el-button size="small" @click="clearEditor" title="清空">
          <el-icon><Delete /></el-icon>
          清空
        </el-button>
      </el-button-group>
    </div>
  </div>
</template>

<script setup>
import { ref, watch, computed, shallowRef } from 'vue'
import { EditorView, basicSetup } from 'codemirror'
import { EditorState } from '@codemirror/state'
import { sql } from '@codemirror/lang-sql'
import { oneDark } from '@codemirror/theme-one-dark'
import { keymap } from '@codemirror/commands'
import { autocompletion } from '@codemirror/autocomplete'
import { sql as sqlLanguage } from '@codemirror/language-data'
import { indentUnit } from '@codemirror/language'
import { sql as sqlFormatter } from 'sql-formatter'

const props = defineProps({
  modelValue: {
    type: String,
    default: ''
  },
  placeholder: {
    type: String,
    default: '请输入SQL查询语句...'
  },
  height: {
    type: String,
    default: '300px'
  },
  showToolbar: {
    type: Boolean,
    default: true
  },
  dataSourceId: {
    type: [String, Number],
    default: null
  }
})

const emit = defineEmits(['update:modelValue', 'change'])

const code = ref(props.modelValue)
const extensions = computed(() => {
  const exts = [
    basicSetup,
    sql(),
    oneDark,
    keymap.of([{ key: 'Mod-Enter', run: () => formatSQL() }])
  ]

  // 添加自动补全（如果提供了数据源）
  if (props.dataSourceId) {
    exts.push(autocompletion({
      override: [sqlAutocompletion(props.dataSourceId)]
    }))
  }

  return exts
})

// 监听 modelValue 变化
watch(() => props.modelValue, (newVal) => {
  code.value = newVal
})

// 监听内部 code 变化，同步到父组件
const handleChange = () => {
  emit('update:modelValue', code.value)
  emit('change', code.value)
}

// 格式化 SQL
const formatSQL = () => {
  try {
    const formatted = sqlFormatter.format(code.value)
    code.value = formatted
    emit('update:modelValue', formatted)
  } catch (error) {
    console.error('SQL 格式化失败:', error)
  }
}

// 清空编辑器
const clearEditor = () => {
  code.value = ''
  emit('update:modelValue', '')
}

// 自动补全函数（将在任务 3 中实现）
const sqlAutocompletion = (dataSourceId) => {
  return (context) => {
    // TODO: 任务 3 中实现
    return []
  }
}
</script>

<style scoped>
.sql-editor-container {
  border: 1px solid var(--el-border-color);
  border-radius: 4px;
  overflow: hidden;
}

.editor-toolbar {
  padding: 8px;
  background-color: var(--el-bg-color);
  border-top: 1px solid var(--el-border-color);
}
</style>
```

### 步骤 2: 更新 ReportDesign.vue 使用 SqlEditor

**文件:** `frontend/src/views/report/ReportDesign.vue`

**修改位置:** 第 38-59 行

**替换:**
```vue
<!-- SQL编辑器 -->
<el-card class="design-card" style="margin-top: 20px;">
  <template #header>
    <span>SQL查询</span>
  </template>
  <el-input
    v-model="form.sqlQuery"
    type="textarea"
    :rows="10"
    placeholder="请输入SQL查询语句..."
  />
```

**为:**
```vue
<!-- SQL编辑器 -->
<el-card class="design-card" style="margin-top: 20px;">
  <template #header>
    <span>SQL查询</span>
  </template>
  <SqlEditor
    v-model="form.sqlQuery"
    :data-source-id="form.dataSourceId"
    :height="'300px'"
    placeholder="请输入SQL查询语句，使用 @参数名 格式定义参数"
    @change="handleSqlChange"
  />
```

### 步骤 3: 添加组件导入

**文件:** `frontend/src/views/report/ReportDesign.vue`

在 `<script setup>` 顶部添加:

```vue
<script setup>
import SqlEditor from '@/components/SqlEditor.vue'
// ... 其他导入保持不变
```

### 步骤 4: 提交 CodeMirror 编辑器

```bash
git add frontend/src/components/SqlEditor.vue
git add frontend/src/views/report/ReportDesign.vue
git commit -m "feat: add CodeMirror SQL editor with syntax highlighting and formatting"
```

---

## 任务 3: 自动补全功能

**文件:**
- 创建: `frontend/src/components/SqlEditor.vue` (补充自动补全逻辑)
- 创建: `frontend/src/api/sqlCompletion.js`
- 修改: `backend/src/DataForgeStudio.Api/Controllers/DataSourcesController.cs` (添加获取表字段的 API)

### 步骤 1: 添加后端 API - 获取数据源的表和字段

**文件:** `backend/src/DataForgeStudio.Api/Controllers/DataSourcesController.cs`

添加新的 API 端点:

```csharp
/// <summary>
/// 获取数据源的表和字段信息（用于 SQL 自动补全）
/// </summary>
[HttpGet("{dataSourceId}/schema")]
[Authorize]
public async Task<ApiResponse<DataSourceSchemaDto>> GetDataSourceSchema(int dataSourceId)
{
    try
    {
        var schema = await _dataSourceService.GetDataSourceSchemaAsync(dataSourceId);
        return ApiResponse<DataSourceSchemaDto>.Ok(schema);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取数据源结构失败");
        return ApiResponse<DataSourceSchemaDto>.Fail("获取数据源结构失败", "GET_SCHEMA_FAILED");
    }
}
```

### 步骤 2: 创建 DataSourceSchemaDto

**文件:** `backend/src/DataForgeStudio.Core/DTO/DataSourceSchemaDto.cs`

```csharp
namespace DataForgeStudio.Core.DTO;

public class TableSchemaDto
{
    public string TableName { get; set; } = string.Empty;
    public List<FieldSchemaDto> Fields { get; set; } = new();
}

public class FieldSchemaDto
{
    public string FieldName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class DataSourceSchemaDto
{
    public List<TableSchemaDto> Tables { get; set; } = new();
    public List<string> CommonTables { get; set; } = new(); // 常用表（预加载）
}
```

### 步骤 3: 创建前端 API 接口

**文件:** `frontend/src/api/sqlCompletion.js`

```javascript
import request from './request'

export const sqlCompletionApi = {
  // 获取数据源的表和字段结构
  getSchema: (dataSourceId) => {
    return request({
      url: `/api/datasources/${dataSourceId}/schema`,
      method: 'get'
    })
  }
}
```

### 步骤 4: 实现 SqlEditor 中的自动补全逻辑

**文件:** `frontend/src/components/SqlEditor.vue`

更新 `sqlAutocompletion` 函数:

```javascript
import { sqlCompletionApi } from '@/api/sqlCompletion'

// 存储已加载的数据源结构缓存
const schemaCache = new Map()
// 常用表（预加载列表）
const commonTables = ['Users', 'Roles', 'Permissions']

// 自动补全函数
const sqlAutocompletion = (dataSourceId) => {
  return async (context) => {
    if (!dataSourceId) return []

    // 从缓存或 API 获取数据源结构
    let schema = schemaCache.get(dataSourceId)
    if (!schema) {
      try {
        const res = await sqlCompletionApi.getSchema(dataSourceId)
        if (res.success) {
          schema = res.data
          schemaCache.set(dataSourceId, schema)
        }
      } catch (error) {
        console.error('获取数据源结构失败:', error)
        return []
      }
    }

    const word = context.matchBefore(/\S*/).toLowerCase()
    const options = []

    // 补全表名 (FROM 或 JOIN 后)
    if (isFromOrJoinContext(context)) {
      schema.Tables.forEach(table => {
        if (table.TableName.toLowerCase().includes(word)) {
          options.push({
            label: table.TableName,
            type: 'table',
            boost: schema.CommonTables.includes(table.TableName) ? 2 : 1
          })
        }
      })
    }

    // 补全字段名 (SELECT 后或 WHERE 条件中)
    if (isFieldContext(context)) {
      schema.Tables.forEach(table => {
        table.Fields.forEach(field => {
          if (field.FieldName.toLowerCase().includes(word)) {
            options.push({
              label: `${table.TableName}.${field.FieldName}`,
              type: 'field',
              detail: field.DataType
            })
          }
        })
      })
    }

    return {
      from: word,
      options: options
    }
  }
}

// 判断是否在 FROM 或 JOIN 上下文中
const isFromOrJoinContext = (context) => {
  const line = context.state.doc.toString()
  const pos = context.pos

  // 简单判断：检查光标前是否有 FROM 或 JOIN 关键字
  const beforeCursor = line.substring(0, pos)
  const lastKeyword = beforeCursor.match(/\b(FROM|JOIN)\s+([^\s]*)$/i)

  return !!lastKeyword
}

// 判断是否在字段上下文（SELECT 后或 WHERE 条件中）
const isFieldContext = (context) => {
  const line = context.state.doc.toString()
  const pos = context.pos

  // 检查是否在表名之后（例如 TableName.）
  const beforeCursor = line.substring(0, pos)
  return /\.\w*$/.test(beforeCursor.substring(0, 50))
}
```

### 步骤 5: 添加依赖注入到 DataSourceService

**文件:** `backend/src/DataForgeStudio.Core/Interfaces/IDataSourceService.cs`

添加方法签名:

```csharp
Task<DataSourceSchemaDto> GetDataSourceSchemaAsync(int dataSourceId);
```

### 步骤 6: 实现 DataSourceService.GetDataSourceSchemaAsync

**文件:** `backend/src/DataForgeStudio.Core/Services/DataSourceService.cs`

```csharp
public async Task<DataSourceSchemaDto> GetDataSourceSchemaAsync(int dataSourceId)
{
    var dataSource = await _context.DataSources
        .FirstOrDefaultAsync(ds => ds.DataSourceId == dataSourceId);

    if (dataSource == null)
    {
        throw new AppException("数据源不存在", "DATASOURCE_NOT_FOUND");
    }

    var schema = new DataSourceSchemaDto
    {
        CommonTables = new List<string> { "Users", "Roles", "Permissions" }
    };

    // 连接数据库获取表结构
    var tables = await GetDatabaseTablesAsync(dataSource);

    foreach (var table in tables)
    {
        var fields = await GetTableFieldsAsync(dataSource, table);
        schema.Tables.Add(new TableSchemaDto
        {
            TableName = table,
            Fields = fields
        });
    }

    return schema;
}

private async Task<List<string>> GetDatabaseTablesAsync(DataSource dataSource)
{
    // 根据数据库类型获取表列表
    // 示例：SQL Server
    var sql = @"
        SELECT TABLE_NAME
        FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_TYPE = 'BASE TABLE'
        ORDER BY TABLE_NAME";

    // 执行查询并返回表名列表
    // ...
}

private async Task<List<FieldSchemaDto>> GetTableFieldsAsync(DataSource dataSource, string tableName)
{
    // 获取表的字段信息
    var sql = $@"
        SELECT
            COLUMN_NAME as FieldName,
            DATA_TYPE as DataType,
            IS_NULLABLE as IsNullable
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = '{tableName}'
        ORDER BY ORDINAL_POSITION";

    // 执行查询并返回字段列表
    // ...
}
```

### 步骤 7: 提交自动补全功能

```bash
git add frontend/src/components/SqlEditor.vue frontend/src/api/sqlCompletion.js
git add backend/src/DataForgeStudio.Core/DTO/DataSourceSchemaDto.cs
git add backend/src/DataForgeStudio.Core/Interfaces/IDataSourceService.cs
git add backend/src/DataForgeStudio.Core/Services/DataSourceService.cs
git add backend/src/DataForgeStudio.Api/Controllers/DataSourcesController.cs
git commit -m "feat: add SQL autocomplete with table/field suggestions"
```

---

## 任务 4: PDF 导出功能

**文件:**
- 创建: `frontend/src/utils/pdfExport.js`
- 修改: `frontend/src/views/report/ReportList.vue`

### 步骤 1: 创建 PDF 导出工具函数

**文件:** `frontend/src/utils/pdfExport.js`

```javascript
import jsPDF from 'jspdf'
import html2canvas from 'html2canvas'
import { ElMessage } from 'element-plus'

/**
 * 导出报表为 PDF
 * @param {Object} reportData - 报表数据
 * @param {Object} chartInstance - ECharts 实例（可选）
 * @param {String} fileName - 文件名
 */
export async function exportReportToPDF(reportData, chartInstance = null, fileName = 'report.pdf') {
  try {
    ElMessage.info('正在生成 PDF，请稍候...')

    // 创建 PDF 文档
    const pdf = new jsPDF({
      orientation: 'landscape',
      unit: 'mm',
      format: 'a4'
    })

    const pageWidth = pdf.internal.pageSize.getWidth()
    const pageHeight = pdf.internal.pageSize.getHeight()
    const margin = 20
    const contentWidth = pageWidth - 2 * margin
    let yPosition = margin

    // 1. 添加标题
    pdf.setFontSize(18)
    pdf.setFont('helvetica', 'bold')
    pdf.text(reportData.reportName, pageWidth / 2, yPosition, { align: 'center' })
    yPosition += 15

    // 2. 添加分类和描述
    pdf.setFontSize(12)
    pdf.setFont('helvetica', 'normal')
    pdf.text(`分类: ${reportData.reportCategory}`, margin, yPosition)
    yPosition += 7
    if (reportData.description) {
      pdf.text(`描述: ${reportData.description}`, margin, yPosition)
      yPosition += 7
    }
    yPosition += 8

    // 3. 添加参数信息（如果有）
    if (reportData.parameters && reportData.parameters.length > 0) {
      pdf.setFontSize(10)
      pdf.setFont('helvetica', 'bold')
      pdf.text('查询参数:', margin, yPosition)
      yPosition += 7

      pdf.setFont('helvetica', 'normal')
      reportData.parameters.forEach(param => {
        const value = queryForm.value[param.name] || '-'
        pdf.text(`  ${param.label}: ${value}`, margin + 5, yPosition)
        yPosition += 6
      })
      yPosition += 8
    }

    // 4. 添加生成时间
    pdf.setFontSize(8)
    pdf.setFont('helvetica', 'normal')
    pdf.text(`生成时间: ${new Date().toLocaleString('zh-CN')}`, margin, yPosition)
    yPosition += 10

    // 5. 添加表格数据
    const tableData = reportData.tableData || []
    if (tableData.length > 0) {
      // 计算列宽
      const columns = Object.keys(tableData[0])
      const columnWidth = (contentWidth - 20) / columns.length

      // 绘制表格标题
      pdf.setFontSize(10)
      pdf.setFont('helvetica', 'bold')
      pdf.setFillColor(240, 240, 240)
      pdf.rect(margin, yPosition, contentWidth, 10, 'F')

      let xPos = margin
      columns.forEach(col => {
        const cellText = String(col).substring(0, 15)
        pdf.text(cellText, xPos, yPosition + 7)
        xPos += columnWidth
      })
      yPosition += 10

      // 绘制表格内容
      pdf.setFont('helvetica', 'normal')
      tableData.forEach((row, index) => {
        // 检查是否需要新页面
        if (yPosition > pageHeight - 40) {
          pdf.addPage()
          yPosition = margin
        }

        xPos = margin
        columns.forEach(col => {
          const cellText = String(row[col] || '-').substring(0, 15)
          pdf.text(cellText, xPos, yPosition + 7)
          xPos += columnWidth
        })
        yPosition += 10
      })
      yPosition += 10
    }

    // 6. 添加图表（如果有）
    if (chartInstance && reportData.charts && reportData.charts.length > 0) {
      // 检查是否需要新页面
      if (yPosition > pageHeight - 150) {
        pdf.addPage()
        yPosition = margin
      }

      for (const chartConfig of reportData.charts) {
        const chartElement = document.getElementById(chartConfig.id)
        if (chartElement) {
          // 使用 html2canvas 将图表转换为图片
          const canvas = await html2canvas(chartElement, {
            scale: 2,
            useCORS: true,
            logging: false
          })

          const imgData = canvas.toDataURL('image/png')
          const imgWidth = contentWidth
          const imgHeight = (canvas.height * imgWidth) / canvas.width

          // 检查图片高度，如果超过一页则换页
          if (yPosition + imgHeight > pageHeight - margin) {
            pdf.addPage()
            yPosition = margin
          }

          pdf.addImage(imgData, 'PNG', margin, yPosition, imgWidth, imgHeight)
          yPosition += imgHeight + 10
        }
      }
    }

    // 7. 添加页脚
    const totalPages = pdf.internal.getNumberOfPages()
    for (let i = 1; i <= totalPages; i++) {
      pdf.setPage(i)
      pdf.setFontSize(8)
      pdf.setFont('helvetica', 'normal')
      pdf.text(
        `第 ${i} / ${totalPages} 页`,
        pageWidth / 2,
        pageHeight - 10,
        { align: 'center' }
      )
    }

    // 8. 保存 PDF
    pdf.save(fileName)

    ElMessage.success('PDF 导出成功')
    return true
  } catch (error) {
    console.error('PDF 导出失败:', error)
    ElMessage.error('PDF 导出失败: ' + error.message)
    return false
  }
}
```

### 步骤 2: 更新 ReportList.vue 集成 PDF 导出

**文件:** `frontend/src/views/report/ReportList.vue`

在 `<script setup>` 中添加导入:

```javascript
import { exportReportToPDF } from '@/utils/pdfExport'
```

**修改 handleExport 函数** (约第 243 行):

```javascript
const handleExport = async (row) => {
  currentReport.value = row
  exportDialogVisible.value = true

  // 加载报表数据和图表
  await loadReportData(row)
}

// 添加 PDF 导出函数
const handleExportPDF = async () => {
  if (!currentReport.value) return

  const chartInstances = []

  // 渲染所有图表
  if (currentReport.value.charts && currentReport.value.charts.length > 0) {
    for (const chartConfig of currentReport.value.charts) {
      try {
        const chartElement = document.getElementById(chartConfig.id)
        if (chartElement) {
          const chart = echarts.init(chartElement, chartConfig.option)
          chartInstances.push(chart)
        }
      } catch (error) {
        console.error('图表渲染失败:', error)
      }
    }
  }

  // 导出 PDF
  await exportReportToPDF(
    {
      reportName: currentReport.value.reportName,
      reportCategory: currentReport.value.reportCategory,
      description: currentReport.value.description,
      parameters: currentReport.value.parameters,
      tableData: tableData.value,
      charts: currentReport.value.charts
    },
    chartInstances.length > 0 ? chartInstances[0] : null,
    `${currentReport.value.reportName}_${new Date().getTime()}.pdf`
  )

  // 清理图表实例
  chartInstances.forEach(chart => chart.dispose())
}
```

**更新导出对话框模板** (添加 PDF 导出按钮):

```vue
<el-dialog v-model="exportDialogVisible" title="导出报表" width="600px">
  <!-- 现有内容... -->

  <template #footer>
    <span class="dialog-footer">
      <el-button @click="exportDialogVisible = false">取消</el-button>
      <el-button type="primary" @click="handleExportExcel">
        <el-icon><Download /></el-icon>
        导出Excel
      </el-button>
      <el-button type="success" @click="handleExportPDF">
        <el-icon><Document /></el-icon>
        导出PDF
      </el-button>
    </span>
  </template>
</el-dialog>
```

### 步骤 3: 提交 PDF 导出功能

```bash
git add frontend/src/utils/pdfExport.js
git add frontend/src/views/report/ReportList.vue
git commit -m "feat: add formatted PDF export with chart support"
```

---

## 完成检查清单

完成后验证以下功能：

### CodeMirror 编辑器
- [ ] SQL 语法高亮正常显示
- [ ] 关键字颜色正确
- [ ] 格式化按钮正常工作
- [ ] 清空按钮正常工作
- [ ] 编辑器与表单双向绑定正常

### 自动补全
- [ ] 输入表名时显示补全建议
- [ ] 常用表优先显示
- [ ] 输入字段名时显示补全建议
- [ ] 补全包含表名.字段名格式
- [ ] 缓存机制正常工作

### PDF 导出
- [ ] PDF 包含报表标题
- [ ] PDF 包含分类和描述
- [ ] PDF 包含查询参数（如有）
- [ ] PDF 包含数据表格（带样式）
- [ ] PDF 包含图表（转图片）
- [ ] 表格分页正常
- [ ] 页码显示正确
- [ ] 中文内容正常显示
- [ ] 文件下载成功

---

## 测试步骤

### CodeMirror 测试
1. 打开报表设计页面
2. 在 SQL 编辑器中输入 SQL 语句
3. 验证语法高亮
4. 点击格式化按钮验证格式化
5. 输入 `SELECT * FROM` 触发自动补全
6. 验证表名和字段名补全建议

### PDF 导出测试
1. 查看一个带图表的报表
2. 点击导出按钮
3. 选择 PDF 导出
4. 验证生成的 PDF 包含所有内容
5. 验证图表以图片形式嵌入
6. 验证表格样式保留
7. 验证分页正常

---

## 部署注意事项

1. **npm 包体积**: CodeMirror 包体积较大，生产环境建议使用 `npm run build` 进行优化
2. **中文字体**: PDF 导出需要中文字体支持，建议配置字体嵌入
3. **图表性能**: html2canvas 转换大图表可能较慢，建议添加加载提示
4. **浏览器兼容**: html2canvas 在某些浏览器中可能有兼容性问题

---

## 已知限制

1. CodeMirror 自动补全目前仅支持表名和字段名，不支持 SQL 函数补全
2. PDF 导出中表格宽度自动计算，复杂表格可能需要手动调整
3. 图表转图片需要图表已渲染完成
4. 超长表格的分页可能需要进一步优化
