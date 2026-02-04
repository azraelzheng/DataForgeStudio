<template>
  <div class="sql-editor-container">
    <div ref="editorRef" class="sql-editor"></div>
  </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount, watch } from 'vue'
import { EditorView } from '@codemirror/view'
import { EditorState } from '@codemirror/state'
import { sql } from '@codemirror/lang-sql'
import { oneDark } from '@codemirror/theme-one-dark'
import { keymap } from '@codemirror/view'
import { defaultKeymap, indentWithTab } from '@codemirror/commands'
import { searchKeymap, highlightSelectionMatches } from '@codemirror/search'
import { autocompletion } from '@codemirror/autocomplete'
import { linter, diagnosticCount } from '@codemirror/lint'
import { bracketMatching } from '@codemirror/language'
import { lineNumbers, highlightActiveLineGutter } from '@codemirror/view'
import { highlightSpecialChars, drawSelection, dropCursor, rectangularSelection } from '@codemirror/view'
import { highlightActiveLine } from '@codemirror/view'
import { format as SQLFormatter } from 'sql-formatter'

const props = defineProps({
  modelValue: {
    type: String,
    default: ''
  },
  theme: {
    type: String,
    default: 'light'
  },
  readOnly: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['update:modelValue', 'change'])

const editorRef = ref(null)
let editorView = null

// SQL 语法检查
const sqlLinter = linter((view) => {
  const diagnostics = []
  const content = view.state.doc.toString()

  // 基本语法检查
  if (content.trim()) {
    // 检查未闭合的括号
    const openParens = (content.match(/\(/g) || []).length
    const closeParens = (content.match(/\)/g) || []).length
    if (openParens !== closeParens) {
      diagnostics.push({
        from: 0,
        to: content.length,
        severity: 'warning',
        message: '括号不匹配'
      })
    }

    // 检查是否包含 SELECT（基本验证）
    const upperContent = content.toUpperCase()
    if (upperContent.includes('SELECT') && !upperContent.includes('FROM')) {
      diagnostics.push({
        from: 0,
        to: content.length,
        severity: 'warning',
        message: 'SELECT 语句通常需要 FROM 子句'
      })
    }
  }

  return diagnostics
})

// 创建编辑器扩展
const getExtensions = () => {
  const extensions = [
    lineNumbers(),
    highlightActiveLineGutter(),
    highlightSpecialChars(),
    highlightActiveLine(),
    drawSelection(),
    dropCursor(),
    bracketMatching(),
    rectangularSelection(),
    sql(),
    keymap.of([
      ...defaultKeymap,
      indentWithTab,
      { key: 'Shift-Alt-f', run: formatSQL }
    ]),
    searchKeymap.of(searchKeymap),
    highlightSelectionMatches(),
    autocompletion(),
    sqlLinter,
    EditorView.updateListener.of((update) => {
      if (update.docChanged) {
        const content = update.state.doc.toString()
        emit('update:modelValue', content)
        emit('change', content)
      }
    }),
    EditorView.theme({
      '&': {
        height: '100%',
        fontSize: '14px'
      },
      '.cm-scroller': {
        overflow: 'auto',
        height: '100%'
      },
      '.cm-content': {
        padding: '10px'
      },
      '.cm-focused': {
        outline: 'none'
      }
    })
  ]

  // 添加主题
  if (props.theme === 'dark') {
    extensions.push(oneDark)
  }

  // 只读模式
  if (props.readOnly) {
    extensions.push(EditorState.readOnly.of(true))
  }

  return extensions
}

// 格式化 SQL
const formatSQL = (view) => {
  const content = view.state.doc.toString()
  try {
    const formatted = SQLFormatter(content, {
      language: 'sql',
      tabWidth: 2,
      keywordCase: 'upper'
    })

    const transaction = view.state.update({
      changes: {
        from: 0,
        to: content.length,
        insert: formatted
      }
    })
    view.dispatch(transaction)
    return true
  } catch (error) {
    console.error('SQL 格式化失败:', error)
    return false
  }
}

// 初始化编辑器
onMounted(() => {
  if (!editorRef.value) return

  const state = EditorState.create({
    doc: props.modelValue,
    extensions: getExtensions()
  })

  editorView = new EditorView({
    state,
    parent: editorRef.value
  })
})

// 监听外部值变化
watch(() => props.modelValue, (newValue) => {
  if (editorView && newValue !== editorView.state.doc.toString()) {
    const transaction = editorView.state.update({
      changes: {
        from: 0,
        to: editorView.state.doc.length,
        insert: newValue
      }
    })
    editorView.dispatch(transaction)
  }
})

// 清理
onBeforeUnmount(() => {
  if (editorView) {
    editorView.destroy()
    editorView = null
  }
})

// 暴露格式化方法
defineExpose({
  formatSQL: () => {
    if (editorView) {
      formatSQL(editorView)
    }
  }
})
</script>

<style scoped>
.sql-editor-container {
  height: 100%;
  border: 1px solid #dcdfe6;
  border-radius: 4px;
  overflow: hidden;
}

.sql-editor {
  height: 100%;
}
</style>
