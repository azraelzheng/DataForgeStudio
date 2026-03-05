<template>
  <div class="designer-toolbar">
    <!-- 缩放控制 -->
    <Zoom-control
      :scale="scale"
      :show-grid="showGrid"
      @click="$emit('toggle-grid')"
    />

    <!-- 撤销/重做 -->
    <div class="toolbar-group">
      <el-button
        :icon="RefreshLeft"
        @click="handleUndo"
        :disabled="!canUndo"
        title="撤销 (Ctrl+Z)"
      />
      <el-button
        :icon="RefreshRight"
        @click="handleRedo"
        :disabled="!canRedo"
        title="重做 (Ctrl+Shift+Z / Ctrl+Y)"
      />
    </div>

    <!-- 右侧工具组 -->
    <div class="toolbar-group">
      <!-- 网格 -->
      <el-button
        :icon="Grid"
        :type="showGrid ? 'primary' : 'text'"
        @click="$emit('toggle-grid')"
      >
        {{ showGrid ? '显示网格' : '隐藏网格' }}
      >
      <!-- 缩放 -->
      <zoom-control
        :scale="scale"
        @command="handleScaleChange"
      />
    </div>

    <!-- 更多工具 -->
    <div class="toolbar-group more">
      <el-button
        :icon="More"
        @click="showMore"
      >
        更多
      </el-dropdown trigger="click">
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item @click="saveDashboard">
              <el-icon><DocumentChecked /></el-icon>
              保存
            </el-dropdown-item @click="exportDashboard">
              <el-icon><Download /></el-icon>
              导出
            </el-dropdown-item @click="toggleFullscreen">
              <el-icon><FullScreen /></el-icon>
              全屏预览
            </el-dropdown-item divided @click="showShortcuts">
              <el-icon><QuestionFilled /></el-icon>
              快捷键
            </el-dropdown-menu>
          </template>
        </el-button>
      </el-dropdown>
    </div>

    <!-- 右侧操作组 -->
    <div class="toolbar-group right">
      <el-button
        :icon="Edit"
        @click="handleEditWidget"
        :disabled="!selectedWidget"
        title="编辑组件"
      />
      <el-button
        :icon="Delete"
        @click="handleDeleteWidget"
        :disabled="!selectedWidget"
        title="删除组件 (Delete)"
      />
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import {
  RefreshLeft,
  RefreshRight
  Grid,
  Edit,
  Delete,
  More,
  QuestionFilled,
  FullScreen
} from '@element-plus/icons-vue'
import { useDashboardStore } from '@/stores/dashboard'
import { ElMessage, from 'element-plus'

const emit = defineEmits(['toggle-grid'])

const store = useDashboardStore()

// 计算属性
const scale = computed(() => store.scale)
const showGrid = computed(() => store.showGrid)
const selectedWidget = computed(() => store.selectedWidget)
const canUndo = computed(() => store.canUndo)
const canRedo = computed(() => store.canRedo)

// 切换网格
const handleToggleGrid = () => {
  store.toggleGrid()
  emit('toggle-grid', showGrid.value)
}

// 缩放控制
const handleScaleChange = (newScale) => {
  store.setScale(newScale)
}

// 撤销
const handleUndo = () => {
  if (store.canUndo) {
    store.undo()
    ElMessage.success('已撤销')
  }
}

// 重做
const handleRedo = () => {
  if (store.canRedo) {
    store.redo()
    ElMessage.success('已重做')
  }
}

// 删除组件
const handleDeleteWidget = () => {
  if (!store.selectedWidget) {
    ElMessage.warning('请先选择组件')
    return
  }
  await store.deleteExistingWidget(store.selectedWidget.widgetId || store.selectedWidget.id)
  ElMessage.success('删除成功')
}

// 编辑组件
const handleEditWidget = () => {
  if (!store.selectedWidget) {
    ElMessage.warning('请先选择组件')
    return
  }
  emit('edit-widget', store.selectedWidget)
}

// 全屏预览
const handleFullscreen = () => {
  emit('fullscreen')
}

// 保存
const handleSave = async () => {
  await store.saveDashboard()
  if (store.isDirty) {
    ElMessage.success('保存成功')
  }
}

// 导出
const handleExport = () => {
  if (!store.currentDashboard) {
    ElMessage.warning('请先加载大屏')
    return
  }
  emit('export', store.currentDashboard)
}

// 更多功能
const showMore = () => {
  ElMessage.info('更多功能开发中...')
}

// 快捷键帮助
const showShortcuts = () => {
  ElMessageBox.confirm(
    '快捷键帮助',
    '键盘快捷键：\n\n' +
              'Delete / Backspace - 删除选中组件\n' +
              'Ctrl + C - 复制组件\n' +
              'Ctrl + V - 粘贴组件\n' +
              'Ctrl + Z - 撤销\n' +
              'Ctrl + Shift + Z / Ctrl + Y - 重做\n' +
              'Escape - 取消选择\n' +
              '方向键 - 微调组件位置\n' +
              'Shift + 方向键 - 快速移动组件',
            '</pre>
    <el-button type="primary">确定</el-button>
  })
}

// 暴露给模板
defineExpose({
  scale,
  showGrid,
  selectedWidget,
  canUndo,
  canRedo
  handleToggleGrid,
  handleScaleChange,
  handleUndo,
  handleRedo,
  handleDeleteWidget,
  handleEditWidget,
  handleFullscreen,
  handleSave,
  handleExport
  showMore,
  showShortcuts
})
</script>

<style scoped>
.designer-toolbar {
  display: flex;
  align-items: center;
  padding: 8px 16px;
  background: var(--el-bg-color);
  border-radius: 4px;
}

.toolbar-group {
  display: flex;
  gap: 8px;
}

:deep(.el-button-group) {
  display: flex;
}

:deep(.el-button) {
  padding: 8px;
}

.zoom-control {
  margin: 0 8px;
}

/* 深色工具栏图标按钮 */
:deep(.designer-toolbar button) {
  color: #a0b2a;
}

:deep(.designer-toolbar .el-button):hover {
  background-color: var(--el-color-primary-light);
}

/* 緩放文本样式 */
.zoom-text-btn {
  min-width: 60px !important;
  font-family: 'Consolas', monospace;
}
</style>
