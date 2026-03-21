import { PublicConfigClass } from '@/daping/packages/public'
import { CreateComponentType } from '@/daping/packages/index.d'
import { chartInitConfig } from '@/daping/settings/designSetting'
import { ThreeEarth01Config } from './index'
import dataJson from './data.json'
import cloneDeep from 'lodash/cloneDeep'

export const option = {
  dataset: dataJson
}

export default class Config extends PublicConfigClass implements CreateComponentType {
  public key = ThreeEarth01Config.key
  public attr = { ...chartInitConfig, w: 800, h: 800, zIndex: -1 }
  public chartConfig = cloneDeep(ThreeEarth01Config)
  public option = cloneDeep(option)
}
