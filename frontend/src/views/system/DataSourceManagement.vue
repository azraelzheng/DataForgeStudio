<template>
  <div class="datasource-management">
    <el-card class="flex-card">
      <template #header>
        <div class="card-header">
          <span>数据源管理</span>
          <el-button type="primary" @click="handleAdd">
            <el-icon><Plus /></el-icon>
            新增数据源
          </el-button>
        </div>
      </template>

      <!-- 搜索表单 -->
      <div class="search-grid">
        <div class="search-item">
          <label class="search-label">数据源名称</label>
          <el-input v-model="searchForm.dataSourceName" placeholder="请输入数据源名称" clearable />
        </div>
        <div class="search-item">
          <label class="search-label">数据库类型</label>
          <el-select v-model="searchForm.dbType" placeholder="请选择数据库类型" clearable>
            <el-option label="全部" value="" />
            <el-option label="SQL Server" value="SqlServer" />
            <el-option label="MySQL" value="MySql" />
            <el-option label="Oracle" value="Oracle" />
            <el-option label="PostgreSQL" value="PostgreSQL" />
          </el-select>
        </div>
        <div class="search-actions">
          <el-button type="primary" @click="handleSearch">
            <el-icon><Search /></el-icon>
            查询
          </el-button>
          <el-button @click="handleReset">重置</el-button>
        </div>
      </div>

      <!-- 数据源表格 -->
      <div class="table-wrapper" ref="tableWrapper">
        <template v-if="tableData && tableData.length > 0">
          <el-table :data="tableData" v-loading="loading" border stripe :height="tableHeight">
          <el-table-column prop="dataSourceName" label="数据源名称" width="200" />
          <el-table-column prop="dbType" label="数据库类型" width="120">
            <template #default="{ row }">
              <el-tag>{{ getDbTypeText(row.dbType) }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="serverAddress" label="服务器地址" width="200" />
          <el-table-column prop="databaseName" label="数据库名" width="150" />
          <el-table-column prop="isActive" label="状态" width="80">
            <template #default="{ row }">
              <el-tag :type="row.isActive ? 'success' : 'danger'">
                {{ row.isActive ? '启用' : '停用' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="创建时间" width="180">
            <template #default="{ row }">
              {{ formatDateTime(row.createdTime) }}
            </template>
          </el-table-column>
          <el-table-column label="操作" width="300" fixed="right">
            <template #default="{ row }">
              <el-button type="primary" link size="small" @click="handleTest(row)" :loading="row.testing">
                <el-icon><Connection /></el-icon>
                测试连接
              </el-button>
              <el-button type="primary" link size="small" @click="handleEdit(row)">
                <el-icon><Edit /></el-icon>
                编辑
              </el-button>
              <el-button :type="row.isActive ? 'warning' : 'success'" link size="small" @click="handleToggleActive(row)" :loading="row.toggling">
                <el-icon><Switch /></el-icon>
                {{ row.isActive ? '停用' : '启用' }}
              </el-button>
              <el-button type="danger" link size="small" @click="handleDelete(row)">
                <el-icon><Delete /></el-icon>
                删除
              </el-button>
            </template>
          </el-table-column>
        </el-table>
        </template>
        <el-empty v-else-if="!loading" description="暂无数据源，请添加数据源" />
      </div>

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

    <!-- 数据源编辑对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="isEdit ? '编辑数据源' : '新增数据源'"
      width="700px"
      @closed="handleDialogClosed"
    >
      <el-form :model="form" :rules="rules" ref="formRef" label-width="120px">
        <el-form-item label="数据源名称" prop="dataSourceName">
          <el-input v-model="form.dataSourceName" placeholder="请输入数据源名称" />
        </el-form-item>
        <el-form-item label="数据库类型" prop="dbType">
          <el-select v-model="form.dbType" placeholder="请选择数据库类型">
            <el-option label="SQL Server" value="SqlServer" />
            <el-option label="MySQL" value="MySql" />
            <el-option label="Oracle" value="Oracle" />
            <el-option label="PostgreSQL" value="PostgreSQL" />
          </el-select>
        </el-form-item>
        <el-form-item label="服务器地址" prop="server">
          <el-input v-model="form.server" placeholder="例如: localhost 或 192.168.1.100" />
        </el-form-item>
        <el-form-item label="端口" prop="port">
          <el-input-number v-model="form.port" :min="1" :max="65535" />
        </el-form-item>
        <el-form-item label="用户名" prop="username">
          <el-input v-model="form.username" placeholder="请输入数据库用户名" />
        </el-form-item>
        <el-form-item label="密码" prop="password">
          <el-input v-model="form.password" type="password" show-password placeholder="请输入数据库密码" />
        </el-form-item>
        <el-form-item label="数据库名" prop="database">
          <el-select
            v-model="form.database"
            placeholder="测试连接后可选择数据库"
            filterable
            allow-create
            clearable
          >
            <el-option
              v-for="db in databaseList"
              :key="db"
              :label="db"
              :value="db"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="描述">
          <el-input v-model="form.description" type="textarea" :rows="3" placeholder="请输入描述" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button @click="handleTestConnection" :loading="testing" :disabled="!canTestConnection">
          <el-icon><Connection /></el-icon>
          测试连接
        </el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">
          确定
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, onUnmounted, nextTick, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { dataSourceApi } from '../../api/request'

const formRef = ref()
const tableWrapper = ref(null)
const tableHeight = ref(null)
const loading = ref(false)
const dialogVisible = ref(false)
const isEdit = ref(false)
const submitting = ref(false)
const testing = ref(false)
const databaseList = ref([])

const tableData = ref([])

const searchForm = reactive({
  dataSourceName: '',
  dbType: ''
})

const form = reactive({
  dataSourceId: null,
  dataSourceName: '',
  dbType: 'SqlServer',
  server: 'localhost',
  port: 1433,
  database: '',
  username: '',
  password: '',
  description: ''
})

// 可以测试连接的条件：服务器地址、用户名、密码已填写
const canTestConnection = computed(() => {
  return form.server && form.username && form.password
})

const rules = {
  dataSourceName: [{ required: true, message: '请输入数据源名称', trigger: 'blur' }],
  dbType: [{ required: true, message: '请选择数据库类型', trigger: 'change' }],
  server: [{ required: true, message: '请输入服务器地址', trigger: 'blur' }],
  port: [{ required: true, message: '请输入端口', trigger: 'blur' }],
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

const pagination = reactive({
  page: 1,
  pageSize: 20,
  total: 0
})

// 根据数据库类型设置默认端口
const defaultPorts = {
  SqlServer: 1433,
  MySql: 3306,
  Oracle: 1521,
  PostgreSQL: 5432
}

// 更新表格高度
const updateTableHeight = () => {
  if (tableWrapper.value) {
    tableHeight.value = tableWrapper.value.clientHeight
  }
}

onMounted(() => {
  loadData()
  nextTick(updateTableHeight)
  window.addEventListener('resize', updateTableHeight)
})

onUnmounted(() => {
  window.removeEventListener('resize', updateTableHeight)
})

const loadData = async () => {
  loading.value = true
  try {
    const res = await dataSourceApi.getDataSources({
      page: pagination.page,
      pageSize: pagination.pageSize,
      ...searchForm
    })
    if (res.success) {
      const data = res.data
      tableData.value = data.Items || data.items || []
      pagination.total = data.TotalCount || data.total || 0
    }
  } catch {
    // 加载失败
  } finally {
    loading.value = false
  }
}

const handleSearch = () => {
  pagination.page = 1
  loadData()
}

const handleReset = () => {
  searchForm.dataSourceName = ''
  searchForm.dbType = ''
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
    dataSourceId: null,
    dataSourceName: '',
    dbType: 'SqlServer',
    server: 'localhost',
    port: 1433,
    database: '',
    username: '',
    password: '',
    description: ''
  })
}

const handleTestConnection = async () => {
  const fieldsToValidate = ['dbType', 'server', 'port', 'username', 'password']
  let allValid = true

  for (const field of fieldsToValidate) {
    try {
      await formRef.value.validateField(field)
    } catch {
      allValid = false
    }
  }

  if (!allValid) return

  testing.value = true
  try {
    const res = await dataSourceApi.testConnectionBeforeSave(form)
    if (res.success) {
      ElMessage.success('连接测试成功，正在获取数据库列表...')
      await fetchDatabases()
    }
  } catch {
    // 测试失败
  } finally {
    testing.value = false
  }
}

const fetchDatabases = async () => {
  try {
    const res = await dataSourceApi.getDatabases(form)
    if (res.success && res.data) {
      databaseList.value = res.data
      if (!form.database && res.data.length > 0) {
        form.database = res.data[0]
      }
      ElMessage.success(`获取到 ${res.data.length} 个数据库`)
    }
  } catch {
    // 获取失败
  }
}

const handleTest = async (row) => {
  row.testing = true
  try {
    const res = await dataSourceApi.testConnection(row.dataSourceId)
    if (res.success) {
      ElMessage.success('连接测试成功')
    }
  } catch {
    // 测试失败
  } finally {
    row.testing = false
  }
}

const handleSubmit = async () => {
  const valid = await formRef.value.validate().catch(() => false)
  if (!valid) return

  submitting.value = true
  try {
    if (isEdit.value) {
      await dataSourceApi.updateDataSource(form.dataSourceId, form)
    } else {
      await dataSourceApi.createDataSource(form)
    }
    ElMessage.success(isEdit.value ? '更新成功' : '创建成功')
    dialogVisible.value = false
    loadData()
  } catch {
    // 操作失败
  } finally {
    submitting.value = false
  }
}

const handleDelete = async (row) => {
  try {
    await ElMessageBox.confirm(`确定要删除数据源"${row.dataSourceName}"吗？`, '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    await dataSourceApi.deleteDataSource(row.dataSourceId)
    ElMessage.success('删除成功')
    loadData()
  } catch (error) {
    if (error !== 'cancel') {
      // 删除失败
    }
  }
}

const handleToggleActive = async (row) => {
  row.toggling = true
  try {
    const res = await dataSourceApi.toggleActive(row.dataSourceId)
    if (res.success) {
      ElMessage.success(res.message || (row.isActive ? '已停用' : '已启用'))
      loadData()
    }
  } catch {
    // 操作失败
  } finally {
    row.toggling = false
  }
}

const getDbTypeText = (type) => {
  const map = {
    'SqlServer': 'SQL Server',
    'MySql': 'MySQL',
    'Oracle': 'Oracle',
    'PostgreSQL': 'PostgreSQL'
  }
  return map[type] || type
}

const formatDateTime = (date) => {
  if (!date) return '-'
  return new Date(date).toLocaleString('zh-CN')
}
</script>

<style scoped>
.datasource-management {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.flex-card {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.flex-card :deep(.el-card__header) {
  flex-shrink: 0;
}

.flex-card :deep(.el-card__body) {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.search-grid {
  flex-shrink: 0;
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

.table-wrapper {
  flex: 1;
  overflow: hidden;
  min-height: 100px;
}

.el-pagination {
  flex-shrink: 0;
}
</style>
