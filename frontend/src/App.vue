<template>
  <ErrorBoundary>
    <!-- 登录页：独立页面，无侧边栏和顶部栏 -->
    <router-view v-if="isLoginPage" />

    <!-- 主应用：带侧边栏和顶部栏 -->
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

        <el-sub-menu index="report" v-if="userStore.hasAnyPermission(['report:view', 'report:design'])">
          <template #title>
            <el-icon><Document /></el-icon>
            <span>报表管理</span>
          </template>
          <el-menu-item index="/report/list" v-if="userStore.hasPermission('report:view')">
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
              <el-dropdown-item command="profile">个人资料</el-dropdown-item>
              <el-dropdown-item command="settings">设置</el-dropdown-item>
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
  </ErrorBoundary>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
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
  FolderOpened
} from '@element-plus/icons-vue'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()

const isCollapse = ref(false)

const currentRoute = computed(() => route)

// 页面加载时检查认证状态
onMounted(async () => {
  // 不在登录页时才检查认证
  if (route.path !== '/login') {
    const isValid = await userStore.checkAuth()
    console.log('App mounted - auth check result:', isValid)
  }
})

const activeMenu = computed(() => {
  const path = route.path
  // 匹配当前路由到菜单项
  if (path.startsWith('/report')) return '/report'
  if (path.startsWith('/system')) return '/system'
  return path
})

// 判断是否为登录页
const isLoginPage = computed(() => {
  return route.path === '/login'
})

const toggleCollapse = () => {
  isCollapse.value = !isCollapse.value
}

const handleCommand = async (command) => {
  switch (command) {
    case 'profile':
      ElMessage.info('个人资料功能待开发')
      break
    case 'settings':
      ElMessage.info('设置功能待开发')
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
}
</style>
