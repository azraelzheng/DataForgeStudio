<!--
  WidgetWrapper - 组件包装器
  为所有组件提供统一的外框、拖拽、调整大小等功能
-->
<template>
  <div
    ref="wrapperRef"
    class="widget-wrapper"
    :class="wrapperClasses"
    :style="wrapperStyle"
    @mousedown="handleMouseDown"
  >
    <!-- 选中状态边框 -->
    <div v-if="isSelected && isEditing" class="widget-border" />

    <!-- 拖拽手柄 -->
    <div v-if="isEditing" class="widget-drag-handle" :class="{ 'is-dragging': isDragging }">
      <el-icon :size="16">
        <Rank />
      </el-icon>
    </div>

    <!-- 工具栏 -->
    <div v-if="isEditing && (isSelected || isHovered)" class="widget-toolbar">
      <el-button-group>
        <el-tooltip content="刷新数据" placement="top">
          <el-button :icon="Refresh" size="small" @click="handleRefresh" />
        </el-tooltip>
        <el-tooltip content="配置" placement="top">
          <el-button :icon="Setting" size="small" @click="handleConfig" />
        </el-tooltip>
        <el-tooltip content="复制" placement="top">
          <el-button :icon="CopyDocument" size="small" @click="handleCopy" />
        </el-tooltip>
        <el-tooltip content="删除" placement="top">
          <el-button :icon="Delete" size="small" type="danger" @click="handleDelete" />
        </el-tooltip>
      </el-button-group>
    </div>

    <!-- 调整大小手柄 -->
    <template v-if="isEditing && isSelected">
      <div
        v-for="handle in resizeHandles"
        :key="handle.position"
        class="resize-handle"
        :class="`resize-handle-${handle.position}`"
        @mousedown.stop="handleResizeStart($event, handle.position)"
      />
    </template>

    <!-- 组件内容 -->
    <div class="widget-content" :class="{ 'is-loading': isLoading }">
      <slot />
    </div>

    <!-- 加载遮罩 -->
    <div v-if="isLoading" class="widget-loading-mask">
      <el-icon class="is-loading" :size="24">
        <Loading />
      </el-icon>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, type PropType } from 'vue'
import { Rank, Refresh, Setting, CopyDocument, Delete, Loading } from '@element-plus/icons-vue'
import type { GridPosition, WidgetInstance } from '../types/dashboard'

/**
 * 调整手柄位置
 */
type ResizeHandlePosition = 'n' | 'e' | 's' | 'w' | 'ne' | 'se' | 'sw' | 'nw'

interface ResizeHandle {
  position: ResizeHandlePosition
  cursor: string
}

// Props
const props = defineProps({
  /** 组件实例 */
  widget: {
    type: Object as PropType<WidgetInstance>,
    required: true
  },
  /** 是否选中 */
  isSelected: {
    type: Boolean,
    default: false
  },
  /** 是否编辑模式 */
  isEditing: {
    type: Boolean,
    default: false
  },
  /** 是否加载中 */
  isLoading: {
    type: Boolean,
    default: false
  },
  /** 是否禁用交互 */
  disabled: {
    type: Boolean,
    default: false
  }
})

// Emits
const emit = defineEmits<{
  /** 选中组件 */
  select: []
  /** 开始拖拽 */
  'drag-start': [event: MouseEvent]
  /** 拖拽中 */
  drag: [event: MouseEvent]
  /** 结束拖拽 */
  'drag-end': [event: MouseEvent]
  /** 开始调整大小 */
  'resize-start': [event: MouseEvent, position: ResizeHandlePosition]
  /** 调整大小中 */
  resize: [event: MouseEvent, position: ResizeHandlePosition]
  /** 结束调整大小 */
  'resize-end': [event: MouseEvent, position: ResizeHandlePosition]
  /** 刷新 */
  refresh: []
  /** 配置 */
  config: []
  /** 复制 */
  copy: []
  /** 删除 */
  delete: []
}>()

// 状态
const wrapperRef = ref<HTMLElement>()
const isHovered = ref(false)
const isDragging = ref(false)

// 调整大小手柄配置
const resizeHandles: ResizeHandle[] = [
  { position: 'n', cursor: 'ns-resize' },
  { position: 'e', cursor: 'ew-resize' },
  { position: 's', cursor: 'ns-resize' },
  { position: 'w', cursor: 'ew-resize' },
  { position: 'ne', cursor: 'nesw-resize' },
  { position: 'se', cursor: 'nwse-resize' },
  { position: 'sw', cursor: 'nesw-resize' },
  { position: 'nw', cursor: 'nwse-resize' }
]

// 计算属性
const wrapperClasses = computed(() => ({
  'is-selected': props.isSelected,
  'is-editing': props.isEditing,
  'is-dragging': isDragging.value,
  'is-disabled': props.disabled
}))

const wrapperStyle = computed(() => {
  const pos = props.widget.position
  return {
    gridColumn: `span ${pos.width}`,
    gridRow: `span ${pos.height}`
  }
})

// 鼠标按下事件
function handleMouseDown(event: MouseEvent): void {
  if (!props.isEditing || props.disabled) return

  emit('select')

  if (event.button !== 0) return // 只响应左键

  isDragging.value = true
  emit('drag-start', event)

  const handleMouseMove = (e: MouseEvent): void => {
    emit('drag', e)
  }

  const handleMouseUp = (e: MouseEvent): void => {
    isDragging.value = false
    emit('drag-end', e)
    document.removeEventListener('mousemove', handleMouseMove)
    document.removeEventListener('mouseup', handleMouseUp)
  }

  document.addEventListener('mousemove', handleMouseMove)
  document.addEventListener('mouseup', handleMouseUp)
}

// 开始调整大小
function handleResizeStart(event: MouseEvent, position: ResizeHandlePosition): void {
  if (!props.isEditing || props.disabled) return

  emit('resize-start', event, position)

  const handleMouseMove = (e: MouseEvent): void => {
    emit('resize', e, position)
  }

  const handleMouseUp = (e: MouseEvent): void => {
    emit('resize-end', e, position)
    document.removeEventListener('mousemove', handleMouseMove)
    document.removeEventListener('mouseup', handleMouseUp)
  }

  document.addEventListener('mousemove', handleMouseMove)
  document.addEventListener('mouseup', handleMouseUp)
}

// 刷新
function handleRefresh(): void {
  emit('refresh')
}

// 配置
function handleConfig(): void {
  emit('config')
}

// 复制
function handleCopy(): void {
  emit('copy')
}

// 删除
function handleDelete(): void {
  emit('delete')
}

// 暴露方法
defineExpose({
  wrapperRef
})
</script>

<style scoped lang="scss">
.widget-wrapper {
  position: relative;
  width: 100%;
  height: 100%;
  min-height: 100px;
  background: #fff;
  border-radius: 4px;
  overflow: hidden;
  transition: box-shadow 0.3s, transform 0.2s;

  &.is-editing {
    cursor: move;

    &:hover {
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }
  }

  &.is-selected {
    box-shadow: 0 0 0 2px #409eff;
  }

  &.is-dragging {
    opacity: 0.8;
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.2);
  }

  &.is-disabled {
    pointer-events: none;
    opacity: 0.6;
  }
}

.widget-border {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  border: 2px dashed #409eff;
  border-radius: 4px;
  pointer-events: none;
  z-index: 1;
}

.widget-drag-handle {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(180deg, rgba(0, 0, 0, 0.05) 0%, rgba(0, 0, 0, 0) 100%);
  cursor: move;
  z-index: 2;
  opacity: 0;
  transition: opacity 0.2s;
  color: #409eff;

  .widget-wrapper.is-editing:hover &,
  .widget-wrapper.is-selected & {
    opacity: 1;
  }
}

.widget-toolbar {
  position: absolute;
  top: 8px;
  right: 8px;
  z-index: 3;
  background: #fff;
  border-radius: 4px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  padding: 4px;
}

.resize-handle {
  position: absolute;
  background: #409eff;
  z-index: 2;

  &.resize-handle-n {
    top: 0;
    left: 8px;
    right: 8px;
    height: 4px;
    cursor: ns-resize;
  }

  &.resize-handle-e {
    top: 8px;
    right: 0;
    bottom: 8px;
    width: 4px;
    cursor: ew-resize;
  }

  &.resize-handle-s {
    bottom: 0;
    left: 8px;
    right: 8px;
    height: 4px;
    cursor: ns-resize;
  }

  &.resize-handle-w {
    top: 8px;
    left: 0;
    bottom: 8px;
    width: 4px;
    cursor: ew-resize;
  }

  &.resize-handle-ne {
    top: 0;
    right: 0;
    width: 8px;
    height: 8px;
    cursor: nesw-resize;
    border-radius: 0 4px 0 0;
  }

  &.resize-handle-se {
    bottom: 0;
    right: 0;
    width: 8px;
    height: 8px;
    cursor: nwse-resize;
    border-radius: 0 0 4px 0;
  }

  &.resize-handle-sw {
    bottom: 0;
    left: 0;
    width: 8px;
    height: 8px;
    cursor: nesw-resize;
    border-radius: 0 0 0 4px;
  }

  &.resize-handle-nw {
    top: 0;
    left: 0;
    width: 8px;
    height: 8px;
    cursor: nwse-resize;
    border-radius: 4px 0 0 0;
  }

  &:hover {
    background: #66b1ff;
  }
}

.widget-content {
  width: 100%;
  height: 100%;
  overflow: hidden;

  &.is-loading {
    opacity: 0.5;
    pointer-events: none;
  }
}

.widget-loading-mask {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(255, 255, 255, 0.8);
  z-index: 10;
  color: #409eff;
}
</style>
