// Minimal i18n setup for daping module
import { createI18n } from 'vue-i18n'
import { LangEnum } from '@/daping/enums/styleEnum'

// 语言列表
export const langList = [
  {
    label: '中文',
    key: LangEnum.ZH
  },
  {
    label: 'English',
    key: LangEnum.EN
  }
]

// 简单的翻译消息
const messages = {
  [LangEnum.ZH]: {},
  [LangEnum.EN]: {}
}

const i18n = createI18n({
  locale: LangEnum.ZH,
  fallbackLocale: LangEnum.ZH,
  messages
})

export default i18n
