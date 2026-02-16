<template>
  <div class="user-management">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>用户管理</span>
          <el-button type="primary" @click="handleAdd">
            <el-icon><Plus /></el-icon>
            新增用户
          </el-button>
        </div>
      </template>

      <!-- 搜索表单 -->
      <el-form :inline="true" :model="searchForm" class="search-form">
        <el-form-item label="用户名">
          <el-input v-model="searchForm.username" placeholder="请输入用户名" clearable />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="searchForm.isActive" placeholder="请选择状态" clearable>
            <el-option label="全部" value="" />
            <el-option label="启用" :value="true" />
            <el-option label="禁用" :value="false" />
          </el-select>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">
            <el-icon><Search /></el-icon>
            查询
          </el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>

      <!-- 用户表格 -->
      <template v-if="tableData && tableData.length > 0">
        <el-table :data="tableData" v-loading="loading" border stripe>
          <el-table-column prop="username" label="用户名" width="180">
            <template #default="{ row }">
              <span>{{ row.username }}</span>
              <el-tag v-if="row.username === 'root'" type="danger" size="small" style="margin-left: 8px;">系统管理员</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="realName" label="真实姓名" width="150" />
          <el-table-column prop="email" label="邮箱" min-width="200" />
          <el-table-column prop="phone" label="手机号" width="150" />
          <el-table-column label="角色" width="200">
            <template #default="{ row }">
              <el-tag
                v-for="role in row.roles"
                :key="role.roleId"
                size="small"
                style="margin-right: 5px;"
              >
                {{ role.roleName }}
              </el-tag>
              <el-tag v-if="row.username === 'root'" type="danger" size="small">全部权限</el-tag>
              <span v-if="(!row.roles || row.roles.length === 0) && row.username !== 'root'">-</span>
            </template>
          </el-table-column>
          <el-table-column label="状态" width="100" align="center">
            <template #default="{ row }">
              <el-switch
                v-model="row.isActive"
                @change="handleToggleStatus(row)"
                :disabled="row.username === 'root'"
              />
            </template>
          </el-table-column>
          <el-table-column prop="lastLoginTime" label="最后登录" width="180" />
          <el-table-column prop="createdTime" label="创建时间" width="180" />
          <el-table-column label="操作" width="280" fixed="right">
            <template #default="{ row }">
              <el-button
                type="primary"
                link
                size="small"
                @click="handleEdit(row)"
                :disabled="row.username === 'root'"
              >
                <el-icon><Edit /></el-icon>
                编辑
              </el-button>
              <el-button
                type="primary"
                link
                size="small"
                @click="handleAssignRoles(row)"
                :disabled="row.username === 'root'"
              >
                <el-icon><UserFilled /></el-icon>
                分配角色
              </el-button>
              <el-button
                type="warning"
                link
                size="small"
                @click="handleResetPassword(row)"
                :disabled="row.username === 'root'"
              >
                <el-icon><RefreshLeft /></el-icon>
                重置密码
              </el-button>
              <el-button
                type="danger"
                link
                size="small"
                @click="handleDelete(row)"
                :disabled="row.username === 'root'"
              >
                <el-icon><Delete /></el-icon>
                删除
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
      <el-empty v-else-if="!loading" description="暂无用户数据" />

      <!-- 分页 -->
      <el-pagination
        v-model:current-page="pagination.page"
        v-model:page-size="pagination.pageSize"
        :page-sizes="[10, 20, 50, 100]"
        :total="pagination.total"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="loadData"
        @current-change="loadData"
        style="margin-top: 20px; justify-content: flex-end;"
      />
    </el-card>

    <!-- 用户编辑对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="isEdit ? '编辑用户' : '新增用户'"
      width="600px"
      @closed="handleDialogClosed"
    >
      <el-form :model="form" :rules="rules" ref="formRef" label-width="100px">
        <el-form-item label="用户名" prop="username">
          <el-input v-model="form.username" placeholder="请输入用户名" :disabled="isEdit" />
        </el-form-item>
        <el-form-item label="密码" prop="password" v-if="!isEdit">
          <el-input v-model="form.password" type="password" placeholder="请输入密码" show-password />
        </el-form-item>
        <el-form-item label="真实姓名" prop="realName">
          <el-input v-model="form.realName" placeholder="请输入真实姓名" />
        </el-form-item>
        <el-form-item label="邮箱" prop="email">
          <el-input v-model="form.email" placeholder="请输入邮箱" />
        </el-form-item>
        <el-form-item label="手机号" prop="phone">
          <el-input v-model="form.phone" placeholder="请输入手机号" />
        </el-form-item>
        <el-form-item label="角色" prop="roleIds">
          <el-select
            v-model="form.roleIds"
            multiple
            placeholder="请选择角色"
            style="width: 100%"
          >
            <el-option
              v-for="role in allRoles"
              :key="role.roleId"
              :label="role.roleName"
              :value="role.roleId"
            >
              <span>{{ role.roleName }}</span>
              <el-tag v-if="role.isSystem" size="small" type="info" style="margin-left: 5px;">系统</el-tag>
            </el-option>
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-switch v-model="form.isActive" active-text="启用" inactive-text="禁用" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">
          确定
        </el-button>
      </template>
    </el-dialog>

    <!-- 分配角色对话框 -->
    <el-dialog v-model="roleDialogVisible" title="分配角色" width="500px">
      <el-checkbox-group v-model="selectedRoles">
        <el-checkbox
          v-for="role in allRoles"
          :key="role.roleId"
          :label="role.roleId"
        >
          {{ role.roleName }}
          <el-tag v-if="role.isSystem" size="small" type="info" style="margin-left: 5px;">系统</el-tag>
        </el-checkbox>
      </el-checkbox-group>
      <template #footer>
        <el-button @click="roleDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSaveRoles" :loading="savingRoles">
          确定
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { userApi, roleApi } from '../../api/request'

const formRef = ref()
const loading = ref(false)
const dialogVisible = ref(false)
const roleDialogVisible = ref(false)
const isEdit = ref(false)
const submitting = ref(false)
const savingRoles = ref(false)

const tableData = ref([])
const allRoles = ref([])
const selectedRoles = ref([])
const currentUser = ref(null)

const searchForm = reactive({
  username: '',
  isActive: ''
})

const form = reactive({
  userId: null,
  username: '',
  password: '',
  realName: '',
  email: '',
  phone: '',
  roleIds: [],
  isActive: true
})

const rules = {
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 3, max: 50, message: '用户名长度在 3 到 50 个字符', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码长度至少 6 个字符', trigger: 'blur' }
  ],
  realName: [{ required: true, message: '请输入真实姓名', trigger: 'blur' }],
  email: [
    { type: 'email', message: '请输入正确的邮箱地址', trigger: 'blur' }
  ]
}

const pagination = reactive({
  page: 1,
  pageSize: 20,
  total: 0
})

onMounted(() => {
  loadData()
  loadRoles()
})

const loadData = async () => {
  loading.value = true
  try {
    const res = await userApi.getUsers({
      page: pagination.page,
      pageSize: pagination.pageSize,
      ...searchForm
    })
    if (res.success) {
      // 处理 PascalCase (后端) 和 camelCase 两种情况
      const data = res.data
      tableData.value = data.Items || data.items || []
      pagination.total = data.TotalCount || data.total || 0
    }
  } catch (error) {
    console.error('加载数据失败:', error)
  } finally {
    loading.value = false
  }
}

const loadRoles = async () => {
  try {
    const res = await roleApi.getRoles({ page: 1, pageSize: 1000 })
    if (res.success) {
      const data = res.data
      allRoles.value = data.Items || data.items || []
    }
  } catch (error) {
    console.error('加载角色失败:', error)
  }
}

const handleSearch = () => {
  pagination.page = 1
  loadData()
}

const handleReset = () => {
  searchForm.username = ''
  searchForm.isActive = ''
  handleSearch()
}

const handleAdd = () => {
  isEdit.value = false
  dialogVisible.value = true
}

const handleEdit = (row) => {
  if (row.username === 'root') {
    ElMessage.warning('root 用户是系统管理员，不能被修改')
    return
  }
  isEdit.value = true
  Object.assign(form, row)
  // 转换角色数据
  form.roleIds = row.roles?.map(r => r.roleId) || []
  dialogVisible.value = true
}

const handleDialogClosed = () => {
  formRef.value?.resetFields()
  Object.assign(form, {
    userId: null,
    username: '',
    password: '',
    realName: '',
    email: '',
    phone: '',
    roleIds: [],
    isActive: true
  })
}

const handleSubmit = async () => {
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  submitting.value = true
  try {
    let response
    if (isEdit.value) {
      response = await userApi.updateUser(form.userId, form)
      // 更新角色
      await userApi.assignRoles(form.userId, { roleIds: form.roleIds })
    } else {
      response = await userApi.createUser(form)
      // 创建后分配角色
      if (form.roleIds && form.roleIds.length > 0) {
        await userApi.assignRoles(response.data.userId, { roleIds: form.roleIds })
      }
    }
    ElMessage.success(isEdit.value ? '更新成功' : '创建成功')
    dialogVisible.value = false
    loadData()
  } catch (error) {
    console.error('操作失败:', error)
  } finally {
    submitting.value = false
  }
}

const handleToggleStatus = async (row) => {
  if (row.username === 'root') {
    ElMessage.warning('root 用户是系统管理员，不能被禁用')
    row.isActive = true
    return
  }
  try {
    await userApi.updateUser(row.userId, { isActive: row.isActive })
    ElMessage.success('状态更新成功')
  } catch (error) {
    console.error('更新状态失败:', error)
    row.isActive = !row.isActive
  }
}

const handleAssignRoles = async (row) => {
  if (row.username === 'root') {
    ElMessage.warning('root 用户拥有所有权限，无需分配角色')
    return
  }

  // 每次打开对话框时重新加载角色列表
  await loadRoles()

  currentUser.value = row
  selectedRoles.value = row.roles?.map(r => r.roleId) || []
  roleDialogVisible.value = true
}

const handleSaveRoles = async () => {
  savingRoles.value = true
  try {
    await userApi.assignRoles(currentUser.value.userId, {
      roleIds: selectedRoles.value
    })
    ElMessage.success('角色分配成功')
    roleDialogVisible.value = false
    loadData()
  } catch (error) {
    console.error('分配角色失败:', error)
  } finally {
    savingRoles.value = false
  }
}

const handleResetPassword = async (row) => {
  if (row.username === 'root') {
    ElMessage.warning('root 用户密码不能通过此方式重置')
    return
  }
  try {
    const { value: newPassword } = await ElMessageBox.prompt('请输入新密码', '重置密码', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      inputPattern: /^.{6,}$/,
      inputErrorMessage: '密码长度至少 6 个字符'
    })

    await userApi.resetPassword(row.userId, { newPassword })
    ElMessage.success('密码重置成功')
  } catch (error) {
    if (error !== 'cancel') {
      console.error('重置密码失败:', error)
    }
  }
}

const handleDelete = async (row) => {
  if (row.username === 'root') {
    ElMessage.error('root 用户是系统管理员，不能被删除')
    return
  }
  try {
    await ElMessageBox.confirm(`确定要删除用户"${row.username}"吗？`, '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    await userApi.deleteUser(row.userId)
    ElMessage.success('删除成功')
    loadData()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除失败:', error)
    }
  }
}
</script>

<style scoped>
.user-management {
  height: 100%;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.search-form {
  margin-bottom: 20px;
}
</style>
