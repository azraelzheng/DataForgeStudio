<!-- frontend/src/components/DirectorySelector.vue -->
<template>
  <div class="directory-selector">
    <el-tree-select
      ref="treeSelectRef"
      v-model="selectedPath"
      :data="treeData"
      :props="treeProps"
      :load="loadDirectories"
      lazy
      clearable
      filterable
      :placeholder="placeholder"
      check-strictly
      :render-after-expand="false"
      @node-click="handleNodeClick"
      @clear="handleClear"
    >
      <template #default="{ data }">
        <span class="tree-node">
          <el-icon v-if="data.isDrive" style="margin-right: 4px;"><FolderOpened /></el-icon>
          <el-icon v-else style="margin-right: 4px;"><Folder /></el-icon>
          <span>{{ data.name }}</span>
        </span>
      </template>
    </el-tree-select>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import { Folder, FolderOpened } from '@element-plus/icons-vue'
import { systemApi } from '../api/request'
import { ElMessage } from 'element-plus'

const props = defineProps({
  modelValue: {
    type: String,
    default: ''
  },
  placeholder: {
    type: String,
    default: '选择备份目录'
  }
})

const emit = defineEmits(['update:modelValue', 'change'])

const treeSelectRef = ref(null)
const selectedPath = ref(props.modelValue)
const treeData = ref([])

const treeProps = {
  label: 'name',
  value: 'path',
  children: 'children',
  isLeaf: (data) => !data.hasChildren
}

// 监听外部 v-model 变化
watch(() => props.modelValue, (newVal) => {
  selectedPath.value = newVal
})

// 监听内部选择变化
watch(selectedPath, (newVal) => {
  emit('update:modelValue', newVal)
})

// 懒加载目录
const loadDirectories = async (node, resolve) => {
  try {
    const path = node.level === 0 ? null : node.data?.path
    const res = await systemApi.getDirectories(path)

    if (res.success && res.data) {
      // 如果是根节点加载驱动器，直接返回
      if (node.level === 0) {
        treeData.value = res.data
      }
      resolve(res.data)
    } else {
      resolve([])
    }
  } catch (error) {
    console.error('加载目录失败:', error)
    ElMessage.warning('加载目录失败，请检查服务器权限')
    resolve([])
  }
}

// 节点点击
const handleNodeClick = (data) => {
  selectedPath.value = data.path
  emit('change', data.path)
}

// 清除选择
const handleClear = () => {
  selectedPath.value = ''
  emit('change', '')
}

// 初始加载根目录（驱动器列表）
const initTree = async () => {
  try {
    const res = await systemApi.getDirectories(null)
    if (res.success && res.data) {
      treeData.value = res.data
    }
  } catch (error) {
    console.error('初始化目录树失败:', error)
  }
}

// 组件挂载时初始化
initTree()

// 暴露刷新方法
defineExpose({
  refresh: initTree
})
</script>

<style scoped>
.directory-selector {
  width: 100%;
}

.tree-node {
  display: flex;
  align-items: center;
}

.directory-selector :deep(.el-tree-select) {
  width: 100%;
}
</style>
