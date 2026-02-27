<template>
  <!-- 关于对话框 - 产品信息卡片风格 -->
  <el-dialog
    v-model="aboutVisible"
    :show-close="false"
    width="420px"
    class="help-dialog about-dialog"
    :close-on-click-modal="true"
    @closed="handleClose('about')"
  >
    <div class="about-card">
      <!-- 顶部渐变装饰 -->
      <div class="about-header">
        <div class="about-logo">
          <svg viewBox="0 0 32 32" width="48" height="48">
            <rect width="32" height="32" rx="7" ry="7" fill="rgba(255,255,255,0.2)"/>
            <rect x="6" y="18" width="4" height="8" rx="1" fill="white"/>
            <rect x="14" y="12" width="4" height="14" rx="1" fill="white"/>
            <rect x="22" y="7" width="4" height="19" rx="1" fill="white"/>
          </svg>
        </div>
        <h1 class="about-title">{{ systemInfo.productName }}</h1>
        <p class="about-subtitle">企业级报表管理系统</p>
      </div>

      <!-- 产品信息 -->
      <div class="about-body">
        <div class="info-row">
          <span class="info-label">版本号</span>
          <span class="info-value version-badge">V{{ systemInfo.version }}</span>
        </div>
        <div class="info-row">
          <span class="info-label">版权所有</span>
          <span class="info-value">{{ systemInfo.copyright }}</span>
        </div>
        <div class="info-row" v-if="systemInfo.company">
          <span class="info-label">开发商</span>
          <span class="info-value">{{ systemInfo.company }}</span>
        </div>
      </div>

      <!-- 底部装饰 -->
      <div class="about-footer">
        <p>Powered by DataForgeStudio Team</p>
      </div>
    </div>

    <template #footer>
      <div class="dialog-footer">
        <el-button type="primary" @click="closeAbout">关 闭</el-button>
      </div>
    </template>
  </el-dialog>

  <!-- 文档查看对话框 - 专业文档阅读风格 -->
  <el-dialog
    v-model="documentVisible"
    :show-close="false"
    width="680px"
    class="help-dialog document-dialog"
    :close-on-click-modal="true"
    @closed="handleClose('document')"
  >
    <template #header>
      <div class="document-header">
        <div class="document-icon" :class="documentType">
          <el-icon :size="24">
            <component :is="documentIcon" />
          </el-icon>
        </div>
        <div class="document-title-wrap">
          <h2 class="document-title">{{ documentTitle }}</h2>
          <p class="document-meta">最后更新：{{ lastUpdateDate }}</p>
        </div>
      </div>
    </template>

    <div class="document-content">
      <div class="document-body" v-html="documentContent"></div>
    </div>

    <template #footer>
      <div class="dialog-footer">
        <el-button type="primary" @click="closeDocument">关 闭</el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { Document, Reading, Lock } from '@element-plus/icons-vue'

// Props
const props = defineProps({
  // 关于对话框
  aboutVisible: {
    type: Boolean,
    default: false
  },
  systemInfo: {
    type: Object,
    default: () => ({
      productName: 'DataForgeStudio',
      version: '',
      copyright: '',
      company: ''
    })
  },
  // 文档对话框
  documentVisible: {
    type: Boolean,
    default: false
  },
  documentTitle: {
    type: String,
    default: ''
  },
  documentContent: {
    type: String,
    default: ''
  },
  documentType: {
    type: String,
    default: 'manual' // manual, eula, privacy
  }
})

// Emits
const emit = defineEmits(['update:aboutVisible', 'update:documentVisible', 'closed'])

// 内部状态
const aboutVisible = computed({
  get: () => props.aboutVisible,
  set: (val) => emit('update:aboutVisible', val)
})

const documentVisible = computed({
  get: () => props.documentVisible,
  set: (val) => emit('update:documentVisible', val)
})

// 文档图标
const documentIcon = computed(() => {
  switch (props.documentType) {
    case 'eula':
      return Document
    case 'privacy':
      return Lock
    default:
      return Reading
  }
})

// 最后更新日期
const lastUpdateDate = computed(() => {
  return new Date().toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
})

// 关闭方法
const closeAbout = () => {
  aboutVisible.value = false
}

const closeDocument = () => {
  documentVisible.value = false
}

const handleClose = (type) => {
  emit('closed', type)
}
</script>

<style scoped>
/* 对话框基础样式 */
.help-dialog :deep(.el-dialog) {
  border-radius: 12px;
  overflow: hidden;
}

.help-dialog :deep(.el-dialog__header) {
  padding: 0;
  margin: 0;
}

.help-dialog :deep(.el-dialog__body) {
  padding: 0;
}

.help-dialog :deep(.el-dialog__footer) {
  padding: 16px 24px;
  border-top: 1px solid #ebeef5;
  background: #fafafa;
}

/* ========== 关于对话框 ========== */
.about-card {
  background: #fff;
}

.about-header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 40px 24px;
  text-align: center;
  color: #fff;
}

.about-logo {
  width: 80px;
  height: 80px;
  background: rgba(255, 255, 255, 0.2);
  border-radius: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 16px;
  backdrop-filter: blur(10px);
  border: 1px solid rgba(255, 255, 255, 0.3);
}

.about-title {
  font-size: 28px;
  font-weight: 600;
  margin: 0 0 8px;
  letter-spacing: 0.5px;
}

.about-subtitle {
  font-size: 14px;
  margin: 0;
  opacity: 0.9;
}

.about-body {
  padding: 24px;
}

.info-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 0;
  border-bottom: 1px solid #f0f0f0;
}

.info-row:last-child {
  border-bottom: none;
}

.info-label {
  color: #909399;
  font-size: 14px;
}

.info-value {
  color: #303133;
  font-size: 14px;
  font-weight: 500;
}

.version-badge {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: #fff;
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 13px;
  font-weight: 500;
}

.about-footer {
  padding: 16px 24px;
  text-align: center;
  background: #fafafa;
  border-top: 1px solid #ebeef5;
}

.about-footer p {
  margin: 0;
  color: #909399;
  font-size: 12px;
}

/* ========== 文档对话框 ========== */
.document-header {
  display: flex;
  align-items: center;
  padding: 20px 24px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: #fff;
}

.document-icon {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 16px;
  background: rgba(255, 255, 255, 0.2);
  backdrop-filter: blur(10px);
  border: 1px solid rgba(255, 255, 255, 0.3);
}

.document-icon.eula {
  background: rgba(103, 194, 58, 0.3);
}

.document-icon.privacy {
  background: rgba(230, 162, 60, 0.3);
}

.document-title-wrap {
  flex: 1;
}

.document-title {
  font-size: 20px;
  font-weight: 600;
  margin: 0 0 4px;
}

.document-meta {
  font-size: 12px;
  margin: 0;
  opacity: 0.8;
}

.document-content {
  max-height: 60vh;
  overflow-y: auto;
}

.document-body {
  padding: 24px 32px;
  line-height: 1.8;
  color: #303133;
  font-size: 14px;
}

/* 文档内容样式 */
.document-body :deep(h1) {
  font-size: 20px;
  font-weight: 600;
  color: #303133;
  margin: 24px 0 16px;
  padding-bottom: 8px;
  border-bottom: 2px solid #667eea;
}

.document-body :deep(h2) {
  font-size: 17px;
  font-weight: 600;
  color: #303133;
  margin: 20px 0 12px;
}

.document-body :deep(h3) {
  font-size: 15px;
  font-weight: 600;
  color: #303133;
  margin: 16px 0 10px;
}

.document-body :deep(p) {
  margin: 0 0 12px;
  text-align: justify;
}

.document-body :deep(ul),
.document-body :deep(ol) {
  margin: 12px 0;
  padding-left: 24px;
}

.document-body :deep(li) {
  margin: 8px 0;
}

.document-body :deep(br) {
  display: block;
  content: "";
  margin-top: 8px;
}

/* 底部按钮 */
.dialog-footer {
  display: flex;
  justify-content: center;
}

.dialog-footer .el-button--primary {
  min-width: 100px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border: none;
}

.dialog-footer .el-button--primary:hover {
  background: linear-gradient(135deg, #5a6fd6 0%, #6a4191 100%);
}
</style>
