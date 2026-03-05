<template>
  <div class="zoom-control">
    <el-button-group>
      <!-- 缩小按钮 -->
      <el-button
        :icon="ZoomOut"
        @click="zoomOut"
        :disabled="scale <= 0.25"
        title="缩小 (25% -)"
      />

      <!-- 缩放比例下拉 -->
      <el-dropdown trigger="click" @command="setScale">
        <el-button class="zoom-text-btn">
          {{ scalePercent }}%
        </el-button>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item
              v-for="level in zoomLevels"
              :key="level"
              :command="level"
              :class="{ 'is-active': scale === level }"
            >
              {{ level * 100 }}%
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>

      <!-- 放大按钮 -->
      <el-button
        :icon="ZoomIn"
        @click="zoomIn"
        :disabled="scale >= 2"
        title="放大 (+ 25%)"
      />

      <!-- 适应屏幕按钮 -->
      <el-button
        :icon="Aim"
        @click="fitToScreen"
        title="适应屏幕"
      />

      <!-- 100% 按钮 -->
      <el-button
        v-if="scale !== 1"
        @click="setScale(1)"
        title="重置为 100%"
      >
        1:1
      </el-button>
    </el-button-group>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { ZoomIn, ZoomOut, Aim } from '@element-plus/icons-vue'
import { useDashboardStore } from '@/stores/dashboard'

const store = useDashboardStore()

// 缩放比例
const scale = computed(() => store.scale)
const scalePercent = computed(() => Math.round(scale.value * 100))

// 预设缩放级别
const zoomLevels = [0.25, 0.5, 0.75, 1, 1.25, 1.5, 1.75, 2]

/**
 * 放大
 */
function zoomIn() {
  store.setScale(Math.min(2, scale.value + 0.25))
}

/**
 * 缩小
 */
function zoomOut() {
  store.setScale(Math.max(0.25, scale.value - 0.25))
}

/**
 * 设置缩放
 * @param {number} level - 缩放级别
 */
function setScale(level) {
  store.setScale(level)
}

/**
 * 适应屏幕
 * 计算能让画布完全显示在容器中的最大缩放比例
 */
function fitToScreen() {
  // 获取画布容器
  const container = document.querySelector('.canvas-container') ||
                    document.querySelector('.designer-canvas-wrapper')

  if (!container) {
    // 默认缩放到 75%
    store.setScale(0.75)
    return
  }

  const containerWidth = container.clientWidth - 40 // 留边距
  const containerHeight = container.clientHeight - 40

  const canvasWidth = store.canvasWidth
  const canvasHeight = store.canvasHeight

  // 计算适应比例
  const scaleX = containerWidth / canvasWidth
  const scaleY = containerHeight / canvasHeight

  // 取较小值，确保画布完全显示
  const fitScale = Math.min(scaleX, scaleY, 1) // 最大不超过 100%

  // 四舍五入到 0.05 的倍数
  const roundedScale = Math.round(fitScale * 20) / 20

  store.setScale(Math.max(0.25, roundedScale))
}
</script>

<style scoped>
.zoom-control {
  display: inline-flex;
  align-items: center;
}

.zoom-text-btn {
  min-width: 60px;
  font-family: 'Consolas', monospace;
}

:deep(.el-button-group) {
  display: flex;
}

:deep(.el-dropdown-menu__item.is-active) {
  color: var(--el-color-primary);
  font-weight: bold;
}
</style>
