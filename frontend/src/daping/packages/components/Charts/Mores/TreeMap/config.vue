<template>
  <global-setting :optionData="optionData"></global-setting>
</template>

<script setup lang="ts">
import { PropType } from 'vue'
import { GlobalSetting } from '@/daping/components/Pages/ChartItemSetting'
import { option } from './config'
import { GlobalThemeJsonType } from '@/daping/settings/chartThemes/index'

defineProps({
  optionData: {
    type: Object as PropType<typeof option & GlobalThemeJsonType>,
    required: true
  }
})
</script>
