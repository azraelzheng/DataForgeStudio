<template>
  <ErrorBoundary>
    <!-- 加载中状态 -->
    <div v-if="isAuthChecking" class="loading-container">
      <el-icon class="is-loading" :size="40"><Loading /></el-icon>
      <p>加载中...</p>
    </div>

    <!-- 登录页或未认证：显示登录组件 -->
    <router-view v-else-if="isLoginPage || !isAuthenticated" />

    <!-- 主应用：带侧边栏和顶部栏（仅认证后显示） -->
    <el-container v-else class="layout-container">
    <!-- 侧边栏 -->
    <el-aside :width="isCollapse ? '64px' : '200px'" class="sidebar">
      <div class="logo">
        <h2 v-if="!isCollapse">数据报表</h2>
        <h2 v-else>报表</h2>
      </div>

      <el-menu
        :default-active="activeMenu"
        :collapse="isCollapse"
        router
        background-color="#304156"
        text-color="#bfcbd9"
        active-text-color="#409EFF"
        class="sidebar-menu"
      >
        <el-menu-item index="/home">
          <el-icon><HomeFilled /></el-icon>
          <span>首页</span>
        </el-menu-item>

        <el-sub-menu index="report" v-if="userStore.hasAnyPermission(['report:query', 'report:design'])">
          <template #title>
            <el-icon><Document /></el-icon>
            <span>报表管理</span>
          </template>
          <el-menu-item index="/report/list" v-if="userStore.hasPermission('report:query')">
            <el-icon><List /></el-icon>
            <span>报表查询</span>
          </el-menu-item>
          <el-menu-item index="/report/design" v-if="userStore.hasPermission('report:design')">
            <el-icon><Edit /></el-icon>
            <span>报表设计</span>
          </el-menu-item>
        </el-sub-menu>

        <el-menu-item index="/license" v-if="userStore.hasPermission('license:view')">
          <el-icon><Key /></el-icon>
          <span>许可管理</span>
        </el-menu-item>

        <el-sub-menu index="system" v-if="userStore.hasAnyPermission(['user:view', 'role:view', 'datasource:view', 'log:view', 'backup:view'])">
          <template #title>
            <el-icon><Setting /></el-icon>
            <span>系统管理</span>
          </template>
          <el-menu-item index="/system/user" v-if="userStore.hasPermission('user:view')">
            <el-icon><User /></el-icon>
            <span>用户管理</span>
          </el-menu-item>
          <el-menu-item index="/system/role" v-if="userStore.hasPermission('role:view')">
            <el-icon><UserFilled /></el-icon>
            <span>权限组管理</span>
          </el-menu-item>
          <el-menu-item index="/system/datasource" v-if="userStore.hasPermission('datasource:view')">
            <el-icon><Coin /></el-icon>
            <span>数据源管理</span>
          </el-menu-item>
          <el-menu-item index="/system/log" v-if="userStore.hasPermission('log:view')">
            <el-icon><DocumentCopy /></el-icon>
            <span>日志管理</span>
          </el-menu-item>
          <el-menu-item index="/system/backup" v-if="userStore.hasPermission('backup:view')">
            <el-icon><FolderOpened /></el-icon>
            <span>备份管理</span>
          </el-menu-item>
        </el-sub-menu>
      </el-menu>
    </el-aside>

    <!-- 主内容区 -->
    <el-container>
      <!-- 顶部栏 -->
      <el-header class="header">
        <div class="header-left">
          <el-icon class="collapse-icon" @click="toggleCollapse">
            <Fold v-if="!isCollapse" />
            <Expand v-else />
          </el-icon>
          <el-breadcrumb separator="/">
            <el-breadcrumb-item>{{ currentRoute.meta.title || '首页' }}</el-breadcrumb-item>
          </el-breadcrumb>
        </div>

        <div class="header-right">
          <el-dropdown @command="handleCommand">
            <span class="user-info">
              <el-icon><User /></el-icon>
              {{ userStore.realName || userStore.username }}
            </span>
            <template #dropdown>
              <el-dropdown-item command="changePassword">修改密码</el-dropdown-item>
              <el-dropdown-item divided command="logout">退出登录</el-dropdown-item>
            </template>
          </el-dropdown>
        </div>
      </el-header>

      <!-- 主内容 -->
      <el-main class="main-content">
        <router-view v-slot="{ Component }">
          <keep-alive>
            <component :is="Component" :key="$route.fullPath" />
          </keep-alive>
        </router-view>
      </el-main>
    </el-container>
  </el-container>

  <!-- 修改密码对话框 -->
  <el-dialog
    v-model="passwordDialogVisible"
    title="修改密码"
    width="400px"
    @closed="handlePasswordDialogClosed"
  >
    <el-form :model="passwordForm" :rules="passwordRules" ref="passwordFormRef" label-width="100px">
      <el-form-item label="旧密码" prop="oldPassword">
        <el-input v-model="passwordForm.oldPassword" type="password" placeholder="请输入旧密码" show-password />
      </el-form-item>
      <el-form-item label="新密码" prop="newPassword">
        <el-input v-model="passwordForm.newPassword" type="password" placeholder="请输入新密码" show-password />
      </el-form-item>
      <el-form-item label="确认密码" prop="confirmPassword">
        <el-input v-model="passwordForm.confirmPassword" type="password" placeholder="请再次输入新密码" show-password />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="passwordDialogVisible = false">取消</el-button>
      <el-button type="primary" @click="handleChangePassword" :loading="passwordSubmitting">确定</el-button>
    </template>
  </el-dialog>
  </ErrorBoundary>
</template>

<script setup>
import { ref, computed, onMounted, reactive } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useUserStore } from './stores/user'
import { ElMessageBox, ElMessage } from 'element-plus'
import ErrorBoundary from './components/ErrorBoundary.vue'
import {
  HomeFilled,
  Fold,
  Expand,
  Document,
  List,
  Edit,
  Key,
  Setting,
  User,
  UserFilled,
  Coin,
  DocumentCopy,
  FolderOpened,
  Loading
} from '@element-plus/icons-vue'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()

const isCollapse = ref(false)
const isAuthChecking = ref(true)

const currentRoute = computed(() => route)

onMounted(async () => {
  const isValid = await userStore.checkAuth()
  isAuthChecking.value = false

  if (!isValid && route.path !== '/login') {
    router.push('/login')
  }
})

const activeMenu = computed(() => {
  const path = route.path
  // 匹配当前路由到菜单项
  if (path.startsWith('/report')) return '/report'
  if (path.startsWith('/system')) return '/system'
  return path
})

// 判断是否为登录页 - 同时检查路由和认证状态
const isLoginPage = computed(() => {
  // 登录页始终显示登录组件
  if (route.path === '/login') return true
  // 非登录页但未认证时，也显示登录组件（等待重定向）
  // 这样可以避免布局闪烁
  return false
})

// 判断是否已认证（用于显示布局）
const isAuthenticated = computed(() => {
  return !!userStore.token && !!userStore.userInfo
})

const toggleCollapse = () => {
  isCollapse.value = !isCollapse.value
}

const handleCommand = async (command) => {
  switch (command) {
    case 'changePassword':
      passwordDialogVisible.value = true
      break
    case 'logout':
      try {
        await ElMessageBox.confirm('确定要退出登录吗？', '提示', {
          confirmButtonText: '确定',
          cancelButtonText: '取消',
          type: 'warning'
        })
        userStore.logout()
        // 使用 window.location.href 强制页面重载，确保所有状态被清除
        window.location.href = '/login'
      } catch {
        // 用户取消
      }
      break
  }
}

// 修改密码相关
const passwordDialogVisible = ref(false)
const passwordSubmitting = ref(false)
const passwordFormRef = ref()
const passwordForm = reactive({
  oldPassword: '',
  newPassword: '',
  confirmPassword: ''
})

// 验证确认密码 - 使用函数确保获取最新值
const validateConfirmPassword = (rule, value, callback) => {
  if (value !== passwordForm.newPassword) {
    callback(new Error('两次输入的密码不一致'))
  } else {
    callback()
  }
}

const passwordRules = {
  oldPassword: [{ required: true, message: '请输入旧密码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 8, message: '密码长度至少8位', trigger: 'blur' },
    { pattern: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/, message: '密码必须包含大小写字母和数字', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, message: '请确认新密码', trigger: 'blur' },
    { validator: validateConfirmPassword, trigger: ['blur', 'change'] }
  ]
}

const handlePasswordDialogClosed = () => {
  passwordFormRef.value?.resetFields()
  passwordForm.oldPassword = ''
  passwordForm.newPassword = ''
  passwordForm.confirmPassword = ''
}

const handleChangePassword = async () => {
  const valid = await passwordFormRef.value.validate().catch(() => false)
  if (!valid) return

  passwordSubmitting.value = true
  try {
    const res = await userStore.changePassword({
      oldPassword: passwordForm.oldPassword,
      newPassword: passwordForm.newPassword
    })
    if (res) {
      ElMessage.success('密码修改成功，请重新登录')
      passwordDialogVisible.value = false
      // 退出登录
      userStore.logout()
      window.location.href = '/login'
    }
  } catch {
    // 密码修改失败已在 store 中处理
  } finally {
    passwordSubmitting.value = false
  }
}
</script>

<style scoped>
.layout-container {
  height: 100vh;
}

.sidebar {
  background-color: #304156;
  transition: width 0.3s;
}

.logo {
  height: 60px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
}

.logo h2 {
  font-size: 18px;
  margin: 0;
}

.sidebar-menu {
  border-right: none;
}

.header {
  background: #fff;
  border-bottom: 1px solid #e6e6e6;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
}

.collapse-icon {
  font-size: 18px;
  cursor: pointer;
  color: #606266;
}

.collapse-icon:hover {
  color: #409EFF;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 16px;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  padding: 8px 12px;
  border-radius: 4px;
  transition: background-color 0.3s;
}

.user-info:hover {
  background-color: #f5f5f5;
}

.main-content {
  background-color: #f0f2f5;
  padding: 20px;
  height: calc(100vh - 60px);
  overflow: hidden;
}

.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100vh;
  background-color: #f0f2f5;
}

.loading-container p {
  margin-top: 16px;
  color: #606266;
}
</style>
