<template>
  <div class="login-container">
    <div class="login-card">
      <div class="login-header">
        <h1>DataForgeStudio V4</h1>
        <p>报表管理系统</p>
      </div>

      <el-form ref="formRef" :model="loginForm" :rules="rules" @submit.prevent="handleLogin" class="login-form">
        <el-form-item prop="username">
          <el-input
            v-model="loginForm.username"
            placeholder="请输入用户名"
            prefix-icon="User"
            size="large"
          />
        </el-form-item>

        <el-form-item prop="password">
          <el-input
            v-model="loginForm.password"
            type="password"
            placeholder="请输入密码"
            prefix-icon="Lock"
            show-password
            size="large"
            @keyup.enter="handleLogin"
          />
        </el-form-item>

        <el-form-item>
          <el-checkbox v-model="loginForm.rememberMe">记住我</el-checkbox>
        </el-form-item>

        <el-form-item>
          <el-button
            type="primary"
            size="large"
            :loading="loading"
            @click="handleLogin"
            class="login-button"
          >
            {{ loading ? '登录中...' : '登 录' }}
          </el-button>
        </el-form-item>
      </el-form>

      <div class="login-footer">
        <p>默认用户: root / admin123</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useUserStore } from '../../stores/user'

const router = useRouter()
const userStore = useUserStore()

const formRef = ref()
const loading = ref(false)

const loginForm = reactive({
  username: '',
  password: '',
  rememberMe: false
})

const rules = {
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' }
  ]
}

// 页面加载时检查是否有保存的凭据
onMounted(() => {
  const savedUsername = localStorage.getItem('remembered_username')
  const savedPassword = localStorage.getItem('remembered_password')
  if (savedUsername) {
    loginForm.username = savedUsername
    loginForm.rememberMe = true
    if (savedPassword) {
      loginForm.password = savedPassword
    }
  }
})

// 保存/清除凭据
const saveCredentials = (username, password, remember) => {
  if (remember) {
    localStorage.setItem('remembered_username', username)
    localStorage.setItem('remembered_password', password)
  } else {
    localStorage.removeItem('remembered_username')
    localStorage.removeItem('remembered_password')
  }
}

const handleLogin = async () => {
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  loading.value = true

  const success = await userStore.login({
    username: loginForm.username,
    password: loginForm.password,
    rememberMe: loginForm.rememberMe
  })

  loading.value = false

  if (success) {
    // 登录成功后保存/清除凭据
    saveCredentials(loginForm.username, loginForm.password, loginForm.rememberMe)
    router.push('/home')
  }
}
</script>

<style scoped>
.login-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.login-card {
  width: 400px;
  padding: 40px;
  background: white;
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.login-header {
  text-align: center;
  margin-bottom: 30px;
}

.login-header h1 {
  font-size: 24px;
  color: #333;
  margin-bottom: 8px;
}

.login-header p {
  color: #666;
  font-size: 14px;
}

.login-form {
  margin-top: 20px;
}

.login-button {
  width: 100%;
}

.login-footer {
  margin-top: 20px;
  text-align: center;
}

.login-footer p {
  color: #999;
  font-size: 12px;
}
</style>
