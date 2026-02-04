import request from './request'

/**
 * 获取数据源的表结构
 * @param {number} dataSourceId - 数据源 ID
 */
export function getTableStructure(dataSourceId) {
  return request({
    url: `/datasources/${dataSourceId}/tables`,
    method: 'get'
  })
}
