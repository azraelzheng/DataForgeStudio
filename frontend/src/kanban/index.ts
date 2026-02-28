/**
 * Kanban Module - 看板视图模块
 * @module kanban
 */

// Components
export { default as KanbanBoard } from './components/KanbanBoard.vue'
export { default as KanbanColumn } from './components/KanbanColumn.vue'
export { default as KanbanCard } from './components/KanbanCard.vue'
export { default as CardDetail } from './components/CardDetail.vue'
export { default as CardForm } from './components/CardForm.vue'
export { default as SwimLaneView } from './components/SwimLaneView.vue'

// Views
export { default as KanbanView } from './views/KanbanView.vue'

// Composables
export { useDragDrop } from './composables/useDragDrop'
export { useKanbanState } from './composables/useKanbanState'
export { useKanbanFilter } from './composables/useKanbanFilter'

// Types
export * from './types/kanban'
