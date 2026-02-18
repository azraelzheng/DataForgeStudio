<template>
  <div class="role-management">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>权限组管理</span>
          <el-button type="primary" @click="handleAdd">
            <el-icon><Plus /></el-icon>
            新增权限组
          </el-button>
        </div>
      </template>

      <!-- 搜索表单 -->
      <div class="search-grid">
        <div class="search-item">
          <label class="search-label">角色名称</label>
          <el-input v-model="searchForm.roleName" placeholder="请输入角色名称" clearable />
        </div>
        <div class="search-actions">
          <el-button type="primary" @click="handleSearch">
            <el-icon><Search /></el-icon>
            查询
          </el-button>
          <el-button @click="handleReset">重置</el-button>
        </div>
      </div>

      <!-- 角色表格 -->
      <template v-if="tableData && tableData.length > 0">
        <el-table :data="tableData" v-loading="loading" border stripe>
          <el-table-column prop="roleName" label="角色名称" width="200" />
          <el-table-column prop="description" label="描述" min-width="300" />
          <el-table-column label="系统角色" width="100" align="center">
            <template #default="{ row }">
              <el-tag v-if="row.isSystem" type="danger" size="small">系统</el-tag>
              <el-tag v-else type="success" size="small">自定义</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="userCount" label="用户数" width="100" align="center" />
          <el-table-column prop="createdTime" label="创建时间" width="180" />
          <el-table-column label="操作" width="260" fixed="right">
            <template #default="{ row }">
              <el-button type="primary" link size="small" @click="handleEdit(row)" :disabled="row.isSystem">
                <el-icon><Edit /></el-icon>
                编辑
              </el-button>
              <el-button type="primary" link size="small" @click="handleAssignPermissions(row)">
                <el-icon><Key /></el-icon>
                配置权限
              </el-button>
              <el-button type="danger" link size="small" @click="handleDelete(row)" :disabled="row.isSystem">
                <el-icon><Delete /></el-icon>
                删除
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </template>
      <el-empty v-else-if="!loading" description="暂无角色数据" />

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

    <!-- 角色编辑对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="isEdit ? '编辑角色' : '新增角色'"
      width="600px"
      @closed="handleDialogClosed"
    >
      <el-form :model="form" :rules="rules" ref="formRef" label-width="100px">
        <el-form-item label="角色名称" prop="roleName">
          <el-input v-model="form.roleName" placeholder="请输入角色名称" />
        </el-form-item>
        <el-form-item label="描述" prop="description">
          <el-input v-model="form.description" type="textarea" :rows="3" placeholder="请输入描述" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">
          确定
        </el-button>
      </template>
    </el-dialog>

    <!-- 权限配置对话框 -->
    <el-dialog v-model="permissionDialogVisible" title="配置权限" width="800px">
      <el-tree
        ref="permissionTreeRef"
        :data="permissionTree"
        :props="{ children: 'children', label: 'label' }"
        node-key="key"
        show-checkbox
        default-expand-all
      />
      <template #footer>
        <el-button @click="permissionDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSavePermissions" :loading="savingPermissions">
          确定
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, onActivated, nextTick } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { roleApi } from '../../api/request'

const formRef = ref()
const permissionTreeRef = ref()
const loading = ref(false)
const dialogVisible = ref(false)
const permissionDialogVisible = ref(false)
const isEdit = ref(false)
const submitting = ref(false)
const savingPermissions = ref(false)

const tableData = ref([])
const currentRole = ref(null)

const searchForm = reactive({
  roleName: ''
})

const form = reactive({
  roleId: null,
  roleName: '',
  description: ''
})

const rules = {
  roleName: [{ required: true, message: '请输入角色名称', trigger: 'blur' }],
  description: [{ required: true, message: '请输入描述', trigger: 'blur' }]
}

// 权限树 - 与后端权限代码一致
const permissionTree = ref([
  {
    key: 'user',
    label: '用户管理',
    children: [
      { key: 'user:view', label: '查看用户' },
      { key: 'user:create', label: '创建用户' },
      { key: 'user:edit', label: '编辑用户' },
      { key: 'user:delete', label: '删除用户' },
      { key: 'user:resetPassword', label: '重置密码' }
    ]
  },
  {
    key: 'role',
    label: '角色管理',
    children: [
      { key: 'role:view', label: '查看角色' },
      { key: 'role:create', label: '创建角色' },
      { key: 'role:edit', label: '编辑角色' },
      { key: 'role:delete', label: '删除角色' },
      { key: 'role:assignPermissions', label: '分配权限' }
    ]
  },
  {
    key: 'report',
    label: '报表管理',
    children: [
      { key: 'report:view', label: '查看报表' },
      { key: 'report:create', label: '创建报表' },
      { key: 'report:edit', label: '编辑报表' },
      { key: 'report:delete', label: '删除报表' },
      { key: 'report:design', label: '报表设计' },
      { key: 'report:execute', label: '执行报表' },
      { key: 'report:export', label: '导出报表' }
    ]
  },
  {
    key: 'datasource',
    label: '数据源管理',
    children: [
      { key: 'datasource:view', label: '查看数据源' },
      { key: 'datasource:create', label: '创建数据源' },
      { key: 'datasource:edit', label: '编辑数据源' },
      { key: 'datasource:delete', label: '删除数据源' },
      { key: 'datasource:test', label: '测试连接' }
    ]
  },
  {
    key: 'log',
    label: '日志管理',
    children: [
      { key: 'log:view', label: '查看日志' },
      { key: 'log:clear', label: '清空日志' },
      { key: 'log:export', label: '导出日志' }
    ]
  },
  {
    key: 'backup',
    label: '备份管理',
    children: [
      { key: 'backup:view', label: '查看备份' },
      { key: 'backup:create', label: '创建备份' },
      { key: 'backup:restore', label: '恢复备份' },
      { key: 'backup:delete', label: '删除备份' }
    ]
  },
  {
    key: 'license',
    label: '许可管理',
    children: [
      { key: 'license:view', label: '查看许可' },
      { key: 'license:activate', label: '激活许可' }
    ]
  },
  {
    key: 'system',
    label: '系统设置',
    children: [
      { key: 'system:view', label: '查看系统设置' },
      { key: 'system:edit', label: '编辑系统设置' }
    ]
  }
])

const pagination = reactive({
  page: 1,
  pageSize: 20,
  total: 0
})

onMounted(() => {
  loadData()
})

// 当组件被激活时（从其他页面返回），重新加载数据以确保显示最新信息
onActivated(() => {
  loadData()
})

const loadData = async () => {
  loading.value = true
  try {
    const res = await roleApi.getRoles({
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

const handleSearch = () => {
  pagination.page = 1
  loadData()
}

const handleReset = () => {
  searchForm.roleName = ''
  handleSearch()
}

const handleAdd = () => {
  isEdit.value = false
  dialogVisible.value = true
}

const handleEdit = (row) => {
  isEdit.value = true
  Object.assign(form, row)
  dialogVisible.value = true
}

const handleDialogClosed = () => {
  formRef.value?.resetFields()
  Object.assign(form, {
    roleId: null,
    roleName: '',
    description: ''
  })
}

const handleSubmit = async () => {
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  submitting.value = true
  try {
    if (isEdit.value) {
      await roleApi.updateRole(form.roleId, form)
    } else {
      await roleApi.createRole(form)
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

const handleAssignPermissions = async (row) => {
  currentRole.value = row
  permissionDialogVisible.value = true

  // 等待对话框渲染完成
  await nextTick()

  // 如果角色没有权限信息，先从API获取
  if (!row.permissions || row.permissions.length === 0) {
    try {
      const res = await roleApi.getRoles({ page: 1, pageSize: 1000 })
      if (res.success) {
        const roles = res.data.Items || res.data.items || []
        const fullRole = roles.find(r => r.roleId === row.roleId)
        if (fullRole && fullRole.permissions) {
          row.permissions = fullRole.permissions
        }
      }
    } catch (error) {
      console.error('获取角色权限失败:', error)
    }
  }

  // 再次等待确保树组件已渲染
  await nextTick()

  // 设置已选中的权限
  const checkedKeys = row.permissions || []
  if (permissionTreeRef.value) {
    permissionTreeRef.value.setCheckedKeys(checkedKeys)
  }
}

const handleSavePermissions = async () => {
  savingPermissions.value = true
  try {
    // 只获取叶子节点的选中状态（实际权限）
    const checkedKeys = permissionTreeRef.value.getCheckedKeys()
    // 过滤掉父节点（包含冒号的key，如 "report", "user"）
    const permissionKeys = checkedKeys.filter(key => key.includes(':'))

    await roleApi.assignPermissions(currentRole.value.roleId, {
      permissions: permissionKeys
    })
    ElMessage.success('权限配置成功')
    permissionDialogVisible.value = false

    // 刷新列表数据以更新权限显示
    loadData()
  } catch (error) {
    console.error('配置权限失败:', error)
  } finally {
    savingPermissions.value = false
  }
}

const handleDelete = async (row) => {
  try {
    await ElMessageBox.confirm(`确定要删除角色"${row.roleName}"吗？`, '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    await roleApi.deleteRole(row.roleId)
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
.role-management {
  height: 100%;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.search-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 12px 16px;
  margin-bottom: 16px;
  align-items: end;
}

.search-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.search-label {
  font-size: 14px;
  color: #606266;
  font-weight: 500;
}

.search-item :deep(.el-input),
.search-item :deep(.el-select) {
  width: 100%;
}

.search-actions {
  display: flex;
  gap: 8px;
  align-items: flex-end;
  height: 32px;
}
</style>
