/**
 * Dashboard 组件注册与导出系统
 *
 * 该文件负责：
 * 1. 导出所有 Dashboard 组件
 * 2. 导出类型定义
 * 3. 提供组件定义映射（用于设计器组件库）
 * 4. 提供 Vue 插件安装函数
 */

import type { App, Component } from 'vue'
import type { WidgetDefinition } from './types'

// ============================================================================
// 组件导入
// ============================================================================

import DashboardChart from './DashboardChart.vue'
import DashboardTable from './DashboardTable.vue'
import DashboardCard from './DashboardCard.vue'
import DashboardStatusLight from './DashboardStatusLight.vue'
import DashboardGauge from './DashboardGauge.vue'
import DashboardProgress from './DashboardProgress.vue'

// ============================================================================
// 组件导出
// ============================================================================

export { DashboardChart }
export { DashboardTable }
export { DashboardCard }
export { DashboardStatusLight }
export { DashboardGauge }
export { DashboardProgress }

// ============================================================================
// 类型导出
// ============================================================================

export * from './types'

// ============================================================================
// 组件定义映射（用于设计器组件库）
// ============================================================================

/**
 * 组件定义列表
 * 包含所有可用组件的元信息，用于设计器组件面板
 */
export const widgetDefinitions: WidgetDefinition[] = [
  {
    type: 'table',
    name: '数据表格',
    icon: 'Table',
    category: '基础组件',
    description: '展示结构化数据，支持分页、排序和条件样式',
    defaultSize: { width: 6, height: 4 },
    minSize: { width: 3, height: 2 },
    maxSize: { width: 12, height: 10 },
    configSchema: {
      type: 'object',
      properties: {
        columns: {
          type: 'array',
          title: '列配置',
          items: {
            type: 'object',
            properties: {
              field: { type: 'string', title: '字段名' },
              title: { type: 'string', title: '列标题' },
              width: { type: 'number', title: '列宽' }
            }
          }
        },
        bordered: { type: 'boolean', title: '显示边框', default: true },
        striped: { type: 'boolean', title: '斑马纹', default: true },
        showPagination: { type: 'boolean', title: '显示分页', default: true },
        overflowMode: {
          type: 'string',
          title: '溢出处理',
          enum: ['auto-page', 'scroll', 'none'],
          default: 'none'
        }
      }
    }
  },
  {
    type: 'stat-card',
    name: '统计卡片',
    icon: 'DataLine',
    category: '基础组件',
    description: '展示关键指标数值，支持趋势和比较',
    defaultSize: { width: 3, height: 2 },
    minSize: { width: 2, height: 1 },
    maxSize: { width: 6, height: 4 },
    configSchema: {
      type: 'object',
      properties: {
        title: { type: 'string', title: '标题' },
        valueField: { type: 'string', title: '数值字段' },
        prefix: { type: 'string', title: '前缀' },
        suffix: { type: 'string', title: '后缀' },
        icon: { type: 'string', title: '图标' },
        iconColor: { type: 'string', title: '图标颜色' }
      }
    }
  },
  {
    type: 'progress-bar',
    name: '进度条',
    icon: 'TrendCharts',
    category: '基础组件',
    description: '展示进度或比例数据',
    defaultSize: { width: 4, height: 1 },
    minSize: { width: 2, height: 1 },
    maxSize: { width: 8, height: 2 },
    configSchema: {
      type: 'object',
      properties: {
        valueField: { type: 'string', title: '数值字段' },
        maxField: { type: 'string', title: '最大值字段' },
        showLabel: { type: 'boolean', title: '显示标签', default: true },
        strokeWidth: { type: 'number', title: '线条宽度', default: 20 },
        color: { type: 'string', title: '进度条颜色' }
      }
    }
  },
  {
    type: 'status-light',
    name: '状态指示灯',
    icon: 'WarnTriangleFilled',
    category: '基础组件',
    description: '根据数据值显示不同状态颜色',
    defaultSize: { width: 2, height: 1 },
    minSize: { width: 1, height: 1 },
    maxSize: { width: 4, height: 2 },
    configSchema: {
      type: 'object',
      properties: {
        valueField: { type: 'string', title: '数值字段' },
        label: { type: 'string', title: '标签' },
        showValue: { type: 'boolean', title: '显示数值', default: true },
        size: { type: 'string', title: '尺寸', enum: ['small', 'medium', 'large'], default: 'medium' }
      }
    }
  },
  {
    type: 'chart-bar',
    name: '柱状图',
    icon: 'Histogram',
    category: '图表组件',
    description: '展示分类数据的对比分析',
    defaultSize: { width: 6, height: 4 },
    minSize: { width: 4, height: 3 },
    maxSize: { width: 12, height: 8 },
    configSchema: {
      type: 'object',
      properties: {
        title: { type: 'string', title: '图表标题' },
        xField: { type: 'string', title: 'X 轴字段' },
        yFields: { type: 'array', title: 'Y 轴字段', items: { type: 'string' } },
        showTooltip: { type: 'boolean', title: '显示提示', default: true },
        showLegend: { type: 'boolean', title: '显示图例', default: true }
      }
    }
  },
  {
    type: 'chart-line',
    name: '折线图',
    icon: 'TrendCharts',
    category: '图表组件',
    description: '展示数据随时间或类别的变化趋势',
    defaultSize: { width: 6, height: 4 },
    minSize: { width: 4, height: 3 },
    maxSize: { width: 12, height: 8 },
    configSchema: {
      type: 'object',
      properties: {
        title: { type: 'string', title: '图表标题' },
        xField: { type: 'string', title: 'X 轴字段' },
        yFields: { type: 'array', title: 'Y 轴字段', items: { type: 'string' } },
        smooth: { type: 'boolean', title: '平滑曲线', default: false },
        showArea: { type: 'boolean', title: '显示面积', default: false }
      }
    }
  },
  {
    type: 'chart-pie',
    name: '饼图',
    icon: 'PieChart',
    category: '图表组件',
    description: '展示数据的占比分布',
    defaultSize: { width: 4, height: 4 },
    minSize: { width: 3, height: 3 },
    maxSize: { width: 8, height: 8 },
    configSchema: {
      type: 'object',
      properties: {
        title: { type: 'string', title: '图表标题' },
        nameField: { type: 'string', title: '名称字段' },
        valueField: { type: 'string', title: '数值字段' },
        showLabel: { type: 'boolean', title: '显示标签', default: true },
        radius: { type: 'array', title: '半径', default: ['40%', '70%'] }
      }
    }
  },
  {
    type: 'gauge',
    name: '仪表盘',
    icon: 'Odometer',
    category: '图表组件',
    description: '展示单个数值在范围内的位置',
    defaultSize: { width: 3, height: 3 },
    minSize: { width: 2, height: 2 },
    maxSize: { width: 6, height: 6 },
    configSchema: {
      type: 'object',
      properties: {
        valueField: { type: 'string', title: '数值字段' },
        min: { type: 'number', title: '最小值', default: 0 },
        max: { type: 'number', title: '最大值', default: 100 },
        unit: { type: 'string', title: '单位' },
        title: { type: 'string', title: '标题' }
      }
    }
  }
]

// ============================================================================
// 组件类型映射
// ============================================================================

/**
 * 组件类型到组件的映射
 * 用于根据类型字符串动态渲染组件
 */
export const componentMap: Record<string, Component> = {
  // 表格组件
  'table': DashboardTable,

  // 卡片组件
  'stat-card': DashboardCard,
  'card-number': DashboardCard,

  // 进度组件
  'progress-bar': DashboardProgress,
  'progress': DashboardProgress,

  // 状态指示灯
  'status-light': DashboardStatusLight,
  'status': DashboardStatusLight,

  // 图表组件
  'chart-bar': DashboardChart,
  'chart-line': DashboardChart,
  'chart-pie': DashboardChart,
  'chart-area': DashboardChart,
  'chart-scatter': DashboardChart,

  // 仪表盘
  'gauge': DashboardGauge
}

/**
 * 根据组件类型获取组件
 * @param type 组件类型
 * @returns Vue 组件或 undefined
 */
export function getComponentByType(type: string): Component | undefined {
  return componentMap[type]
}

/**
 * 根据组件类型获取组件定义
 * @param type 组件类型
 * @returns 组件定义或 undefined
 */
export function getDefinitionByType(type: string): WidgetDefinition | undefined {
  return widgetDefinitions.find(def => def.type === type)
}

/**
 * 获取指定分类的所有组件定义
 * @param category 分类名称
 * @returns 组件定义数组
 */
export function getDefinitionsByCategory(category: string): WidgetDefinition[] {
  return widgetDefinitions.filter(def => def.category === category)
}

/**
 * 获取所有组件分类
 * @returns 分类名称数组
 */
export function getCategories(): string[] {
  return [...new Set(widgetDefinitions.map(def => def.category))]
}

// ============================================================================
// Vue 插件安装函数
// ============================================================================

/**
 * Vue 插件安装函数
 * 用于全局注册所有 Dashboard 组件
 *
 * @example
 * ```ts
 * import { createApp } from 'vue'
 * import { installDashboardComponents } from '@/components/dashboard'
 *
 * const app = createApp(App)
 * installDashboardComponents(app)
 * ```
 */
export function installDashboardComponents(app: App): void {
  // 注册所有组件
  app.component('DashboardChart', DashboardChart)
  app.component('DashboardTable', DashboardTable)
  app.component('DashboardCard', DashboardCard)
  app.component('DashboardStatusLight', DashboardStatusLight)
  app.component('DashboardGauge', DashboardGauge)
  app.component('DashboardProgress', DashboardProgress)
}

/**
 * Vue 插件对象
 * 支持app.use()方式安装
 *
 * @example
 * ```ts
 * import { createApp } from 'vue'
 * import DashboardPlugin from '@/components/dashboard'
 *
 * const app = createApp(App)
 * app.use(DashboardPlugin)
 * ```
 */
export default {
  install: installDashboardComponents
}

// ============================================================================
// 组件分类常量
// ============================================================================

/**
 * 组件分类枚举
 */
export const WIDGET_CATEGORIES = {
  BASIC: '基础组件',
  CHART: '图表组件',
  ADVANCED: '高级组件',
  CUSTOM: '自定义组件'
} as const

// ============================================================================
// 默认导出
// ============================================================================

/**
 * 获取组件默认配置
 * @param type 组件类型
 * @returns 默认配置对象
 */
export function getDefaultConfig(type: string): Record<string, unknown> {
  const definition = getDefinitionByType(type)
  if (!definition?.configSchema?.properties) {
    return {}
  }

  const config: Record<string, unknown> = {}
  const properties = definition.configSchema.properties as Record<string, { default?: unknown }>

  for (const [key, prop] of Object.entries(properties)) {
    if (prop.default !== undefined) {
      config[key] = prop.default
    }
  }

  return config
}
