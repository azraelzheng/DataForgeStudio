import { http } from '@/daping/api/http'
import { httpErrorHandle } from '@/daping/utils'
import { RequestHttpEnum } from '@/daping/enums/httpEnum'
import { ProjectItem, ProjectDetail } from './project'

// * 项目列表
export const projectListApi = async (data: object) => {
  try {
    const res = await http(RequestHttpEnum.POST)<{
      list: ProjectItem[],
      count: number
    }>('/api/daping/projects/list', data)
    return res
  } catch {
    httpErrorHandle()
  }
}

// * 新增项目
export const createProjectApi = async (data: object) => {
  try {
    const res = await http(RequestHttpEnum.POST)<number>('/api/daping/projects', data)
    return res
  } catch {
    httpErrorHandle()
  }
}

// * 获取项目
export const fetchProjectApi = async (id: number) => {
  try {
    const res = await http(RequestHttpEnum.GET)<ProjectDetail>(`/api/daping/projects/${id}`)
    return res
  } catch {
    httpErrorHandle()
  }
}

// * 保存项目
export const saveProjectApi = async (id: number, data: object) => {
  try {
    const res = await http(RequestHttpEnum.PUT)(`/api/daping/projects/${id}`, data)
    return res
  } catch {
    httpErrorHandle()
  }
}

// * 修改项目基础信息
export const updateProjectApi = async (id: number, data: object) => {
  try {
    const res = await http(RequestHttpEnum.PUT)(`/api/daping/projects/${id}`, data)
    return res
  } catch {
    httpErrorHandle()
  }
}

// * 删除项目
export const deleteProjectApi = async (id: number) => {
  try {
    const res = await http(RequestHttpEnum.DELETE)(`/api/daping/projects/${id}`)
    return res
  } catch {
    httpErrorHandle()
  }
}

// * 发布项目
export const publishProjectApi = async (id: number) => {
  try {
    const res = await http(RequestHttpEnum.POST)(`/api/daping/projects/${id}/publish`)
    return res
  } catch {
    httpErrorHandle()
  }
}

// * 取消发布
export const unpublishProjectApi = async (id: number) => {
  try {
    const res = await http(RequestHttpEnum.POST)(`/api/daping/projects/${id}/unpublish`)
    return res
  } catch {
    httpErrorHandle()
  }
}
