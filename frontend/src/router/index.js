import { createRouter, createWebHistory } from 'vue-router'
import { useUserStore } from '../stores/user'
import { ElMessage } from 'element-plus'

const routes = [
  {
    path: '/',
    name: 'Dashboard',
    redirect: '/home',
    meta: { title: '仪表盘', requiresAuth: true }
  },
  {
    path: '/home',
    name: 'Home',
    component: () => import('../views/home/HomePage.vue'),
    meta: { title: '首页', requiresAuth: true }
  },
  {
    path: '/report',
    name: 'Report',
    redirect: '/report/list',
    meta: { title: '报表管理', requiresAuth: true }
  },
  {
    path: '/report/list',
    name: 'ReportList',
    component: () => import('../views/report/ReportList.vue'),
    meta: { title: '报表查询', requiresAuth: true, permission: 'report:view' }
  },
  {
    path: '/report/design',
    name: 'ReportDesign',
    component: () => import('../views/report/ReportDesign.vue'),
    meta: { title: '报表设计', requiresAuth: true, permission: 'report:design' }
  },
  {
    path: '/license',
    name: 'License',
    component: () => import('../views/license/LicenseManagement.vue'),
    meta: { title: '许可管理', requiresAuth: true, permission: 'license:view' }
  },
  {
    path: '/system',
    name: 'System',
    redirect: '/system/user',
    meta: { title: '系统管理', requiresAuth: true }
  },
  {
    path: '/system/user',
    name: 'SystemUser',
    component: () => import('../views/system/UserManagement.vue'),
    meta: { title: '用户管理', requiresAuth: true, permission: 'user:view' }
  },
  {
    path: '/system/role',
    name: 'SystemRole',
    component: () => import('../views/system/RoleManagement.vue'),
    meta: { title: '权限组管理', requiresAuth: true, permission: 'role:view' }
  },
  {
    path: '/system/datasource',
    name: 'SystemDataSource',
    component: () => import('../views/system/DataSourceManagement.vue'),
    meta: { title: '数据源管理', requiresAuth: true, permission: 'datasource:view' }
  },
  {
    path: '/system/log',
    name: 'SystemLog',
    component: () => import('../views/system/LogManagement.vue'),
    meta: { title: '日志管理', requiresAuth: true, permission: 'log:view' }
  },
  {
    path: '/system/backup',
    name: 'SystemBackup',
    component: () => import('../views/system/BackupManagement.vue'),
    meta: { title: '备份管理', requiresAuth: true, permission: 'backup:view' }
  },
  {
    path: '/login',
    name: 'Login',
    component: () => import('../views/auth/LoginPage.vue'),
    meta: { title: '登录', requiresAuth: false }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 路由守卫
router.beforeEach(async (to, from, next) => {
  // 设置页面标题
  document.title = to.meta.title ? `${to.meta.title} - DataForgeStudio V4` : 'DataForgeStudio V4'

  const userStore = useUserStore()

  // 避免重复导航
  if (to.path === from.path) {
    next()
    return
  }

  // 检查是否需要认证
  if (to.meta.requiresAuth) {
    // 使用 store 中的 isLoggedIn 判断，它会检查 token.value
    if (!userStore.isLoggedIn) {
      // 未登录，跳转到登录页
      console.log('Route guard: Not logged in, redirecting to login')
      next('/login')
      return
    }

    // 有 token，但需要验证是否有效（如果用户信息未加载）
    if (!userStore.userInfo) {
      try {
        await userStore.getCurrentUser()
        // 如果获取用户信息失败（token 过期），getCurrentUser 会自动调用 logout()
        if (!userStore.userInfo) {
          console.log('Route guard: Failed to get user info, redirecting to login')
          next('/login')
          return
        }
      } catch (error) {
        // token 无效，跳转到登录页
        console.log('Route guard: Token invalid, redirecting to login')
        next('/login')
        return
      }
    }
  }

  // 如果访问登录页且已经登录，跳转到首页
  if (to.path === '/login' && userStore.isLoggedIn) {
    console.log('Route guard: Already logged in, redirecting to home')
    next('/home')
    return
  }

  // 检查权限
  if (to.meta.permission) {
    if (!userStore.hasPermission(to.meta.permission)) {
      ElMessage.error(`您没有访问该页面的权限，需要权限：${to.meta.permission}`)
      next(from.path || '/')
      return
    }
  }

  next()
})

export default router
