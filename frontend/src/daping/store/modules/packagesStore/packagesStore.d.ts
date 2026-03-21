import { PackagesType, ConfigType } from '@/daping/packages/index.d'

export { ConfigType }

export { PackagesType }
export interface PackagesStoreType {
  packagesList: PackagesType,
  newPhoto?: ConfigType
}
