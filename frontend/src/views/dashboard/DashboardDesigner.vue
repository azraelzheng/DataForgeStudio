<template>
  <div class="dashboard-designer">
    <!-- 顶部工具栏 -->
    <div class="designer-toolbar">
      <div class="toolbar-left">
        <el-button @click="handleBack">
          <el-icon><ArrowLeft /></el-icon>
          返回
        </el-button>
        <el-divider direction="vertical" />
        <span class="dashboard-name">
          <el-input
            v-if="isEditingName"
            v-model="dashboardForm.name"
            size="small"
            style="width: 200px"
            @blur="isEditingName = false"
            @keyup.enter="isEditingName = false"
          />
          <span v-else @click="isEditingName = true" class="name-text">
            {{ dashboardForm.name || '未命名大屏' }}
            <el-icon class="edit-icon"><Edit /></el-icon>
          </span>
        </span>
      </div>
      <div class="toolbar-right">
        <el-button-group>
          <el-button @click="handlePreview">
            <el-icon><View /></el-icon>
            预览
          </el-button>
          <el-button type="primary" @click="handleSave" :loading="saving">
            <el-icon><Check /></el-icon>
            保存
          </el-button>
        </el-button-group>
      </div>
    </div>

    <!-- 主体区域 -->
    <div class="designer-body">
      <!-- 左侧组件库面板 -->
      <div class="widget-panel">
        <div class="panel-header">
          <span>组件库</span>
        </div>
        <div class="panel-content">
          <div class="widget-category">
            <div class="category-title">基础组件</div>
            <div class="widget-list">
              <div
                v-for="widget in basicWidgets"
                :key="widget.type"
                class="widget-item"
                draggable="true"
                @dragstart="handleDragStart($event, widget)"
              >
                <el-icon :size="24"><component :is="widget.icon" /></el-icon>
                <span>{{ widget.name }}</span>
              </div>
            </div>
          </div>
          <div class="widget-category">
            <div class="category-title">图表组件</div>
            <div class="widget-list">
              <div
                v-for="widget in chartWidgets"
                :key="widget.type"
                class="widget-item"
                draggable="true"
                @dragstart="handleDragStart($event, widget)"
              >
                <el-icon :size="24"><component :is="widget.icon" /></el-icon>
                <span>{{ widget.name }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 中间画布区域 -->
      <div class="canvas-container" ref="canvasContainer">
        <div
          class="canvas-wrapper"
          :style="canvasStyle"
          @dragover.prevent
          @drop="handleDrop"
        >
          <grid-layout
            v-model:layout="layout"
            :col-num="12"
            :row-height="rowHeight"
            :margin="[10, 10]"
            :is-draggable="true"
            :is-resizable="true"
            :vertical-compact="true"
            :use-css-transforms="true"
            @layout-updated="onLayoutUpdated"
          >
            <grid-item
              v-for="item in layout"
              :key="item.i"
              :x="item.x"
              :y="item.y"
              :w="item.w"
              :h="item.h"
              :i="item.i"
              @click="handleSelectWidget(item)"
              @resize="handleResize"
              :class="{ 'selected': selectedWidgetId === item.i }"
            >
              <div class="widget-content">
                <!-- 表格组件 -->
                <template v-if="item.widgetType === 'table'">
                  <div class="widget-title">{{ item.title || '表格' }}</div>
                  <div class="widget-preview table-preview">
                    <el-table :data="[]" size="small" border>
                      <el-table-column label="列1" width="80" />
                      <el-table-column label="列2" width="80" />
                      <el-table-column label="列3" width="80" />
                    </el-table>
                  </div>
                </template>

                <!-- 数字卡片组件 -->
                <template v-else-if="item.widgetType === 'card-number'">
                  <div class="widget-preview card-preview">
                    <div class="card-value">{{ item.config?.value || '1,234' }}</div>
                    <div class="card-label">{{ item.title || '数据标题' }}</div>
                  </div>
                </template>

                <!-- 进度条组件 -->
                <template v-else-if="item.widgetType === 'progress-bar'">
                  <div class="widget-title">{{ item.title || '进度' }}</div>
                  <div class="widget-preview progress-preview">
                    <el-progress :percentage="item.config?.percentage || 75" :stroke-width="20" />
                  </div>
                </template>

                <!-- 状态灯组件 -->
                <template v-else-if="item.widgetType === 'status-light'">
                  <div class="widget-preview status-preview">
                    <div class="status-light" :style="{ backgroundColor: item.config?.color || '#67c23a' }"></div>
                    <div class="status-label">{{ item.title || '状态' }}</div>
                  </div>
                </template>

                <!-- 图表组件 -->
                <template v-else-if="['chart-bar', 'chart-line', 'chart-pie', 'gauge'].includes(item.widgetType)">
                  <div class="widget-title">{{ item.title || '图表' }}</div>
                  <div class="widget-preview chart-preview">
                    <div class="chart-placeholder">
                      <el-icon :size="48"><TrendCharts /></el-icon>
                      <span>{{ getChartTypeName(item.widgetType) }}</span>
                    </div>
                  </div>
                </template>

                <!-- 删除按钮 -->
                <el-button
                  class="widget-delete-btn"
                  type="danger"
                  circle
                  size="small"
                  @click.stop="handleDeleteWidget(item)"
                >
                  <el-icon><Delete /></el-icon>
                </el-button>
              </div>
            </grid-item>
          </grid-layout>
        </div>
      </div>

      <!-- 右侧属性面板 -->
      <div class="property-panel">
        <el-tabs v-model="activeTab">
          <!-- 大屏设置 -->
          <el-tab-pane label="大屏设置" name="dashboard">
            <el-form :model="dashboardForm" label-width="80px" size="small">
              <el-form-item label="大屏名称">
                <el-input v-model="dashboardForm.name" placeholder="请输入大屏名称" />
              </el-form-item>
              <el-form-item label="描述">
                <el-input v-model="dashboardForm.description" type="textarea" :rows="3" placeholder="请输入描述" />
              </el-form-item>
              <el-form-item label="画布宽度">
                <el-input-number v-model="dashboardForm.width" :min="800" :max="3840" :step="100" />
              </el-form-item>
              <el-form-item label="画布高度">
                <el-input-number v-model="dashboardForm.height" :min="600" :max="2160" :step="100" />
              </el-form-item>
              <el-form-item label="背景颜色">
                <el-color-picker v-model="dashboardForm.backgroundColor" />
              </el-form-item>
              <el-form-item label="背景图片">
                <el-input v-model="dashboardForm.backgroundImage" placeholder="背景图片URL" />
              </el-form-item>
              <el-form-item label="主题">
                <el-radio-group v-model="dashboardForm.theme">
                  <el-radio value="dark">深色</el-radio>
                  <el-radio value="light">浅色</el-radio>
                </el-radio-group>
              </el-form-item>
              <el-form-item label="刷新间隔">
                <el-select v-model="dashboardForm.refreshInterval" style="width: 100%">
                  <el-option label="不刷新" :value="0" />
                  <el-option label="30秒" :value="30" />
                  <el-option label="1分钟" :value="60" />
                  <el-option label="5分钟" :value="300" />
                  <el-option label="10分钟" :value="600" />
                </el-select>
              </el-form-item>
            </el-form>
          </el-tab-pane>

          <!-- 组件配置 -->
          <el-tab-pane label="组件配置" name="widget" :disabled="!selectedWidget">
            <template v-if="selectedWidget">
              <el-form :model="selectedWidget" label-width="80px" size="small">
                <el-form-item label="组件标题">
                  <el-input v-model="selectedWidget.title" placeholder="请输入组件标题" />
                </el-form-item>
                <el-form-item label="绑定报表">
                  <el-select
                    v-model="selectedWidget.reportId"
                    placeholder="请选择报表"
                    style="width: 100%"
                    filterable
                    clearable
                    @change="handleReportChange"
                  >
                    <el-option
                      v-for="report in reportList"
                      :key="report.reportId"
                      :label="report.reportName"
                      :value="report.reportId"
                    />
                  </el-select>
                </el-form-item>

                <!-- 条件样式配置 -->
                <el-divider>条件样式</el-divider>
                <div class="condition-styles">
                  <div
                    v-for="(condition, index) in selectedWidget.conditionStyles"
                    :key="index"
                    class="condition-item"
                  >
                    <el-row :gutter="10">
                      <el-col :span="10">
                        <el-select v-model="condition.field" placeholder="字段" size="small">
                          <el-option v-for="field in availableFields" :key="field" :label="field" :value="field" />
                        </el-select>
                      </el-col>
                      <el-col :span="6">
                        <el-select v-model="condition.operator" placeholder="条件" size="small">
                          <el-option label="等于" value="eq" />
                          <el-option label="大于" value="gt" />
                          <el-option label="小于" value="lt" />
                          <el-option label="包含" value="contains" />
                        </el-select>
                      </el-col>
                      <el-col :span="6">
                        <el-input v-model="condition.value" placeholder="值" size="small" />
                      </el-col>
                      <el-col :span="2">
                        <el-button type="danger" link size="small" @click="removeCondition(index)">
                          <el-icon><Delete /></el-icon>
                        </el-button>
                      </el-col>
                    </el-row>
                    <el-row :gutter="10" style="margin-top: 8px">
                      <el-col :span="12">
                        <el-color-picker v-model="condition.backgroundColor" size="small" />
                        <span style="margin-left: 8px; font-size: 12px">背景色</span>
                      </el-col>
                      <el-col :span="12">
                        <el-color-picker v-model="condition.textColor" size="small" />
                        <span style="margin-left: 8px; font-size: 12px">文字色</span>
                      </el-col>
                    </el-row>
                  </div>
                  <el-button type="primary" link size="small" @click="addCondition">
                    <el-icon><Plus /></el-icon>
                    添加条件
                  </el-button>
                </div>

                <!-- 组件特定配置 -->
                <template v-if="selectedWidget.widgetType === 'card-number'">
                  <el-divider>数字卡片配置</el-divider>
                  <el-form-item label="数据字段">
                    <el-select v-model="selectedWidget.config.valueField" placeholder="选择数值字段" style="width: 100%">
                      <el-option v-for="field in availableFields" :key="field" :label="field" :value="field" />
                    </el-select>
                  </el-form-item>
                  <el-form-item label="前缀">
                    <el-input v-model="selectedWidget.config.prefix" placeholder="如: ¥" />
                  </el-form-item>
                  <el-form-item label="后缀">
                    <el-input v-model="selectedWidget.config.suffix" placeholder="如: %" />
                  </el-form-item>
                </template>

                <template v-if="selectedWidget.widgetType === 'progress-bar'">
                  <el-divider>进度条配置</el-divider>
                  <el-form-item label="数值字段">
                    <el-select v-model="selectedWidget.config.valueField" placeholder="选择数值字段" style="width: 100%">
                      <el-option v-for="field in availableFields" :key="field" :label="field" :value="field" />
                    </el-select>
                  </el-form-item>
                  <el-form-item label="进度条颜色">
                    <el-color-picker v-model="selectedWidget.config.color" />
                  </el-form-item>
                </template>

                <template v-if="selectedWidget.widgetType === 'status-light'">
                  <el-divider>状态灯配置</el-divider>
                  <el-form-item label="状态字段">
                    <el-select v-model="selectedWidget.config.statusField" placeholder="选择状态字段" style="width: 100%">
                      <el-option v-for="field in availableFields" :key="field" :label="field" :value="field" />
                    </el-select>
                  </el-form-item>
                  <el-form-item label="默认颜色">
                    <el-color-picker v-model="selectedWidget.config.color" />
                  </el-form-item>
                </template>

                <template v-if="['chart-bar', 'chart-line', 'chart-pie'].includes(selectedWidget.widgetType)">
                  <el-divider>图表配置</el-divider>
                  <el-form-item label="X轴字段">
                    <el-select v-model="selectedWidget.config.xField" placeholder="选择X轴字段" style="width: 100%">
                      <el-option v-for="field in availableFields" :key="field" :label="field" :value="field" />
                    </el-select>
                  </el-form-item>
                  <el-form-item label="Y轴字段">
                    <el-select v-model="selectedWidget.config.yField" placeholder="选择Y轴字段" style="width: 100%">
                      <el-option v-for="field in availableFields" :key="field" :label="field" :value="field" />
                    </el-select>
                  </el-form-item>
                </template>

                <template v-if="selectedWidget.widgetType === 'gauge'">
                  <el-divider>仪表盘配置</el-divider>
                  <el-form-item label="数值字段">
                    <el-select v-model="selectedWidget.config.valueField" placeholder="选择数值字段" style="width: 100%">
                      <el-option v-for="field in availableFields" :key="field" :label="field" :value="field" />
                    </el-select>
                  </el-form-item>
                  <el-form-item label="最大值">
                    <el-input-number v-model="selectedWidget.config.maxValue" :min="1" />
                  </el-form-item>
                </template>
              </el-form>
            </template>
            <el-empty v-else description="请选择一个组件" :image-size="60" />
          </el-tab-pane>
        </el-tabs>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { GridLayout, GridItem } from 'vue-grid-layout'
import {
  getDashboard,
  createDashboard,
  updateDashboard,
  addWidget,
  updateWidget,
  deleteWidget,
  updateWidgetPositions
} from '../../api/dashboard'
import { reportApi } from '../../api/request'

const router = useRouter()
const route = useRoute()

// 状态
const saving = ref(false)
const isEditingName = ref(false)
const activeTab = ref('dashboard')
const selectedWidgetId = ref(null)
const canvasContainer = ref(null)
const reportList = ref([])
const availableFields = ref([])

// 大屏表单
const dashboardForm = reactive({
  name: '',
  description: '',
  width: 1920,
  height: 1080,
  backgroundColor: '#0d1b2a',
  backgroundImage: '',
  theme: 'dark',
  refreshInterval: 0
})

// 布局数据
const layout = ref([])

// 计算属性
const canvasStyle = computed(() => ({
  width: `${dashboardForm.width}px`,
  height: `${dashboardForm.height}px`,
  backgroundColor: dashboardForm.backgroundColor,
  backgroundImage: dashboardForm.backgroundImage ? `url(${dashboardForm.backgroundImage})` : 'none',
  backgroundSize: 'cover',
  backgroundPosition: 'center'
}))

const rowHeight = computed(() => {
  // 根据画布高度计算行高，使网格更均匀
  return Math.floor((dashboardForm.height - 120) / 12)
})

const selectedWidget = computed(() => {
  if (!selectedWidgetId.value) return null
  return layout.value.find(item => item.i === selectedWidgetId.value)
})

// 基础组件列表
const basicWidgets = [
  { type: 'table', name: '表格', icon: 'Grid' },
  { type: 'card-number', name: '数字卡片', icon: 'Odometer' },
  { type: 'progress-bar', name: '进度条', icon: 'DataLine' },
  { type: 'status-light', name: '状态灯', icon: 'Sunrise' }
]

// 图表组件列表
const chartWidgets = [
  { type: 'chart-bar', name: '柱状图', icon: 'Histogram' },
  { type: 'chart-line', name: '折线图', icon: 'TrendCharts' },
  { type: 'chart-pie', name: '饼图', icon: 'PieChart' },
  { type: 'gauge', name: '仪表盘', icon: 'Stopwatch' }
]

// 获取图表类型名称
const getChartTypeName = (type) => {
  const names = {
    'chart-bar': '柱状图',
    'chart-line': '折线图',
    'chart-pie': '饼图',
    'gauge': '仪表盘'
  }
  return names[type] || '图表'
}

// 加载报表列表
const loadReportList = async () => {
  try {
    const res = await reportApi.getReports({ pageSize: 1000, includeDisabled: false })
    if (res.success) {
      const data = res.data
      reportList.value = data.Items || data.items || []
    }
  } catch {
    // 加载失败
  }
}

// 加载大屏数据
const loadDashboard = async (id) => {
  try {
    const res = await getDashboard(id)
    if (res.success) {
      const data = res.data
      dashboardForm.name = data.name || ''
      dashboardForm.description = data.description || ''
      dashboardForm.width = data.width || 1920
      dashboardForm.height = data.height || 1080
      dashboardForm.backgroundColor = data.backgroundColor || '#0d1b2a'
      dashboardForm.backgroundImage = data.backgroundImage || ''
      dashboardForm.theme = data.settings?.theme || 'dark'
      dashboardForm.refreshInterval = data.settings?.refreshInterval || 0

      // 加载组件布局
      if (data.widgets && data.widgets.length > 0) {
        layout.value = data.widgets.map((w, index) => ({
          i: w.widgetId || w.id || `widget-${index}`,
          x: w.x || 0,
          y: w.y || index,
          w: w.width || 3,
          h: w.height || 3,
          widgetType: w.widgetType,
          title: w.title,
          reportId: w.reportId,
          config: w.config || {},
          conditionStyles: w.conditionStyles || []
        }))
      }
    }
  } catch {
    ElMessage.error('加载大屏失败')
  }
}

// 拖拽开始
const handleDragStart = (event, widget) => {
  event.dataTransfer.setData('widgetType', widget.type)
  event.dataTransfer.setData('widgetName', widget.name)
}

// 放置组件
const handleDrop = (event) => {
  const widgetType = event.dataTransfer.getData('widgetType')
  const widgetName = event.dataTransfer.getData('widgetName')

  if (!widgetType) return

  // 计算放置位置
  const rect = event.currentTarget.getBoundingClientRect()
  const x = Math.floor((event.clientX - rect.left) / (rect.width / 12))
  const y = Math.floor((event.clientY - rect.top) / rowHeight.value)

  // 创建新组件
  const newWidget = {
    i: `widget-${Date.now()}`,
    x: Math.max(0, Math.min(x, 11)),
    y: Math.max(0, y),
    w: getDefaultWidth(widgetType),
    h: getDefaultHeight(widgetType),
    widgetType,
    title: widgetName,
    reportId: null,
    config: getDefaultConfig(widgetType),
    conditionStyles: []
  }

  layout.value.push(newWidget)
  selectedWidgetId.value = newWidget.i
  activeTab.value = 'widget'
}

// 获取默认宽度
const getDefaultWidth = (type) => {
  const widths = {
    'table': 8,
    'card-number': 2,
    'progress-bar': 4,
    'status-light': 2,
    'chart-bar': 4,
    'chart-line': 4,
    'chart-pie': 3,
    'gauge': 3
  }
  return widths[type] || 3
}

// 获取默认高度
const getDefaultHeight = (type) => {
  const heights = {
    'table': 4,
    'card-number': 2,
    'progress-bar': 2,
    'status-light': 2,
    'chart-bar': 4,
    'chart-line': 4,
    'chart-pie': 4,
    'gauge': 4
  }
  return heights[type] || 3
}

// 获取默认配置
const getDefaultConfig = (type) => {
  const configs = {
    'table': {},
    'card-number': { value: '1,234', valueField: '', prefix: '', suffix: '' },
    'progress-bar': { percentage: 75, valueField: '', color: '#409eff' },
    'status-light': { color: '#67c23a', statusField: '' },
    'chart-bar': { xField: '', yField: '' },
    'chart-line': { xField: '', yField: '' },
    'chart-pie': { xField: '', yField: '' },
    'gauge': { valueField: '', maxValue: 100 }
  }
  return configs[type] || {}
}

// 选择组件
const handleSelectWidget = (item) => {
  selectedWidgetId.value = item.i
  activeTab.value = 'widget'
}

// 删除组件
const handleDeleteWidget = async (item) => {
  try {
    await ElMessageBox.confirm('确定要删除这个组件吗？', '确认删除', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    const index = layout.value.findIndex(w => w.i === item.i)
    if (index !== -1) {
      layout.value.splice(index, 1)
      if (selectedWidgetId.value === item.i) {
        selectedWidgetId.value = null
        activeTab.value = 'dashboard'
      }
    }
  } catch {
    // 用户取消
  }
}

// 布局更新
const onLayoutUpdated = () => {
  // 布局更新时的回调
}

// 组件大小变化
const handleResize = () => {
  // 组件大小变化时的回调
}

// 报表变更时加载字段
const handleReportChange = async (reportId) => {
  if (!reportId) {
    availableFields.value = []
    return
  }

  try {
    const res = await reportApi.getReport(reportId)
    if (res.success && res.data.columns) {
      availableFields.value = res.data.columns.map(col => col.fieldName)
    }
  } catch {
    availableFields.value = []
  }
}

// 添加条件样式
const addCondition = () => {
  if (selectedWidget.value) {
    if (!selectedWidget.value.conditionStyles) {
      selectedWidget.value.conditionStyles = []
    }
    selectedWidget.value.conditionStyles.push({
      field: '',
      operator: 'eq',
      value: '',
      backgroundColor: '#ff0000',
      textColor: '#ffffff'
    })
  }
}

// 移除条件样式
const removeCondition = (index) => {
  if (selectedWidget.value && selectedWidget.value.conditionStyles) {
    selectedWidget.value.conditionStyles.splice(index, 1)
  }
}

// 保存大屏
const handleSave = async () => {
  if (!dashboardForm.name) {
    ElMessage.warning('请输入大屏名称')
    return
  }

  saving.value = true
  try {
    const dashboardId = route.params.id

    // 保存大屏基本配置
    const dashboardData = {
      name: dashboardForm.name,
      description: dashboardForm.description,
      width: dashboardForm.width,
      height: dashboardForm.height,
      backgroundColor: dashboardForm.backgroundColor,
      backgroundImage: dashboardForm.backgroundImage,
      isPublic: false,
      settings: {
        theme: dashboardForm.theme,
        refreshInterval: dashboardForm.refreshInterval
      }
    }

    let savedDashboardId = dashboardId

    if (dashboardId) {
      // 更新大屏
      await updateDashboard(dashboardId, dashboardData)
    } else {
      // 创建大屏
      const res = await createDashboard(dashboardData)
      if (res.success) {
        savedDashboardId = res.data.dashboardId || res.data.id
        // 更新 URL
        router.replace(`/dashboard/designer/${savedDashboardId}`)
      }
    }

    // 保存组件位置和配置
    if (savedDashboardId && layout.value.length > 0) {
      // 先获取现有组件
      const existingWidgets = layout.value.filter(w => !w.i.startsWith('widget-'))

      // 更新现有组件
      for (const widget of existingWidgets) {
        await updateWidget(savedDashboardId, widget.i, {
          widgetType: widget.widgetType,
          title: widget.title,
          x: widget.x,
          y: widget.y,
          width: widget.w,
          height: widget.h,
          reportId: widget.reportId,
          config: widget.config,
          conditionStyles: widget.conditionStyles
        })
      }

      // 添加新组件
      const newWidgets = layout.value.filter(w => w.i.startsWith('widget-'))
      for (const widget of newWidgets) {
        await addWidget(savedDashboardId, {
          widgetType: widget.widgetType,
          title: widget.title,
          x: widget.x,
          y: widget.y,
          width: widget.w,
          height: widget.h,
          reportId: widget.reportId,
          config: widget.config,
          conditionStyles: widget.conditionStyles
        })
      }

      // 批量更新组件位置
      const positions = layout.value.map(w => ({
        widgetId: w.i,
        x: w.x,
        y: w.y,
        width: w.w,
        height: w.h
      }))
      await updateWidgetPositions(savedDashboardId, positions)
    }

    ElMessage.success('保存成功')
  } catch (error) {
    ElMessage.error('保存失败: ' + (error.message || '未知错误'))
  } finally {
    saving.value = false
  }
}

// 预览大屏
const handlePreview = () => {
  const dashboardId = route.params.id
  if (dashboardId) {
    window.open(`/dashboard/view/${dashboardId}`, '_blank')
  } else {
    ElMessage.warning('请先保存大屏后再预览')
  }
}

// 返回列表
const handleBack = () => {
  router.push('/dashboard/list')
}

// 监听画布尺寸变化，调整缩放
watch([() => dashboardForm.width, () => dashboardForm.height], () => {
  // 可以在这里添加画布缩放逻辑
})

onMounted(async () => {
  await loadReportList()

  const dashboardId = route.params.id
  if (dashboardId) {
    await loadDashboard(dashboardId)
  }
})
</script>

<style scoped>
.dashboard-designer {
  height: 100vh;
  display: flex;
  flex-direction: column;
  background-color: #1a1a2e;
  overflow: hidden;
}

/* 工具栏 */
.designer-toolbar {
  height: 56px;
  min-height: 56px;
  background-color: #16213e;
  border-bottom: 1px solid #0f3460;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 16px;
}

.toolbar-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.toolbar-right {
  display: flex;
  align-items: center;
  gap: 8px;
}

.dashboard-name {
  color: #fff;
  font-size: 16px;
  font-weight: 500;
}

.name-text {
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
}

.name-text:hover .edit-icon {
  opacity: 1;
}

.edit-icon {
  opacity: 0;
  font-size: 14px;
  color: #909399;
  transition: opacity 0.2s;
}

/* 主体区域 */
.designer-body {
  flex: 1;
  display: flex;
  overflow: hidden;
}

/* 左侧组件面板 */
.widget-panel {
  width: 240px;
  min-width: 240px;
  background-color: #16213e;
  border-right: 1px solid #0f3460;
  display: flex;
  flex-direction: column;
}

.panel-header {
  height: 48px;
  display: flex;
  align-items: center;
  padding: 0 16px;
  color: #fff;
  font-weight: 500;
  border-bottom: 1px solid #0f3460;
}

.panel-content {
  flex: 1;
  overflow-y: auto;
  padding: 12px;
}

.widget-category {
  margin-bottom: 16px;
}

.category-title {
  color: #909399;
  font-size: 12px;
  margin-bottom: 8px;
  padding-left: 4px;
}

.widget-list {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 8px;
}

.widget-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 12px 8px;
  background-color: #1a1a2e;
  border: 1px solid #0f3460;
  border-radius: 6px;
  color: #fff;
  cursor: grab;
  transition: all 0.2s;
}

.widget-item:hover {
  background-color: #0f3460;
  border-color: #e94560;
}

.widget-item span {
  margin-top: 6px;
  font-size: 12px;
}

/* 中间画布区域 */
.canvas-container {
  flex: 1;
  overflow: auto;
  background-color: #0f0f1a;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
}

.canvas-wrapper {
  position: relative;
  border: 2px solid #0f3460;
  border-radius: 4px;
  box-shadow: 0 0 20px rgba(0, 0, 0, 0.3);
  overflow: hidden;
}

/* 组件样式 */
.widget-content {
  height: 100%;
  background-color: rgba(22, 33, 62, 0.9);
  border: 1px solid #0f3460;
  border-radius: 4px;
  padding: 10px;
  position: relative;
  overflow: hidden;
}

.grid-item.selected .widget-content {
  border-color: #e94560;
  box-shadow: 0 0 10px rgba(233, 69, 96, 0.3);
}

.widget-title {
  color: #fff;
  font-size: 14px;
  font-weight: 500;
  margin-bottom: 8px;
}

.widget-preview {
  height: calc(100% - 24px);
  display: flex;
  align-items: center;
  justify-content: center;
}

.table-preview {
  display: block;
}

.card-preview {
  flex-direction: column;
}

.card-value {
  font-size: 32px;
  font-weight: bold;
  color: #00d9ff;
}

.card-label {
  color: #909399;
  font-size: 14px;
  margin-top: 8px;
}

.progress-preview {
  flex-direction: column;
  width: 100%;
}

.status-preview {
  flex-direction: column;
  gap: 8px;
}

.status-light {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  box-shadow: 0 0 20px currentColor;
}

.status-label {
  color: #fff;
  font-size: 12px;
}

.chart-preview {
  width: 100%;
  height: 100%;
}

.chart-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: #909399;
}

.chart-placeholder span {
  margin-top: 8px;
  font-size: 12px;
}

.widget-delete-btn {
  position: absolute;
  top: 4px;
  right: 4px;
  opacity: 0;
  transition: opacity 0.2s;
}

.widget-content:hover .widget-delete-btn {
  opacity: 1;
}

/* 右侧属性面板 */
.property-panel {
  width: 320px;
  min-width: 320px;
  background-color: #16213e;
  border-left: 1px solid #0f3460;
  display: flex;
  flex-direction: column;
}

.property-panel :deep(.el-tabs__header) {
  margin: 0;
  padding: 0 16px;
  background-color: #1a1a2e;
}

.property-panel :deep(.el-tabs__item) {
  color: #909399;
}

.property-panel :deep(.el-tabs__item.is-active) {
  color: #fff;
}

.property-panel :deep(.el-tabs__content) {
  padding: 16px;
  overflow-y: auto;
}

.property-panel :deep(.el-form-item__label) {
  color: #909399;
}

.property-panel :deep(.el-input__wrapper),
.property-panel :deep(.el-textarea__inner) {
  background-color: #1a1a2e;
  border-color: #0f3460;
  box-shadow: none;
}

.property-panel :deep(.el-input__inner) {
  color: #fff;
}

/* 条件样式 */
.condition-styles {
  margin-top: 8px;
}

.condition-item {
  background-color: rgba(26, 26, 46, 0.5);
  border: 1px solid #0f3460;
  border-radius: 4px;
  padding: 12px;
  margin-bottom: 12px;
}

/* Element Plus 暗色主题适配 */
:deep(.el-divider__text) {
  background-color: #16213e;
  color: #909399;
}

:deep(.el-empty__description) {
  color: #909399;
}
</style>
