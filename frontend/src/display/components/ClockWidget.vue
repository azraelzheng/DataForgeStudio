<template>
  <div class="clock-widget" :class="{ compact }">
    <div class="clock-time">{{ time }}</div>
    <div v-if="!compact" class="clock-date">{{ date }}</div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'

interface Props {
  /** 是否紧凑模式（仅显示时间） */
  compact?: boolean
  /** 是否显示秒 */
  showSeconds?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  compact: false,
  showSeconds: true
})

const time = ref('')
const date = ref('')

// 星期名称
const weekDays = ['星期日', '星期一', '星期二', '星期三', '星期四', '星期五', '星期六']

/**
 * 更新时间
 */
function updateClock(): void {
  const now = new Date()

  // 格式化时间
  const hours = String(now.getHours()).padStart(2, '0')
  const minutes = String(now.getMinutes()).padStart(2, '0')
  const seconds = String(now.getSeconds()).padStart(2, '0')

  if (props.showSeconds) {
    time.value = `${hours}:${minutes}:${seconds}`
  } else {
    time.value = `${hours}:${minutes}`
  }

  // 格式化日期
  if (!props.compact) {
    const year = now.getFullYear()
    const month = String(now.getMonth() + 1).padStart(2, '0')
    const day = String(now.getDate()).padStart(2, '0')
    const weekDay = weekDays[now.getDay()]
    date.value = `${year}年${month}月${day}日 ${weekDay}`
  }
}

let timer: ReturnType<typeof setInterval> | null = null

onMounted(() => {
  updateClock()
  timer = setInterval(updateClock, 1000)
})

onUnmounted(() => {
  if (timer) {
    clearInterval(timer)
  }
})
</script>

<style scoped>
.clock-widget {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  font-family: 'Segoe UI', 'Roboto', 'Helvetica Neue', Arial, sans-serif;
  font-weight: 500;
}

.clock-time {
  font-size: 2.5rem;
  line-height: 1.2;
  font-feature-settings: 'tnum';
  font-variant-numeric: tabular-nums;
  letter-spacing: 0.05em;
}

.clock-date {
  font-size: 0.875rem;
  opacity: 0.7;
  margin-top: 0.25rem;
}

.compact .clock-time {
  font-size: 1.5rem;
}

/* 响应式适配 */
@media (max-width: 1920px) {
  .clock-time {
    font-size: 2rem;
  }
}

@media (min-width: 2560px) {
  .clock-time {
    font-size: 3rem;
  }

  .clock-date {
    font-size: 1rem;
  }
}

@media (min-width: 3840px) {
  .clock-time {
    font-size: 4rem;
  }

  .clock-date {
    font-size: 1.25rem;
  }
}
</style>
