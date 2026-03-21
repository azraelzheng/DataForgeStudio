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
        <!-- Zoom Control -->
        <ZoomControl :scale="scale" @scale-change="handleScaleChange" />

        <!-- Grid Toggle -->
        <el-button
          :type="showGrid ? 'primary' : 'default'"
          @click="$emit('toggle-grid')"
        title="显示网格"
        >
          <el-icon><Grid /></el-icon>
        </el-button>

        <!-- Undo/Redo buttons -->
        <el-button-group style="margin-left: 8px">
          <el-button
            @click="handleUndo"
            :disabled="!canUndo"
            title="撤销 (Ctrl+Z)"
          >
            <el-icon><RefreshLeft /></el-icon>
          </el-button>
          <el-button
            @click="handleRedo"
            :disabled="!canRedo"
            title="重做 (Ctrl+Y)"
          >
            <el-icon><RefreshRight /></el-icon>
          </el-button>
        </el-button-group>

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
          <!-- 预置模板 -->
          <div class="widget-category">
            <div class="category-title">预置模板</div>
            <div class="template-list">
              <div
                v-for="template in presetTemplates"
                :key="template.id"
                class="template-item"
                @click="applyTemplate(template)"
              >
                <div class="template-preview" :style="{ background: template.previewBg }">
                  <div class="template-grid">
                    <div v-for="(cell, idx) in template.previewGrid" :key="idx" class="preview-cell" :style="cell"></div>
                  </div>
                </div>
                <div class="template-name">{{ template.name }}</div>
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
                    <el-table :data="getWidgetPreviewData(item)" size="small" border>
                      <el-table-column
                        v-for="(col, index) in getTableColumns(item)"
                        :key="index"
                        :prop="col.field"
                        :label="col.label"
                        :min-width="col.width || 80"
                      />
                      <el-table-column
                        v-if="!item.config?.columns || item.config.columns.length === 0"
                        label="未配置列"
                        width="120"
                      />
                    </el-table>
                    <div v-if="getWidgetPreviewData(item).length === 0" class="no-data-hint">
                      {{ item.reportId ? '加载中...' : '请绑定报表以显示数据' }}
                    </div>
                  </div>
                </template>

                <!-- 数字卡片组件 (支持 statistics 别名) -->
                <template v-else-if="item.widgetType === 'card-number' || item.widgetType === 'statistics'">
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

                <!-- 未知组件类型 -->
                <template v-else>
                  <div class="widget-title">{{ item.title || '组件' }}</div>
                  <div class="widget-preview unknown-preview">
                    <el-icon :size="32"><QuestionFilled /></el-icon>
                    <span>{{ item.widgetType || '未知类型' }}</span>
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
                <el-color-picker id="backgroundColor" v-model="dashboardForm.backgroundColor" />
              </el-form-item>
              <el-form-item label="背景图片">
                <el-input v-model="dashboardForm.backgroundImage" placeholder="背景图片URL" />
              </el-form-item>
              <el-form-item label="主题">
                <el-radio-group id="theme" v-model="dashboardForm.theme">
                  <el-radio value="dark">深色</el-radio>
                  <el-radio value="light">浅色</el-radio>
                </el-radio-group>
              </el-form-item>
              <el-form-item label="刷新间隔">
                <el-select id="refreshInterval" v-model="dashboardForm.refreshInterval" style="width: 100%">
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

                <!-- 查询条件配置 (绑定报表后显示) -->
                <template v-if="widgetQueryConditions.length > 0">
                  <el-divider>查询条件</el-divider>
                  <div class="query-conditions-config">
                    <div v-for="qc in widgetQueryConditions" :key="qc.fieldName + qc.operator" class="condition-item">
                      <el-form-item :label="qc.displayName">
                        <!-- 不需要输入值的操作符 -->
                        <template v-if="['null', 'notnull', 'true', 'false'].includes(qc.operator)">
                          <span class="operator-label">{{ getOperatorLabel(qc.operator) }}</span>
                        </template>

                        <!-- DateTime between: 日期范围选择器 -->
                        <template v-else-if="qc.operator === 'between' && qc.dataType === 'DateTime'">
                          <el-date-picker
                            v-model="queryConditionValues[getFieldKey(qc)]"
                            type="daterange"
                            range-separator="至"
                            start-placeholder="开始日期"
                            end-placeholder="结束日期"
                            value-format="YYYY-MM-DD"
                            style="width: 100%;"
                          />
                        </template>

                        <!-- Number between: 两个数字输入框 -->
                        <template v-else-if="qc.operator === 'between' && qc.dataType === 'Number'">
                          <div class="number-range-input">
                            <el-input-number
                              v-model="queryConditionValues[getFieldKey(qc) + '_start']"
                              placeholder="最小值"
                              :controls-position="'right'"
                              style="flex: 1;"
                            />
                            <span style="margin: 0 8px;">~</span>
                            <el-input-number
                              v-model="queryConditionValues[getFieldKey(qc) + '_end']"
                              placeholder="最大值"
                              :controls-position="'right'"
                              style="flex: 1;"
                            />
                          </div>
                        </template>

                        <!-- String 类型 -->
                        <template v-else-if="qc.dataType === 'String'">
                          <el-input
                            v-model="queryConditionValues[getFieldKey(qc)]"
                            :placeholder="getOperatorPlaceholder(qc.operator)"
                            clearable
                          />
                        </template>

                        <!-- Number 类型 -->
                        <template v-else-if="qc.dataType === 'Number'">
                          <el-input-number
                            v-model="queryConditionValues[getFieldKey(qc)]"
                            :placeholder="getOperatorPlaceholder(qc.operator)"
                            :controls-position="'right'"
                            style="width: 100%;"
                          />
                        </template>

                        <!-- DateTime 类型 -->
                        <template v-else-if="qc.dataType === 'DateTime'">
                          <el-date-picker
                            v-model="queryConditionValues[getFieldKey(qc)]"
                            type="date"
                            :placeholder="getOperatorPlaceholder(qc.operator)"
                            value-format="YYYY-MM-DD"
                            style="width: 100%;"
                          />
                        </template>

                        <!-- Boolean 类型 -->
                        <template v-else-if="qc.dataType === 'Boolean'">
                          <el-select
                            v-model="queryConditionValues[getFieldKey(qc)]"
                            placeholder="请选择"
                            clearable
                            style="width: 100%;"
                          >
                            <el-option label="是" :value="true" />
                            <el-option label="否" :value="false" />
                          </el-select>
                        </template>
                      </el-form-item>
                    </div>
                  </div>
                </template>

                <!-- 表格样式配置 -->
                <template v-if="selectedWidget.widgetType === 'table'">
                  <el-divider>表格样式</el-divider>
                  <el-form-item label="表格风格">
                    <el-select v-model="selectedWidget.styleConfig.tableStyle" placeholder="选择表格风格" style="width: 100%">
                      <el-option label="默认" value="default" />
                      <el-option label="深蓝色系（流程数据）" value="deep-blue" />
                      <el-option label="深紫色系（结果数据）" value="deep-purple" />
                      <el-option label="青色系（强调）" value="cyan" />
                      <el-option label="橙色系（警告/重点）" value="orange" />
                    </el-select>
                  </el-form-item>
                  <el-form-item label="斑马纹">
                    <el-switch v-model="selectedWidget.styleConfig.zebra" />
                  </el-form-item>
                </template>

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
                <template v-if="selectedWidget.widgetType === 'card-number' || selectedWidget.widgetType === 'statistics'">
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
import { ref, reactive, computed, onMounted, onActivated, watch, nextTick } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  ArrowLeft, Edit, View, Check, Delete, Plus,
  TrendCharts, QuestionFilled,
  Grid, Odometer, DataLine, Sunrise,
  Histogram, PieChart, Stopwatch, RefreshLeft, RefreshRight, Aim
} from '@element-plus/icons-vue'
import { useDashboardStore } from '@/stores/dashboard'
import { useKeyboard } from '@/composables/useKeyboard'
// vue-grid-layout 已在 main.js 中全局注册
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

// 图标映射
const iconMap = {
  Grid, Odometer, DataLine, Sunrise,
  Histogram, TrendCharts, PieChart, Stopwatch
}

const router = useRouter()
const route = useRoute()
const store = useDashboardStore()

// 初始化键盘快捷键
useKeyboard()

// 本地组件状态
const saving = ref(false)
const isEditingName = ref(false)
const activeTab = ref('dashboard')
const canvasContainer = ref(null)
const reportList = ref([])
const availableFields = ref([])
const widgetDataMap = ref({})  // 存储组件预览数据

const widgetQueryConditions = ref([])  // 当前选中组件的查询条件定义
const queryConditionValues = ref({})   // 当前选中组件的查询条件值

const isLocalDirty = ref(false)  // 用于追踪本地修改状态

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

// 使用 store 中的计算属性
const selectedWidget = computed(() => store.selectedWidget)
const selectedWidgetId = computed(() => store.selectedWidgetId)
const canUndo = computed(() => store.canUndo)
const canRedo = computed(() => store.canRedo)
const scale = computed({
  get: () => store.scale,
  set: (val) => store.setScale(val)
})
const showGrid = computed(() => store.showGrid)

// 撤销
const handleUndo = () => {
  store.undo()
}

// 重做
const handleRedo = () => {
  store.redo()
}

// 缩放变化
const handleScaleChange = (newScale) => {
  scale.value = newScale
}

// 切换网格
const handleToggleGrid = () => {
  store.toggleGrid()
}

// 布局数据与 store 同步
const layout = computed({
  get: () => store.widgets.map(w => ({
    i: w.widgetId || w.id,
    x: w.positionX ?? 0,
    y: w.positionY ?? 0,
    w: w.width ?? 3,
    h: w.height ?? 3,
    widgetType: w.widgetType,
    title: w.title,
    reportId: w.reportId,
    config: w.config || {},
    conditionStyles: w.conditionStyles || [],
    styleConfig: w.styleConfig || { tableStyle: 'default', zebra: false, conditionStyles: [] }
  })),
  set: (val) => {
    // 将布局变化同步回 store
    const widgets = val.map(item => ({
      widgetId: item.i,
      positionX: item.x,
      positionY: item.y,
      width: item.w,
      height: item.h,
      widgetType: item.widgetType,
      title: item.title,
      reportId: item.reportId,
      config: item.config,
      conditionStyles: item.conditionStyles,
      styleConfig: item.styleConfig
    }))
    store.widgets = widgets
  }
})

// 基础组件列表
const basicWidgets = [
  { type: 'table', name: '表格', icon: Grid },
  { type: 'card-number', name: '数字卡片', icon: Odometer },
  { type: 'progress-bar', name: '进度条', icon: DataLine },
  { type: 'status-light', name: '状态灯', icon: Sunrise }
]

// 图表组件列表
const chartWidgets = [
  { type: 'chart-bar', name: '柱状图', icon: Histogram },
  { type: 'chart-line', name: '折线图', icon: TrendCharts },
  { type: 'chart-pie', name: '饼图', icon: PieChart },
  { type: 'gauge', name: '仪表盘', icon: Stopwatch }
]

// 预置模板列表 - 基于参考图片设计
const presetTemplates = [
  {
    id: 'production-monitor',
    name: '生产监控',
    previewBg: 'linear-gradient(135deg, #0a1628 0%, #1a2a4a 100%)',
    previewGrid: [
      { gridArea: '1/1/3/3', background: 'rgba(0,212,255,0.2)', border: '1px solid rgba(0,212,255,0.3)' },
      { gridArea: '1/3/2/4', background: 'rgba(0,255,136,0.2)', border: '1px solid rgba(0,255,136,0.3)' },
      { gridArea: '2/3/3/4', background: 'rgba(255,170,0,0.2)', border: '1px solid rgba(255,170,0,0.3)' }
    ],
    layout: [
      { i: 'w1', x: 0, y: 0, w: 8, h: 8, widgetType: 'table', title: '生产数据监控', config: {} },
      { i: 'w2', x: 8, y: 0, w: 4, h: 4, widgetType: 'card-number', title: '当日产量', config: { color: '#00d4ff' } },
      { i: 'w3', x: 8, y: 4, w: 4, h: 4, widgetType: 'card-number', title: '设备稼动率', config: { color: '#00ff88' } }
    ]
  },
  {
    id: 'quality-inspection',
    name: '质检',
    previewBg: 'linear-gradient(135deg, #0a1628 0%, #2a1a4a 100%)',
    previewGrid: [
      { gridArea: '1/1/2/3', background: 'rgba(138,100,255,0.2)', border: '1px solid rgba(138,100,255,0.3)' },
      { gridArea: '2/1/3/2', background: 'rgba(0,212,255,0.2)', border: '1px solid rgba(0,212,255,0.3)' },
      { gridArea: '2/2/3/3', background: 'rgba(0,255,136,0.2)', border: '1px solid rgba(0,255,136,0.3)' }
    ],
    layout: [
      { i: 'w1', x: 0, y: 0, w: 8, h: 5, widgetType: 'table', title: '质检数据', config: {} },
      { i: 'w2', x: 8, y: 0, w: 4, h: 5, widgetType: 'chart-bar', title: '合格率统计', config: { color: '#8a64ff' } },
      { i: 'w3', x: 0, y: 5, w: 6, h: 4, widgetType: 'table', title: '不良品明细', config: {} },
      { i: 'w4', x: 6, y: 5, w: 6, h: 4, widgetType: 'gauge', title: '整体合格率', config: { maxValue: 100, value: 95 } }
    ]
  },
  {
    id: 'process-progress',
    name: '工序进度',
    previewBg: 'linear-gradient(135deg, #0a1628 0%, #1a3a3a 100%)',
    previewGrid: [
      { gridArea: '1/1/2/4', background: 'rgba(0,212,255,0.2)', border: '1px solid rgba(0,212,255,0.3)' },
      { gridArea: '2/1/3/4', background: 'rgba(0,255,136,0.15)', border: '1px solid rgba(0,255,136,0.3)' },
      { gridArea: '2/2/3/3', background: 'rgba(255,170,0,0.3)', border: '1px solid rgba(255,170,0,0.5)' }
    ],
    layout: [
      { i: 'w1', x: 0, y: 0, w: 12, h: 4, widgetType: 'table', title: '工序进度表', config: {} },
      { i: 'w2', x: 0, y: 4, w: 12, h: 5, widgetType: 'chart-bar', title: '甘特图', config: { color: '#00d4ff' } },
      { i: 'w3', x: 0, y: 9, w: 4, h: 3, widgetType: 'progress-bar', title: '整体进度', config: { percentage: 65, color: '#00ff88' } },
      { i: 'w4', x: 4, y: 9, w: 4, h: 3, widgetType: 'progress-bar', title: '完成率', config: { percentage: 78, color: '#00d4ff' } },
      { i: 'w5', x: 8, y: 9, w: 4, h: 3, widgetType: 'progress-bar', title: '延误率', config: { percentage: 12, color: '#ff6b6b' } }
    ]
  },
  {
    id: 'order-progress',
    name: '订单进度',
    previewBg: 'linear-gradient(135deg, #0a1628 0%, #2a2a1a 100%)',
    previewGrid: [
      { gridArea: '1/1/3/3', background: 'rgba(255,170,0,0.2)', border: '1px solid rgba(255,170,0,0.3)' },
      { gridArea: '1/3/2/4', background: 'rgba(0,212,255,0.2)', border: '1px solid rgba(0,212,255,0.3)' },
      { gridArea: '2/3/3/4', background: 'rgba(0,255,136,0.2)', border: '1px solid rgba(0,255,136,0.3)' }
    ],
    layout: [
      { i: 'w1', x: 0, y: 0, w: 8, h: 7, widgetType: 'table', title: '订单明细', config: {} },
      { i: 'w2', x: 8, y: 0, w: 4, h: 3, widgetType: 'card-number', title: '待处理订单', config: { color: '#ffaa00' } },
      { i: 'w3', x: 8, y: 3, w: 4, h: 4, widgetType: 'chart-pie', title: '订单状态分布', config: {} },
      { i: 'w4', x: 0, y: 7, w: 6, h: 5, widgetType: 'table', title: '今日交付', config: {} },
      { i: 'w5', x: 6, y: 7, w: 6, h: 5, widgetType: 'chart-line', title: '订单趋势', config: { color: '#00d4ff' } }
    ]
  },
  {
    id: 'equipment-status',
    name: '设备状态',
    previewBg: 'linear-gradient(135deg, #0a1628 0%, #1a1a2a 100%)',
    previewGrid: [
      { gridArea: '1/1/2/2', background: 'rgba(0,255,136,0.2)', border: '1px solid rgba(0,255,136,0.3)' },
      { gridArea: '1/2/2/3', background: 'rgba(255,170,0,0.2)', border: '1px solid rgba(255,170,0,0.3)' },
      { gridArea: '1/3/2/4', background: 'rgba(255,100,100,0.2)', border: '1px solid rgba(255,100,100,0.3)' },
      { gridArea: '2/1/3/4', background: 'rgba(0,212,255,0.15)', border: '1px solid rgba(0,212,255,0.3)' }
    ],
    layout: [
      { i: 'w1', x: 0, y: 0, w: 4, h: 3, widgetType: 'status-light', title: '运行中', config: { color: '#00ff88' } },
      { i: 'w2', x: 4, y: 0, w: 4, h: 3, widgetType: 'status-light', title: '待机', config: { color: '#ffaa00' } },
      { i: 'w3', x: 8, y: 0, w: 4, h: 3, widgetType: 'status-light', title: '故障', config: { color: '#ff6b6b' } },
      { i: 'w4', x: 0, y: 3, w: 12, h: 5, widgetType: 'table', title: '设备列表', config: {} },
      { i: 'w5', x: 0, y: 8, w: 6, h: 4, widgetType: 'chart-bar', title: '设备效率', config: { color: '#00d4ff' } },
      { i: 'w6', x: 6, y: 8, w: 6, h: 4, widgetType: 'chart-line', title: '温度监控', config: { color: '#ff6b6b' } }
    ]
  },
  {
    id: 'kanban-board',
    name: '看板布局',
    previewBg: 'linear-gradient(135deg, #0a1628 0%, #1a2a3a 100%)',
    previewGrid: [
      { gridArea: '1/1/3/2', background: 'rgba(0,212,255,0.2)', border: '1px solid rgba(0,212,255,0.3)' },
      { gridArea: '1/2/3/3', background: 'rgba(0,255,136,0.2)', border: '1px solid rgba(0,255,136,0.3)' },
      { gridArea: '1/3/3/4', background: 'rgba(255,170,0,0.2)', border: '1px solid rgba(255,170,0,0.3)' }
    ],
    layout: [
      { i: 'w1', x: 0, y: 0, w: 4, h: 12, widgetType: 'table', title: '待处理', config: {} },
      { i: 'w2', x: 4, y: 0, w: 4, h: 12, widgetType: 'table', title: '进行中', config: {} },
      { i: 'w3', x: 8, y: 0, w: 4, h: 12, widgetType: 'table', title: '已完成', config: {} }
    ]
  }
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

// 获取表格列配置
const getTableColumns = (widget) => {
  if (widget.config?.columns && widget.config.columns.length > 0) {
    return widget.config.columns
  }
  // 默认返回空数组，显示"未配置列"
  return []
}

// 加载报表列表
const loadReportList = async () => {
  try {
    const res = await reportApi.getReports({ pageSize: 1000, includeDisabled: false })
    if (res.success) {
      const data = res.data
      // 后端返回 PascalCase，需要转换为统一格式
      const items = data.Items || data.items || []
      reportList.value = items.map(r => ({
        reportId: r.ReportId || r.reportId,
        reportName: r.ReportName || r.reportName
      }))
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
      dashboardForm.name = data.Name || data.name || ''
      dashboardForm.description = data.Description || data.description || ''
      dashboardForm.theme = data.Theme || data.theme || 'dark'
      dashboardForm.refreshInterval = data.RefreshInterval || data.refreshInterval || 0

      // 解析 ThemeConfig 获取画布配置
      try {
        const themeConfig = JSON.parse(data.ThemeConfig || data.themeConfig || '{}')
        dashboardForm.width = themeConfig.width || 1920
        dashboardForm.height = themeConfig.height || 1080
        dashboardForm.backgroundColor = themeConfig.backgroundColor || '#0d1b2a'
        dashboardForm.backgroundImage = themeConfig.backgroundImage || ''
      } catch {
        // 解析失败时使用默认值
        dashboardForm.width = 1920
        dashboardForm.height = 1080
        dashboardForm.backgroundColor = '#0d1b2a'
      }

      // 先清空布局，再加载组件
      layout.value = []
      const widgets = data.Widgets || data.widgets || []
      console.log('Loading widgets from server:', widgets)
      if (widgets.length > 0) {
        layout.value = widgets.map((w, index) => {
          // 解析 DataConfig 和 StyleConfig
          let config = {}
          let conditionStyles = []
          try {
            config = JSON.parse(w.DataConfig || w.dataConfig || '{}')
          } catch { config = {} }
          try {
            const styleConfig = JSON.parse(w.StyleConfig || w.styleConfig || '{}')
            conditionStyles = styleConfig.conditionStyles || []
          } catch { }

          const widget = {
            i: w.WidgetId || w.widgetId || w.id || `widget-${index}`,
            x: w.PositionX ?? w.x ?? 0,
            y: w.PositionY ?? w.y ?? index,
            w: w.Width ?? w.width ?? 3,
            h: w.Height ?? w.height ?? 3,
            widgetType: w.WidgetType || w.widgetType,
            title: w.Title || w.title,
            reportId: w.ReportId || w.reportId,
            config: config,
            conditionStyles: conditionStyles
          }
          console.log('Loaded widget:', widget.i, 'reportId:', widget.reportId, 'raw:', w.ReportId, w.reportId)
          return widget
        })
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

// 应用预置模板
const applyTemplate = async (template) => {
  try {
    await ElMessageBox.confirm(
      `确定要应用"${template.name}"模板吗？这将清空当前画布上的所有组件。`,
      '应用模板',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )

    // 清空当前布局
    layout.value = []

    // 应用模板布局（重新生成唯一ID）
    const newLayout = template.layout.map((item, index) => ({
      ...item,
      i: `template-${Date.now()}-${index}`,
      config: { ...item.config },
      reportId: null,
      conditionStyles: []
    }))

    layout.value = newLayout

    // 设置主题色
    if (template.previewBg) {
      dashboardForm.backgroundColor = '#0a1628'
    }

    ElMessage.success(`已应用"${template.name}"模板，请为各组件绑定数据源`)
  } catch {
    // 用户取消
  }
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
    conditionStyles: [],
    styleConfig: {
      tableStyle: 'default',
      zebra: false,
      conditionStyles: []
    }
  }

  layout.value.push(newWidget)
  // 选中新添加的组件
  store.selectWidget(newWidget.i)
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
const handleSelectWidget = async (item) => {
  store.selectWidget(item.i)
  activeTab.value = 'widget'

  // 获取选中的组件
  const widget = layout.value.find(w => w.i === item.i)

  // 加载已保存的查询条件值
  if (widget?.config?.queryConditionValues) {
    queryConditionValues.value = { ...widget.config.queryConditionValues }
  } else {
    queryConditionValues.value = {}
  }

  // 如果有报表ID，加载查询条件定义
  if (widget?.reportId) {
    await loadQueryConditionsForWidget(widget.reportId)
  } else {
    widgetQueryConditions.value = []
  }
}

// 加载组件的查询条件定义
const loadQueryConditionsForWidget = async (reportId) => {
  try {
    const res = await reportApi.getReport(reportId)
    if (res.success) {
      const reportData = res.data
      const conditions = reportData.QueryConditions || reportData.queryConditions || []
      widgetQueryConditions.value = conditions.map(qc => ({
        fieldName: qc.FieldName || qc.fieldName,
        displayName: qc.DisplayName || qc.displayName || qc.FieldName || qc.fieldName,
        dataType: qc.DataType || qc.dataType || 'String',
        operator: qc.Operator || qc.operator || 'eq',
        defaultValue: qc.DefaultValue || qc.defaultValue
      }))
    }
  } catch (error) {
    console.error('加载查询条件失败:', error)
  }
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

// 获取字段键名（用于表单绑定）
const getFieldKey = (qc) => {
  return `${qc.fieldName}_${qc.operator}`
}

// 获取操作符标签
const getOperatorLabel = (operator) => {
  const labels = {
    'eq': '等于',
    'ne': '不等于',
    'gt': '大于',
    'lt': '小于',
    'ge': '大于等于',
    'le': '小于等于',
    'like': '包含',
    'between': '介于',
    'null': '为空',
    'notnull': '不为空',
    'true': '为真',
    'false': '为假'
  }
  return labels[operator] || operator
}

// 获取操作符占位符
const getOperatorPlaceholder = (operator) => {
  const labels = {
    'eq': '请输入等于的值',
    'ne': '请输入不等于的值',
    'gt': '请输入最小值（不含）',
    'lt': '请输入最大值（不含）',
    'ge': '请输入最小值',
    'le': '请输入最大值',
    'like': '请输入包含的关键字',
    'between': '请输入范围值'
  }
  return labels[operator] || '请输入值'
}

// 报表变更时加载字段、查询条件和预览数据
const handleReportChange = async (reportId) => {
  if (!reportId) {
    availableFields.value = []
    widgetQueryConditions.value = []
    queryConditionValues.value = {}
    return
  }

  try {
    // 获取报表详情
    const res = await reportApi.getReport(reportId)
    if (res.success) {
      const reportData = res.data
      const columns = reportData.Columns || reportData.columns || []
      availableFields.value = columns.map(col => col.FieldName || col.fieldName)

      // 获取查询条件
      const conditions = reportData.QueryConditions || reportData.queryConditions || []
      widgetQueryConditions.value = conditions.map(qc => ({
        fieldName: qc.FieldName || qc.fieldName,
        displayName: qc.DisplayName || qc.displayName || qc.FieldName || qc.fieldName,
        dataType: qc.DataType || qc.dataType || 'String',
        operator: qc.Operator || qc.operator || 'eq',
        defaultValue: qc.DefaultValue || qc.defaultValue
      }))

      // 初始化条件值（使用默认值）
      queryConditionValues.value = {}
      widgetQueryConditions.value.forEach(qc => {
        if (!['null', 'notnull', 'true', 'false'].includes(qc.operator)) {
          if (qc.defaultValue) {
            queryConditionValues.value[getFieldKey(qc)] = qc.defaultValue
          }
        }
      })

      // 存储列信息到组件配置中
      if (selectedWidget.value) {
        selectedWidget.value.config = selectedWidget.value.config || {}
        selectedWidget.value.config.columns = columns.map(col => ({
          field: col.FieldName || col.fieldName,
          label: col.DisplayName || col.displayName || col.FieldName || col.fieldName,
          width: col.Width || col.width || 100
        }))
      }
    }

    // 获取报表预览数据（最多显示 10 条）
    const dataRes = await reportApi.executeReport(reportId, { pageSize: 10 })
    if (dataRes.success && dataRes.data) {
      const data = dataRes.data?.Data || dataRes.data?.data || dataRes.data || []
      widgetDataMap.value[reportId] = Array.isArray(data) ? data.slice(0, 10) : []
    }
  } catch (err) {
    console.error('加载报表信息失败:', err)
    ElMessage.error('加载报表信息失败')
  }
}

// 获取组件预览数据
const getWidgetPreviewData = (widget) => {
  const data = widgetDataMap.value[widget.i]
  if (data && Array.isArray(data)) {
    return data
  }
  return []
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

  // 检查是否有组件未绑定报表
  const unboundWidgets = layout.value.filter(w => !w.reportId)
  if (unboundWidgets.length > 0) {
    const widgetNames = unboundWidgets.map(w => w.title || w.widgetType).join(', ')
    const confirmed = await ElMessageBox.confirm(
      `以下组件未绑定报表，将不会被保存：${widgetNames}。是否继续保存其他组件？`,
      '提示',
      {
        confirmButtonText: '继续保存',
        cancelButtonText: '取消',
        type: 'warning'
      }
    ).catch(() => false)

    if (!confirmed) {
      return
    }
  }

  saving.value = true
  try {
    const dashboardId = route.params.id

    // 保存大屏基本配置 - 使用后端期望的字段名 (PascalCase)
    // 注意：Width/Height/BackgroundColor/BackgroundImage 存储在 ThemeConfig 中
    const themeConfig = JSON.stringify({
      width: dashboardForm.width,
      height: dashboardForm.height,
      backgroundColor: dashboardForm.backgroundColor,
      backgroundImage: dashboardForm.backgroundImage,
      theme: dashboardForm.theme
    })

    const dashboardData = {
      Name: dashboardForm.name,
      Description: dashboardForm.description,
      Theme: dashboardForm.theme,
      RefreshInterval: dashboardForm.refreshInterval,
      IsPublic: false,
      ThemeConfig: themeConfig
    }

    let savedDashboardId = dashboardId

    if (dashboardId) {
      // 更新大屏
      console.log('Updating dashboard:', dashboardId, dashboardData)
      await updateDashboard(dashboardId, dashboardData)
    } else {
      // 创建大屏
      console.log('Creating dashboard:', dashboardData)
      const res = await createDashboard(dashboardData)
      if (res.success) {
        savedDashboardId = res.data.DashboardId || res.data.dashboardId || res.data.id
        // 更新 URL
        router.replace(`/dashboard/designer/${savedDashboardId}`)
      }
    }

    // 保存组件
    if (savedDashboardId) {
      // 获取数据库中已有的组件 ID 列表
      console.log('Fetching existing widgets for dashboard:', savedDashboardId)
      const existingRes = await getDashboard(savedDashboardId)
      const existingDbWidgets = existingRes.data?.Widgets || existingRes.data?.widgets || []
      const existingWidgetIds = existingDbWidgets.map(w => w.WidgetId || w.widgetId || w.id)

      // 只处理已绑定报表的组件
      const boundWidgets = layout.value.filter(w => w.reportId)
      const currentWidgetIds = boundWidgets.map(w => w.i)
      const existingWidgets = boundWidgets.filter(w => !String(w.i).startsWith('widget-'))
      const newWidgets = boundWidgets.filter(w => String(w.i).startsWith('widget-'))

      console.log('Widget classification:', {
        existingWidgetIds,
        currentWidgetIds,
        existingWidgets: existingWidgets.length,
        newWidgets: newWidgets.length,
        skipped: layout.value.length - boundWidgets.length
      })

      // 删除不再存在的组件（包括未绑定报表的组件）
      for (const widgetId of existingWidgetIds) {
        if (!currentWidgetIds.includes(widgetId)) {
          console.log('Deleting widget:', widgetId)
          await deleteWidget(savedDashboardId, widgetId)
        }
      }

      // 更新现有组件
      for (const widget of existingWidgets) {
        const widgetData = {
          WidgetType: widget.widgetType,
          Title: widget.title,
          PositionX: widget.x,
          PositionY: widget.y,
          Width: widget.w,
          Height: widget.h,
          ReportId: widget.reportId,  // 已经确保有 reportId
          DataConfig: JSON.stringify(widget.config || {}),
          StyleConfig: JSON.stringify({
            conditionStyles: widget.conditionStyles || [],
            tableStyle: widget.styleConfig?.tableStyle || 'default',
            zebra: widget.styleConfig?.zebra || false
          })
        }
        console.log('Updating widget:', widget.i, widgetData)
        await updateWidget(savedDashboardId, widget.i, widgetData)
      }

      // 添加新组件，并更新 layout 中的 ID 为真实 ID
      for (const widget of newWidgets) {
        console.log('Adding widget:', widget.widgetType)
        const res = await addWidget(savedDashboardId, {
          WidgetType: widget.widgetType,
          Title: widget.title,
          PositionX: widget.x,
          PositionY: widget.y,
          Width: widget.w,
          Height: widget.h,
          ReportId: widget.reportId,  // 已经确保有 reportId
          DataConfig: JSON.stringify(widget.config || {}),
          StyleConfig: JSON.stringify({
            conditionStyles: widget.conditionStyles || [],
            tableStyle: widget.styleConfig?.tableStyle || 'default',
            zebra: widget.styleConfig?.zebra || false
          })
        })

        // 更新 layout 中的组件 ID 为后端返回的真实 ID
        if (res.success && res.data) {
          const realId = res.data.WidgetId || res.data.widgetId || res.data.id
          if (realId) {
            const widgetIndex = layout.value.findIndex(w => w.i === widget.i)
            if (widgetIndex !== -1) {
              layout.value[widgetIndex].i = realId
              console.log('Updated widget ID from', widget.i, 'to', realId)
            }
          }
        }
      }

      // 只有当有组件时才更新位置
      if (boundWidgets.length > 0) {
        // 重新获取更新后的组件列表，用于批量更新位置
        const updatedRes = await getDashboard(savedDashboardId)
        const updatedWidgets = updatedRes.data?.Widgets || updatedRes.data?.widgets || []

        // 构建位置数组，确保使用正确的数据库 ID
        const positions = []
        for (const w of boundWidgets) {
          // 找到对应的数据库组件
          const dbWidget = updatedWidgets.find(uw =>
            (uw.WidgetId || uw.widgetId) === w.i ||
            (uw.WidgetId || uw.widgetId) === parseInt(w.i)
          )
          if (dbWidget) {
            positions.push({
              WidgetId: dbWidget.WidgetId || dbWidget.widgetId,
              PositionX: w.x,
              PositionY: w.y,
              Width: w.w,
              Height: w.h
            })
          }
        }

        if (positions.length > 0) {
          console.log('Updating widget positions:', positions)
          await updateWidgetPositions(savedDashboardId, positions)
        }
      }
    }

    ElMessage.success('保存成功')
  } catch (error) {
    console.error('保存失败:', error)
    ElMessage.error('保存失败: ' + (error.message || '未知错误'))
  } finally {
    saving.value = false
  }
}

// 预览大屏
const handlePreview = () => {
  const dashboardId = route.params.id
  if (dashboardId) {
    // 打开新窗口并传递 fullscreen 参数
    window.open(`/dashboard/view/${dashboardId}?fullscreen=true`, '_blank', 'fullscreen=yes')
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

// 同步查询条件值到组件配置
watch(queryConditionValues, (newValues) => {
  if (selectedWidget.value) {
    selectedWidget.value.config = selectedWidget.value.config || {}
    selectedWidget.value.config.queryConditionValues = { ...newValues }
  }
}, { deep: true })

// 组件选择变化时重置查询条件
watch(selectedWidgetId, (newId) => {
  if (!newId) {
    widgetQueryConditions.value = []
    queryConditionValues.value = {}
  }
})

onMounted(async () => {
  await loadReportList()

  const dashboardId = route.params.id
  if (dashboardId) {
    await loadDashboard(dashboardId)
  }
})

// keep-alive 激活时重置并重新加载数据
onActivated(async () => {
  const dashboardId = route.params.id

  // 重置状态
  layout.value = []
  selectedWidgetId.value = null
  activeTab.value = 'dashboard'
  dashboardForm.name = ''
  dashboardForm.description = ''
  dashboardForm.width = 1920
  dashboardForm.height = 1080
  dashboardForm.backgroundColor = '#0d1b2a'
  dashboardForm.backgroundImage = ''
  dashboardForm.theme = 'dark'
  dashboardForm.refreshInterval = 0

  // 重新加载大屏数据
  if (dashboardId) {
    await loadDashboard(dashboardId)
  }
})
</script>

<style scoped>
/* ==================== 大屏设计器主题变量（符合文档规范） ==================== */
.dashboard-designer {
  /* 布局尺寸 */
  --toolbar-height: 50px;
  --left-panel-width: 240px;
  --right-panel-width: 280px;
  --statusbar-height: 30px;

  /* 动效参数 */
  --transition-fast: 0.15s ease;
  --transition-base: 0.3s ease;
  --transition-slow: 0.5s ease;

  /* 背景色系（保持现有蓝色风格） */
  --bg-primary: #0a1628;
  --bg-secondary: rgba(0, 30, 60, 0.6);
  --bg-tertiary: rgba(0, 40, 80, 0.4);
  --bg-elevated: rgba(0, 50, 100, 0.3);
  --bg-hover: rgba(0, 212, 255, 0.15);

  /* 文字色系 */
  --text-primary: #e0f7ff;
  --text-secondary: rgba(0, 212, 255, 0.7);
  --text-tertiary: #909399;
  --text-disabled: #666666;

  /* 强调色（保持现有蓝色风格） */
  --accent-primary: #00d4ff;
  --accent-primary-light: #00e5ff;
  --accent-secondary: #0099ff;
  --accent-highlight: #00ffcc;
  --accent-success: #67c23a;
  --accent-warning: #e6a23c;
  --accent-danger: #e94560;

  /* 边框色系 */
  --border-primary: rgba(0, 212, 255, 0.3);
  --border-secondary: #0f3460;
  --border-active: #00d4ff;

  /* 阴影 */
  --shadow-glow: 0 0 20px rgba(0, 212, 255, 0.15);
  --shadow-card: 0 4px 12px rgba(0, 0, 0, 0.2);

  /* 圆角 */
  --radius-sm: 2px;
  --radius-md: 4px;
  --radius-lg: 8px;

  height: 100%;
  display: flex;
  flex-direction: column;
  background: linear-gradient(135deg, #0a1628 0%, #0d1f3c 50%, #0a1628 100%);
  overflow: hidden;
}

/* 工具栏 */
.designer-toolbar {
  height: var(--toolbar-height);
  min-height: var(--toolbar-height);
  background: linear-gradient(90deg, rgba(0, 20, 40, 0.95) 0%, rgba(0, 30, 60, 0.8) 100%);
  border-bottom: 1px solid var(--border-primary);
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
  transition: opacity var(--transition-base);
}

/* 主体区域 */
.designer-body {
  flex: 1;
  display: flex;
  overflow: hidden;
}

/* 左侧组件面板 */
.widget-panel {
  width: var(--left-panel-width);
  min-width: var(--left-panel-width);
  background: linear-gradient(180deg, rgba(0, 20, 40, 0.9) 0%, rgba(0, 30, 60, 0.8) 100%);
  border-right: 1px solid var(--border-primary);
  display: flex;
  flex-direction: column;
  box-shadow: 2px 0 10px rgba(0, 0, 0, 0.3);
}

.panel-header {
  height: 48px;
  display: flex;
  align-items: center;
  padding: 0 16px;
  color: var(--text-primary);
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
  color: var(--accent-primary);
  font-size: 12px;
  margin-bottom: 8px;
  padding-left: 4px;
  text-transform: uppercase;
  letter-spacing: 1px;
  text-shadow: 0 0 5px var(--accent-primary);
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
  background: rgba(0, 40, 80, 0.3);
  border: 1px solid var(--border-primary);
  border-radius: 6px;
  color: var(--text-primary);
  cursor: grab;
  transition: all 0.3s ease;
}

.widget-item:hover {
  background: rgba(0, 212, 255, 0.15);
  border-color: var(--accent-primary);
  box-shadow: 0 0 15px rgba(0, 212, 255, 0.3);
}

.widget-item span {
  margin-top: 6px;
  font-size: 12px;
}

/* 预置模板样式 */
.template-list {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 12px;
  padding: 8px;
}

.template-item {
  cursor: pointer;
  border-radius: 8px;
  overflow: hidden;
  border: 1px solid #0f3460;
  transition: all 0.3s ease;
  background: rgba(26, 26, 46, 0.8);
}

.template-item:hover {
  border-color: #00d4ff;
  box-shadow: 0 0 15px rgba(0, 212, 255, 0.3);
  transform: translateY(-2px);
}

.template-preview {
  height: 60px;
  padding: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.template-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  grid-template-rows: repeat(2, 1fr);
  gap: 3px;
  width: 100%;
  height: 100%;
}

.preview-cell {
  border-radius: 2px;
  transition: all 0.3s;
}

.template-item:hover .preview-cell {
  transform: scale(1.02);
}

.template-name {
  padding: 6px 8px;
  font-size: 12px;
  color: #e0f7ff;
  text-align: center;
  background: rgba(0, 20, 40, 0.6);
  border-top: 1px solid rgba(0, 212, 255, 0.2);
}

/* 中间画布区域 */
.canvas-container {
  flex: 1;
  overflow: auto;
  background: linear-gradient(135deg, #050d18 0%, #0a1628 50%, #050d18 100%);
  display: flex;
  align-items: flex-start;
  justify-content: center;
  padding: 20px;
}

.canvas-container::-webkit-scrollbar {
  width: 8px;
  height: 8px;
}

.canvas-container::-webkit-scrollbar-thumb {
  background: var(--border-primary);
  border-radius: 4px;
}

.canvas-container::-webkit-scrollbar-track {
  background: rgba(0, 20, 40, 0.5);
}

.canvas-wrapper {
  position: relative;
  border: 2px solid var(--border-primary);
  border-radius: 8px;
  box-shadow: 0 0 30px rgba(0, 212, 255, 0.15);
  overflow: hidden;
  padding: 10px;  /* 画布内边距，防止组件贴边被裁剪 */
  box-sizing: border-box;
}

/* 组件样式 */
.widget-content {
  height: 100%;
  background: linear-gradient(135deg, rgba(0, 40, 80, 0.4) 0%, rgba(0, 60, 100, 0.3) 100%);
  border: 1px solid var(--border-primary);
  border-radius: 8px;
  padding: 10px;
  position: relative;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  backdrop-filter: blur(5px);
}

.grid-item.selected .widget-content {
  border-color: var(--accent-primary);
  box-shadow: 0 0 15px rgba(0, 212, 255, 0.4);
}

.widget-title {
  color: var(--text-primary);
  font-size: 14px;
  font-weight: 500;
  margin-bottom: 8px;
  flex-shrink: 0;
  text-shadow: 0 0 5px var(--accent-primary);
}

.widget-preview {
  flex: 1;
  min-height: 0;  /* 允许 flex 子项收缩 */
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: auto;  /* 内容过多时可滚动 */
}

.table-preview {
  display: block;
  position: relative;
}

.no-data-hint {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  color: #909399;
  font-size: 12px;
  text-align: center;
  padding: 8px;
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

.unknown-preview {
  flex-direction: column;
  gap: 8px;
  color: #909399;
}

.unknown-preview span {
  font-size: 12px;
}

.widget-delete-btn {
  position: absolute;
  top: 4px;
  right: 4px;
  opacity: 0;
  transition: opacity var(--transition-base);
}

.widget-content:hover .widget-delete-btn {
  opacity: 1;
}

/* 右侧属性面板 */
.property-panel {
  width: var(--right-panel-width);
  min-width: var(--right-panel-width);
  background: linear-gradient(180deg, rgba(0, 20, 40, 0.9) 0%, rgba(0, 30, 60, 0.8) 100%);
  border-left: 1px solid var(--border-primary);
  display: flex;
  flex-direction: column;
  box-shadow: -2px 0 10px rgba(0, 0, 0, 0.3);
}

.property-panel :deep(.el-tabs__header) {
  margin: 0;
  padding: 0 16px;
  background: rgba(0, 30, 60, 0.5);
  border-bottom: 1px solid var(--border-primary);
}

.property-panel :deep(.el-tabs__item) {
  color: var(--text-secondary);
}

.property-panel :deep(.el-tabs__item.is-active) {
  color: var(--accent-primary);
  text-shadow: 0 0 5px var(--accent-primary);
}

.property-panel :deep(.el-tabs__active-bar) {
  background-color: var(--accent-primary);
}

.property-panel :deep(.el-tabs__content) {
  padding: 16px;
  overflow-y: auto;
}

.property-panel :deep(.el-form-item__label) {
  color: var(--text-secondary);
}

.property-panel :deep(.el-input__wrapper),
.property-panel :deep(.el-textarea__inner) {
  background-color: rgba(0, 30, 60, 0.5);
  border: 1px solid var(--border-primary);
  box-shadow: none;
  transition: all 0.3s;
}

.property-panel :deep(.el-input__wrapper:hover),
.property-panel :deep(.el-textarea__inner:hover) {
  border-color: var(--accent-primary);
}

.property-panel :deep(.el-input__wrapper:focus-within),
.property-panel :deep(.el-textarea__inner:focus) {
  border-color: var(--accent-primary);
  box-shadow: 0 0 10px rgba(0, 212, 255, 0.2);
}

.property-panel :deep(.el-input__inner) {
  color: var(--text-primary);
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

/* vue-grid-layout 样式 */
:deep(.vue-grid-layout) {
  min-height: 100%;
}

:deep(.vue-grid-item) {
  transition: all var(--transition-base);
  transition-property: left, top, width, height;
}

:deep(.vue-grid-item > div) {
  height: 100%;
}

:deep(.vue-grid-item.vue-grid-placeholder) {
  background-color: #e94560 !important;
  opacity: 0.4;
  border-radius: 4px;
}

:deep(.vue-grid-item.vue-draggable-dragging) {
  opacity: 0.8;
  z-index: 10;
}

:deep(.vue-grid-item.resizing) {
  opacity: 0.9;
}

:deep(.vue-resizable) {
  position: absolute;
  width: 100%;
  height: 100%;
}

:deep(.vue-resizable-handle) {
  position: absolute;
  width: 20px;
  height: 20px;
  bottom: 0;
  right: 0;
  cursor: se-resize;
  z-index: 10;
  background: linear-gradient(-45deg, transparent 50%, #e94560 50%);
  border-radius: 0 0 4px 0;
}

/* 查询条件配置样式 */
.query-conditions-config {
  max-height: 300px;
  overflow-y: auto;
}

.query-conditions-config .condition-item {
  margin-bottom: 8px;
}

.query-conditions-config .operator-label {
  color: #909399;
  font-size: 12px;
}

.query-conditions-config .number-range-input {
  display: flex;
  align-items: center;
}
</style>
