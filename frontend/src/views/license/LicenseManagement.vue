<template>
  <div class="license-management">
    <!-- 许可证状态警告 -->
    <el-alert
      v-if="licenseStore.warningMessage"
      :type="licenseStore.warningLevel"
      :title="licenseStore.warningMessage"
      :closable="false"
      style="margin-bottom: 20px;"
      show-icon
    />

    <el-row :gutter="20">
      <el-col :span="12">
        <!-- 许可证信息 -->
        <el-card>
          <template #header>
            <div class="card-header">
              <span>许可证信息</span>
              <el-button
                type="primary"
                size="small"
                @click="handleRefresh"
                :loading="refreshing"
              >
                <el-icon><Refresh /></el-icon>
                刷新
              </el-button>
            </div>
          </template>
          <el-descriptions :column="1" border v-if="licenseStore.license">
            <el-descriptions-item label="产品名称">DataForgeStudio V4</el-descriptions-item>
            <el-descriptions-item label="许可证类型">
              <el-tag :type="licenseStore.isTrial ? 'warning' : 'success'">
                {{ getLicenseTypeText(licenseStore.license.licenseType) }}
              </el-tag>
            </el-descriptions-item>
            <el-descriptions-item label="客户名称">
              {{ licenseStore.license.customerName || '-' }}
            </el-descriptions-item>
            <el-descriptions-item label="到期时间">
              <span :style="{ color: licenseStore.isExpired ? 'red' : (licenseStore.isExpiringSoon ? 'orange' : '') }">
                {{ formatDate(licenseStore.license.expiryDate) }}
              </span>
              <el-tag
                v-if="licenseStore.isExpired"
                type="danger"
                size="small"
                style="margin-left: 10px;"
              >
                已过期
              </el-tag>
              <el-tag
                v-else-if="licenseStore.isExpiringSoon"
                type="warning"
                size="small"
                style="margin-left: 10px;"
              >
                即将到期
              </el-tag>
            </el-descriptions-item>
            <el-descriptions-item label="剩余天数">
              <span :style="{ color: licenseStore.isExpiringSoon ? 'orange' : '' }">
                {{ licenseStore.daysRemaining !== null ? `${licenseStore.daysRemaining} 天` : '-' }}
              </span>
            </el-descriptions-item>
            <el-descriptions-item label="用户数量">
              {{ stats.currentUsers }} / {{ licenseStore.license.maxUsers || '无限制' }}
            </el-descriptions-item>
            <el-descriptions-item label="报表数量">
              {{ stats.currentReports }} / {{ licenseStore.license.maxReports || '无限制' }}
            </el-descriptions-item>
            <el-descriptions-item label="数据源数量">
              {{ stats.currentDataSources }} / {{ licenseStore.license.maxDataSources || '无限制' }}
            </el-descriptions-item>
          </el-descriptions>
          <el-empty v-else description="未激活许可证" />
        </el-card>

        <!-- 使用统计 -->
        <el-card style="margin-top: 20px;">
          <template #header>
            <span>使用统计</span>
          </template>
          <el-row :gutter="20">
            <el-col :span="8">
              <div class="stat-item">
                <div class="stat-value" :style="{ color: getUserCountColor() }">
                  {{ stats.currentUsers }}
                </div>
                <div class="stat-label">当前用户数</div>
                <div class="stat-max">
                  最大: {{ licenseStore.license?.maxUsers || '-' }}
                </div>
              </div>
            </el-col>
            <el-col :span="8">
              <div class="stat-item">
                <div class="stat-value" :style="{ color: getReportCountColor() }">
                  {{ stats.currentReports }}
                </div>
                <div class="stat-label">当前报表数</div>
                <div class="stat-max">
                  最大: {{ licenseStore.license?.maxReports || '-' }}
                </div>
              </div>
            </el-col>
            <el-col :span="8">
              <div class="stat-item">
                <div class="stat-value" :style="{ color: getDataSourceCountColor() }">
                  {{ stats.currentDataSources }}
                </div>
                <div class="stat-label">当前数据源数</div>
                <div class="stat-max">
                  最大: {{ licenseStore.license?.maxDataSources || '-' }}
                </div>
              </div>
            </el-col>
          </el-row>
        </el-card>
      </el-col>

      <el-col :span="12">
        <!-- 机器码 -->
        <el-card>
          <template #header>
            <span>机器码</span>
          </template>
          <div class="machine-code-container">
            <el-input
              v-model="machineCode"
              type="textarea"
              :rows="3"
              readonly
              placeholder="加载中..."
            />
            <el-button
              type="primary"
              @click="handleCopyMachineCode"
              style="margin-top: 10px;"
              :disabled="!machineCode"
            >
              <el-icon><CopyDocument /></el-icon>
              复制机器码
            </el-button>
            <el-alert
              type="info"
              :closable="false"
              style="margin-top: 15px;"
            >
              <template #title>
                <div style="font-size: 12px;">
                  请将此机器码提供给供应商，以获取适用于当前服务器的许可证文件。
                </div>
              </template>
            </el-alert>
          </div>
        </el-card>

        <!-- 许可证激活 -->
        <el-card style="margin-top: 20px;">
          <template #header>
            <span>许可证激活</span>
          </template>
          <el-form :model="activateForm" :rules="rules" ref="formRef" label-width="100px">
            <el-form-item label="许可证文件" prop="licenseFile">
              <el-upload
                ref="uploadRef"
                :auto-upload="false"
                :limit="1"
                :on-change="handleFileChange"
                :on-remove="handleFileRemove"
                accept=".lic"
                drag
              >
                <el-icon class="el-icon--upload"><UploadFilled /></el-icon>
                <div class="el-upload__text">
                  将.lic文件拖到此处，或<em>点击上传</em>
                </div>
                <template #tip>
                  <div class="el-upload__tip">
                    请上传供应商提供的许可证文件（.lic格式）
                  </div>
                </template>
              </el-upload>
            </el-form-item>
            <el-form-item>
              <el-button
                type="primary"
                @click="handleActivate"
                :loading="activating"
                :disabled="!activateForm.licenseFile"
              >
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
            <template #default>
              <div style="font-size: 12px; line-height: 1.8;">
                <p><strong>试用许可证：</strong>有效期30天，无用户和报表数量限制</p>
                <p><strong>正式许可证：</strong>需要联系销售获取，有效期根据购买时长而定</p>
                <p><strong>许可证绑定：</strong>许可证与服务器机器码绑定，不可迁移</p>
              </div>
            </template>
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

    <!-- 许可证限制 -->
    <el-card v-if="licenseStore.license && hasRestrictions()" style="margin-top: 20px;">
      <template #header>
        <span>许可证限制</span>
      </template>
      <el-alert
        v-if="licenseStore.restrictions.isReadOnly"
        type="error"
        title="只读模式"
        :closable="false"
        style="margin-bottom: 10px;"
      >
        许可证已过期，系统处于只读模式，无法进行创建、修改和删除操作。
      </el-alert>
      <el-descriptions :column="2" border>
        <el-descriptions-item label="用户限制">
          {{ licenseStore.license.maxUsers ? `最多 ${licenseStore.license.maxUsers} 个用户` : '无限制' }}
        </el-descriptions-item>
        <el-descriptions-item label="报表限制">
          {{ licenseStore.license.maxReports ? `最多 ${licenseStore.license.maxReports} 个报表` : '无限制' }}
        </el-descriptions-item>
        <el-descriptions-item label="数据源限制">
          {{ licenseStore.license.maxDataSources ? `最多 ${licenseStore.license.maxDataSources} 个数据源` : '无限制' }}
        </el-descriptions-item>
        <el-descriptions-item label="许可证状态">
          <el-tag :type="getLicenseStatusType()">
            {{ getLicenseStatusText() }}
          </el-tag>
        </el-descriptions-item>
      </el-descriptions>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { useLicenseStore } from '../../stores/license'
import {
  Refresh,
  CopyDocument,
  Key,
  CircleCheck,
  UploadFilled,
  Edit,
  Search,
  DataLine,
  Download,
  Document,
  Coin
} from '@element-plus/icons-vue'

const licenseStore = useLicenseStore()
const formRef = ref()
const uploadRef = ref()
const activating = ref(false)
const validating = ref(false)
const refreshing = ref(false)
const machineCode = ref('')

const activateForm = reactive({
  licenseFile: null
})

const rules = {
  licenseFile: [{ required: true, message: '请上传许可证文件', trigger: 'change' }]
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
  { name: '数据源管理', icon: 'Coin', enabled: true }
])

onMounted(async () => {
  await loadMachineCode()
  await loadLicense()
  await loadStats()
})

const loadMachineCode = async () => {
  const code = await licenseStore.getMachineCode()
  if (code) {
    machineCode.value = code
  }
}

const loadLicense = async () => {
  await licenseStore.loadLicense()
  updateFeatures()
}

const loadStats = async () => {
  // TODO: 调用API获取统计数据
  stats.currentUsers = 1
  stats.currentReports = 0
  stats.currentDataSources = 0
}

const updateFeatures = () => {
  if (licenseStore.license?.features) {
    features.value.forEach(f => {
      f.enabled = licenseStore.hasFeature(f.name)
    })
  }
}

const handleCopyMachineCode = () => {
  navigator.clipboard.writeText(machineCode.value).then(() => {
    ElMessage.success('机器码已复制到剪贴板')
  }).catch(() => {
    ElMessage.error('复制失败，请手动复制')
  })
}

const handleFileChange = (file) => {
  activateForm.licenseFile = file.raw
}

const handleFileRemove = () => {
  activateForm.licenseFile = null
}

const handleActivate = async () => {
  if (!activateForm.licenseFile) {
    ElMessage.warning('请先上传许可证文件')
    return
  }

  activating.value = true
  try {
    // 读取文件内容
    const licenseKey = await readFileAsText(activateForm.licenseFile)

    // 转换为 base64
    const base64Key = btoa(licenseKey)

    // 激活许可证
    const success = await licenseStore.activateLicense(base64Key)
    if (success) {
      activateForm.licenseFile = null
      uploadRef.value?.clearFiles()
      await loadLicense()
    }
  } catch (error) {
    console.error('激活失败:', error)
    ElMessage.error('许可证激活失败: ' + error.message)
  } finally {
    activating.value = false
  }
}

const readFileAsText = (file) => {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = (e) => resolve(e.target.result)
    reader.onerror = (e) => reject(e)
    reader.readAsText(file)
  })
}

const handleValidate = async () => {
  validating.value = true
  try {
    const valid = await licenseStore.validateLicense(true)
    if (valid) {
      ElMessage.success('许可证有效')
    } else {
      ElMessage.warning('许可证无效或已过期')
    }
  } finally {
    validating.value = false
  }
}

const handleRefresh = async () => {
  refreshing.value = true
  try {
    await licenseStore.validateLicense(true)
    await loadLicense()
    ElMessage.success('刷新成功')
  } finally {
    refreshing.value = false
  }
}

const formatDate = (dateStr) => {
  if (!dateStr) return '-'
  const date = new Date(dateStr)
  return date.toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  })
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

const getLicenseStatusType = () => {
  if (licenseStore.isExpired) return 'danger'
  if (licenseStore.isExpiringSoon) return 'warning'
  if (licenseStore.isTrial) return 'info'
  return 'success'
}

const getLicenseStatusText = () => {
  if (licenseStore.isExpired) return '已过期'
  if (licenseStore.isExpiringSoon) return '即将到期'
  if (licenseStore.isTrial) return '试用版'
  return '正常'
}

const hasRestrictions = () => {
  return licenseStore.license &&
    (licenseStore.restrictions.isReadOnly ||
     licenseStore.license.maxUsers ||
     licenseStore.license.maxReports ||
     licenseStore.license.maxDataSources)
}

const getUserCountColor = () => {
  if (!licenseStore.license?.maxUsers) return '#409EFF'
  const ratio = stats.currentUsers / licenseStore.license.maxUsers
  if (ratio >= 1) return '#F56C6C'
  if (ratio >= 0.8) return '#E6A23C'
  return '#409EFF'
}

const getReportCountColor = () => {
  if (!licenseStore.license?.maxReports) return '#409EFF'
  const ratio = stats.currentReports / licenseStore.license.maxReports
  if (ratio >= 1) return '#F56C6C'
  if (ratio >= 0.8) return '#E6A23C'
  return '#409EFF'
}

const getDataSourceCountColor = () => {
  if (!licenseStore.license?.maxDataSources) return '#409EFF'
  const ratio = stats.currentDataSources / licenseStore.license.maxDataSources
  if (ratio >= 1) return '#F56C6C'
  if (ratio >= 0.8) return '#E6A23C'
  return '#409EFF'
}
</script>

<style scoped>
.license-management {
  height: 100%;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.machine-code-container {
  padding: 10px 0;
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

:deep(.el-alert__title) {
  font-size: 14px;
}

:deep(.el-upload-dragger) {
  width: 100%;
}
</style>
