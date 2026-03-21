import { PublicConfigClass } from '@/daping/packages/public'
import { chartInitConfig } from '@/daping/settings/designSetting'
import { CreateComponentType } from '@/daping/packages/index.d'
import { Decorates06Config } from './index'
import cloneDeep from 'lodash/cloneDeep'

export const option = {
  colors: ['#1DC1F533', '#1DC1F5FF'],
  dataset: '我是标题',
  textColor: '#fff',
  textSize: 32
}

export default class Config extends PublicConfigClass implements CreateComponentType {
  public key = Decorates06Config.key
  public attr = { ...chartInitConfig, w: 500, h: 70, zIndex: 1 }
  public chartConfig = cloneDeep(Decorates06Config)
  public option = cloneDeep(option)
}
