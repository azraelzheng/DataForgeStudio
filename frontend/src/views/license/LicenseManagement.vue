<template>
  <div class="license-management">
    <el-row :gutter="20">
      <el-col :span="12">
        <el-card>
          <template #header>
            <span>许可证信息</span>
          </template>
          <el-descriptions :column="1" border v-if="licenseInfo">
            <el-descriptions-item label="产品名称">DataForgeStudio V4</el-descriptions-item>
            <el-descriptions-item label="许可证类型">
              <el-tag :type="licenseInfo.licenseType === 'Trial' ? 'warning' : 'success'">
                {{ getLicenseTypeText(licenseInfo.licenseType) }}
              </el-tag>
            </el-descriptions-item>
            <el-descriptions-item label="客户名称">{{ licenseInfo.customerName || '-' }}</el-descriptions-item>
            <el-descriptions-item label="到期时间">
              <span :style="{ color: isExpiringSoon ? 'red' : '' }">
                {{ licenseInfo.expiryDate || '-' }}
              </span>
            </el-descriptions-item>
            <el-descriptions-item label="用户数量">
              {{ licenseInfo.maxUsers || '-' }}
            </el-descriptions-item>
            <el-descriptions-item label="报表数量">
              {{ licenseInfo.maxReports || '-' }}
            </el-descriptions-item>
            <el-descriptions-item label="数据源数量">
              {{ licenseInfo.maxDataSources || '-' }}
            </el-descriptions-item>
          </el-descriptions>
          <el-empty v-else description="未激活许可证" />
        </el-card>
      </el-col>

      <el-col :span="12">
        <el-card>
          <template #header>
            <span>许可证激活</span>
          </template>
          <el-form :model="activateForm" :rules="rules" ref="formRef" label-width="100px">
            <el-form-item label="许可证密钥" prop="licenseKey">
              <el-input
                v-model="activateForm.licenseKey"
                type="textarea"
                :rows="6"
                placeholder="请输入许可证密钥"
              />
            </el-form-item>
            <el-form-item>
              <el-button type="primary" @click="handleActivate" :loading="activating">
                <el-icon><Key /></el-icon>
                激活许可证
              </el-button>
              <el-button @click="handleValidate" :loading="validating">
                <el-icon><CircleCheck /></el-icon>
                验证许可证
              </el-button>
            </el-form-item>
          </el-form>

          <el-alert
            type="info"
            title="许可证说明"
            :closable="false"
            style="margin-top: 20px;"
          >
            <p>试用许可证：有效期30天，无用户和报表数量限制</p>
            <p>正式许可证：需要联系销售获取，有效期根据购买时长而定</p>
          </el-alert>
        </el-card>
      </el-col>
    </el-row>

    <!-- 功能模块 -->
    <el-card style="margin-top: 20px;">
      <template #header>
        <span>功能模块</span>
      </template>
      <el-row :gutter="20">
        <el-col :span="8" v-for="feature in features" :key="feature.name">
          <div class="feature-item">
            <el-icon :size="30" :color="feature.enabled ? '#67C23A' : '#909399'">
              <component :is="feature.icon" />
            </el-icon>
            <div class="feature-info">
              <div class="feature-name">{{ feature.name }}</div>
              <div class="feature-status">
                <el-tag :type="feature.enabled ? 'success' : 'info'" size="small">
                  {{ feature.enabled ? '已启用' : '未启用' }}
                </el-tag>
              </div>
            </div>
          </div>
        </el-col>
      </el-row>
    </el-card>

    <!-- 使用统计 -->
    <el-card style="margin-top: 20px;">
      <template #header>
        <span>使用统计</span>
      </template>
      <el-row :gutter="20">
        <el-col :span="6">
          <div class="stat-item">
            <div class="stat-value">{{ stats.currentUsers }}</div>
            <div class="stat-label">当前用户数</div>
            <div class="stat-max">最大: {{ licenseInfo?.maxUsers || '-' }}</div>
          </div>
        </el-col>
        <el-col :span="6">
          <div class="stat-item">
            <div class="stat-value">{{ stats.currentReports }}</div>
            <div class="stat-label">当前报表数</div>
            <div class="stat-max">最大: {{ licenseInfo?.maxReports || '-' }}</div>
          </div>
        </el-col>
        <el-col :span="6">
          <div class="stat-item">
            <div class="stat-value">{{ stats.currentDataSources }}</div>
            <div class="stat-label">当前数据源数</div>
            <div class="stat-max">最大: {{ licenseInfo?.maxDataSources || '-' }}</div>
          </div>
        </el-col>
        <el-col :span="6">
          <div class="stat-item">
            <div class="stat-value">{{ remainingDays }}</div>
            <div class="stat-label">剩余天数</div>
            <div class="stat-max" :style="{ color: isExpiringSoon ? 'red' : '' }">
              {{ isExpiringSoon ? '即将到期' : '正常' }}
            </div>
          </div>
        </el-col>
      </el-row>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { licenseApi } from '../../api/request'

const formRef = ref()
const activating = ref(false)
const validating = ref(false)
const licenseInfo = ref(null)

const activateForm = reactive({
  licenseKey: ''
})

const rules = {
  licenseKey: [{ required: true, message: '请输入许可证密钥', trigger: 'blur' }]
}

const stats = reactive({
  currentUsers: 0,
  currentReports: 0,
  currentDataSources: 0
})

const features = ref([
  { name: '报表设计', icon: 'Edit', enabled: true },
  { name: '报表查询', icon: 'Search', enabled: true },
  { name: '图表展示', icon: 'DataLine', enabled: true },
  { name: 'Excel导出', icon: 'Download', enabled: true },
  { name: 'PDF导出', icon: 'Document', enabled: false },
  { name: '数据源管理', icon: 'Database', enabled: true }
])

const remainingDays = computed(() => {
  if (!licenseInfo.value?.expiryDate) return '-'
  const expiry = new Date(licenseInfo.value.expiryDate)
  const now = new Date()
  const diff = Math.floor((expiry - now) / (1000 * 60 * 60 * 24))
  return diff > 0 ? diff : 0
})

const isExpiringSoon = computed(() => {
  const days = remainingDays.value
  return typeof days === 'number' && days < 30 && days >= 0
})

onMounted(() => {
  loadLicense()
  loadStats()
})

const loadLicense = async () => {
  try {
    const res = await licenseApi.getLicense()
    if (res.success) {
      licenseInfo.value = res.data
      // 更新功能模块状态
      if (res.data.features) {
        features.value.forEach(f => {
          f.enabled = res.data.features.includes(f.name)
        })
      }
    }
  } catch (error) {
    console.error('加载许可证失败:', error)
  }
}

const loadStats = async () => {
  // TODO: 调用API获取统计数据
  stats.currentUsers = 5
  stats.currentReports = 12
  stats.currentDataSources = 3
}

const handleActivate = async () => {
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  activating.value = true
  try {
    const res = await licenseApi.activateLicense({
      licenseKey: activateForm.licenseKey
    })
    if (res.success) {
      ElMessage.success('许可证激活成功')
      licenseInfo.value = res.data
      activateForm.licenseKey = ''
    }
  } catch (error) {
    console.error('激活失败:', error)
  } finally {
    activating.value = false
  }
}

const handleValidate = async () => {
  validating.value = true
  try {
    const res = await licenseApi.validateLicense()
    if (res.success) {
      if (res.data.valid) {
        ElMessage.success('许可证有效')
      } else {
        ElMessage.warning(`许可证无效: ${res.data.message}`)
      }
    }
  } catch (error) {
    console.error('验证失败:', error)
  } finally {
    validating.value = false
  }
}

const getLicenseTypeText = (type) => {
  const map = {
    'Trial': '试用版',
    'Standard': '标准版',
    'Professional': '专业版',
    'Enterprise': '企业版'
  }
  return map[type] || type
}
</script>

<style scoped>
.license-management {
  height: 100%;
}

.feature-item {
  display: flex;
  align-items: center;
  padding: 15px;
  border: 1px solid #e4e7ed;
  border-radius: 4px;
  margin-bottom: 10px;
}

.feature-info {
  margin-left: 15px;
  flex: 1;
}

.feature-name {
  font-size: 14px;
  font-weight: bold;
  margin-bottom: 5px;
}

.feature-status {
  font-size: 12px;
}

.stat-item {
  text-align: center;
  padding: 20px;
  border: 1px solid #e4e7ed;
  border-radius: 4px;
}

.stat-value {
  font-size: 32px;
  font-weight: bold;
  color: #409EFF;
  margin-bottom: 10px;
}

.stat-label {
  font-size: 14px;
  color: #606266;
  margin-bottom: 5px;
}

.stat-max {
  font-size: 12px;
  color: #909399;
}
</style>
