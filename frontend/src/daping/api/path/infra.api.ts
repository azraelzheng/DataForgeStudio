import { http } from '@/daping/api/http'
import { httpErrorHandle } from '@/daping/utils'
import { RequestHttpEnum, ModuleTypeEnum, ContentTypeEnum } from '@/daping/enums/httpEnum'

// * 上传文件
export const uploadFile = async (data: object) => {
  try {
    const res = await http(RequestHttpEnum.POST)<string>(`${ModuleTypeEnum.INFRA}/file/upload`, data, ContentTypeEnum.FORM_DATA)
    return res
  } catch {
    httpErrorHandle()
  }
}
