import { createRouter, createWebHistory } from 'vue-router'
import { useUserStore } from '../stores/user'
import { useLicenseStore } from '../stores/license'
import { ElMessage } from 'element-plus'

// 需要有效许可证才能访问的路由
const LICENSE_REQUIRED_ROUTES = [
  '/report/design',
  '/report/designer',
  '/report/list',
  '/system/datasource'
]

const routes = [
  {
    path: '/',
    redirect: '/home'
  },
  {
    path: '/home',
    name: 'Home',
    component: () => import('../views/home/HomePage.vue'),
    meta: { title: '首页', requiresAuth: true }
  },
  // 报表模块重定向
  {
    path: '/report',
    redirect: '/report/list'
  },
  // 报表查询 - 纯查询页面
  {
    path: '/report/list',
    name: 'ReportQuery',
    component: () => import('../views/report/ReportQuery.vue'),
    meta: { title: '报表查询', requiresAuth: true, permission: 'report:execute' }
  },
  // 报表设计列表 - 设计管理页面
  {
    path: '/report/design',
    name: 'ReportDesignList',
    component: () => import('../views/report/ReportDesignList.vue'),
    meta: { title: '报表设计', requiresAuth: true, permission: 'report:design' }
  },
  // 报表设计器 - 实际设计页面
  {
    path: '/report/designer',
    name: 'ReportDesigner',
    component: () => import('../views/report/ReportDesigner.vue'),
    meta: { title: '报表设计器', requiresAuth: true, permission: 'report:design' }
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
  // 大屏模块
  {
    path: '/dashboard',
    name: 'Dashboard',
    redirect: '/dashboard/list'
  },
  {
    path: '/dashboard/list',
    name: 'DashboardList',
    component: () => import('../views/dashboard/DashboardList.vue'),
    meta: { title: '大屏管理', requiresAuth: true, permission: 'dashboard:view' }
  },
  {
    path: '/dashboard/designer/:id?',
    name: 'DashboardDesigner',
    component: () => import('../views/dashboard/DashboardDesigner.vue'),
    meta: { title: '大屏设计器', requiresAuth: true, permission: 'dashboard:edit' }
  },
  {
    path: '/dashboard/view/:id',
    name: 'DashboardView',
    component: () => import('../views/dashboard/DashboardView.vue'),
    meta: { title: '大屏展示', requiresAuth: true, permission: 'dashboard:view' }
  },
  {
    path: '/public/d/:id',
    name: 'PublicDashboard',
    component: () => import('../views/dashboard/PublicDashboard.vue'),
    meta: { title: '大屏', requiresAuth: false }
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

router.beforeEach(async (to, from, next) => {
  document.title = to.meta.title ? `${to.meta.title} - DataForgeStudio` : 'DataForgeStudio'

  const userStore = useUserStore()
  const requiresAuth = to.meta.requiresAuth !== false
  const isLoginPage = to.path === '/login'

  if (isLoginPage) {
    const hasToken = !!localStorage.getItem('token')
    if (hasToken && userStore.userInfo) {
      next('/home')
      return
    }
    next()
    return
  }

  if (requiresAuth) {
    const hasToken = !!localStorage.getItem('token')

    if (!hasToken) {
      next('/login')
      return
    }

    if (!userStore.userInfo) {
      try {
        await userStore.getCurrentUser()
        if (!userStore.userInfo) {
          next('/login')
          return
        }
      } catch {
        next('/login')
        return
      }
    }
  }

  if (to.meta.permission && !userStore.hasPermission(to.meta.permission)) {
    ElMessage.error(`您没有访问该页面的权限，需要权限：${to.meta.permission}`)
    next(from.path || '/home')
    return
  }

  if (LICENSE_REQUIRED_ROUTES.some(route => to.path.startsWith(route))) {
    const licenseStore = useLicenseStore()

    if (!licenseStore.license && !licenseStore.licenseStatus) {
      await licenseStore.loadLicense()
    }

    if (licenseStore.isExpired || licenseStore.licenseStatus === 'expired') {
      ElMessage.error('许可证已过期，无法访问此功能。请续费后继续使用。')
      next('/license')
      return
    }

    if (licenseStore.licenseStatus === 'invalid' || !licenseStore.license) {
      ElMessage.error('许可证无效，请先激活许可证。')
      next('/license')
      return
    }
  }

  next()
})

export default router
