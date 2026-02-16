<template>
  <div class="home-page">
    <el-row :gutter="20">
      <!-- 统计卡片 -->
      <el-col :xs="24" :sm="12" :md="6">
        <el-card class="stat-card">
          <div class="stat-icon" style="background: #409EFF;">
            <el-icon :size="40"><Document /></el-icon>
          </div>
          <div class="stat-content">
            <div class="stat-title">报表数量</div>
            <div class="stat-value">{{ stats.reportCount }}</div>
          </div>
        </el-card>
      </el-col>

      <el-col :xs="24" :sm="12" :md="6">
        <el-card class="stat-card">
          <div class="stat-icon" style="background: #67C23A;">
            <el-icon :size="40"><User /></el-icon>
          </div>
          <div class="stat-content">
            <div class="stat-title">用户数量</div>
            <div class="stat-value">{{ stats.userCount }}</div>
          </div>
        </el-card>
      </el-col>

      <el-col :xs="24" :sm="12" :md="6">
        <el-card class="stat-card">
          <div class="stat-icon" style="background: #E6A23C;">
            <el-icon :size="40"><DataBoard /></el-icon>
          </div>
          <div class="stat-content">
            <div class="stat-title">数据源数量</div>
            <div class="stat-value">{{ stats.dataSourceCount }}</div>
          </div>
        </el-card>
      </el-col>

      <el-col :xs="24" :sm="12" :md="6">
        <el-card class="stat-card">
          <div class="stat-icon" style="background: #F56C6C;">
            <el-icon :size="40"><Clock /></el-icon>
          </div>
          <div class="stat-content">
            <div class="stat-title">系统运行天数</div>
            <div class="stat-value">{{ stats.systemDays }} 天</div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 快捷入口 -->
    <el-row :gutter="20" style="margin-top: 20px;">
      <el-col :span="24">
        <el-card>
          <template #header>
            <span>快捷入口</span>
          </template>
          <div class="quick-actions">
            <el-button
              v-for="action in quickActions"
              :key="action.name"
              :type="action.type"
              :icon="action.icon"
              @click="router.push(action.path)"
            >
              {{ action.name }}
            </el-button>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 最近报表 -->
    <el-row :gutter="20" style="margin-top: 20px;">
      <el-col :span="24">
        <el-card>
          <template #header>
            <div style="display: flex; justify-content: space-between; align-items: center;">
              <span>最近报表</span>
              <el-button text type="primary" @click="router.push('/report')">查看全部</el-button>
            </div>
          </template>
          <el-empty v-if="!recentReports.length" description="暂无报表" />
          <el-table v-else :data="recentReports" style="width: 100%">
            <el-table-column prop="reportName" label="报表名称" />
            <el-table-column prop="reportCategory" label="分类" width="120" />
            <el-table-column prop="viewCount" label="查看次数" width="100" />
            <el-table-column prop="lastViewTime" label="最后查看时间" width="180" />
            <el-table-column label="操作" width="150">
              <template #default="{ row }">
                <el-button type="primary" link size="small" @click="viewReport(row)">
                  查询
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useUserStore } from '../../stores/user'
import {
  Document,
  User,
  DataBoard,
  Clock,
  Plus,
  Search,
  Key,
  Setting,
  View,
  Download
} from '@element-plus/icons-vue'

const router = useRouter()
const userStore = useUserStore()

// 统计数据
const stats = ref({
  reportCount: 0,
  userCount: 0,
  dataSourceCount: 0,
  systemDays: 365
})

// 所有快捷操作
const allQuickActions = [
  { name: '创建报表', path: '/report/design', icon: 'Plus', type: 'primary', permission: 'report:design' },
  { name: '报表查询', path: '/report', icon: 'Search', type: 'default', permission: 'report:view' },
  { name: '数据源管理', path: '/system/datasource', icon: 'DataBoard', type: 'default', permission: 'datasource:view' },
  { name: '用户管理', path: '/system/user', icon: 'User', type: 'default', permission: 'user:view' },
  { name: '许可管理', path: '/license', icon: 'Key', type: 'warning', permission: 'license:view' },
  { name: '系统设置', path: '/system/backup', icon: 'Setting', type: 'default', permission: 'backup:view' }
]

// 根据权限过滤快捷操作
const quickActions = computed(() => {
  return allQuickActions.filter(action => {
    if (!action.permission) return true
    return userStore.hasPermission(action.permission)
  })
})

// 最近报表
const recentReports = ref([])

onMounted(async () => {
  // 加载统计数据
  await loadStats()
  await loadRecentReports()
})

const loadStats = async () => {
  // TODO: 调用 API 获取统计数据
  stats.value = {
    reportCount: 12,
    userCount: 5,
    dataSourceCount: 3,
    systemDays: 365
  }
}

const loadRecentReports = async () => {
  // TODO: 调用 API 获取最近报表
  recentReports.value = [
    { reportId: 1, reportName: '销售报表', reportCategory: '销售', viewCount: 128, lastViewTime: '2025-02-03 15:30' },
    { reportId: 2, reportName: '库存报表', reportCategory: '库存', viewCount: 86, lastViewTime: '2025-02-03 14:20' },
    { reportId: 3, reportName: '财务报表', reportCategory: '财务', viewCount: 64, lastViewTime: '2025-02-03 12:10' }
  ]
}

const viewReport = (row) => {
  router.push(`/report?reportId=${row.reportId}`)
}
</script>

<style scoped>
.home-page {
  padding: 0;
}

.stat-card {
  display: flex;
  align-items: center;
  padding: 20px;
}

.stat-icon {
  width: 60px;
  height: 60px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  margin-right: 20px;
}

.stat-content {
  flex: 1;
}

.stat-title {
  font-size: 14px;
  color: #909399;
  margin-bottom: 8px;
}

.stat-value {
  font-size: 24px;
  font-weight: bold;
  color: #303133;
}

.quick-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}
</style>
