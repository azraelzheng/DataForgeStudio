import jsPDF from 'jspdf'
import html2canvas from 'html2canvas'

/**
 * 导出报表为 PDF
 * @param {Object} options - 导出选项
 * @param {string} options.title - 报表标题
 * @param {Array} options.parameters - 查询参数 [{name, label, value}]
 * @param {Object} options.chart - 图表配置 {enableChart, chartType, chartRef}
 * @param {HTMLElement} options.tableElement - 表格 DOM 元素
 * @param {string} options.filename - 文件名（不含扩展名）
 */
export async function exportToPdf(options) {
  const {
    title = '报表',
    parameters = [],
    chart = null,
    tableElement = null,
    filename = `报表_${Date.now()}`
  } = options

  // 创建 PDF 文档 (A4 纵向, 单位 mm)
  const pdf = new jsPDF({
    orientation: 'portrait',
    unit: 'mm',
    format: 'a4'
  })

  // 页面尺寸
  const pageWidth = pdf.internal.pageSize.getWidth()
  const pageHeight = pdf.internal.pageSize.getHeight()
  const margin = 15
  const contentWidth = pageWidth - 2 * margin
  let yPosition = margin

  // 字体设置
  // 注意: jsPDF 默认字体不支持中文字符显示
  // 这是一个已知的浏览器环境限制
  // 可能的解决方案:
  // 1. 使用自定义中文字体文件 (需要加载 .ttf 文件，增加包大小)
  // 2. 使用 canvas 将中文文本渲染为图片后嵌入 PDF
  // 3. 使用后端生成 PDF (推荐用于生产环境)
  // 当前实现: 使用 html2canvas 将整个表格/图表转为图片，中文可以正常显示
  pdf.setFont('helvetica')
  pdf.setFontSize(16)

  // 1. 标题
  pdf.text(title, margin, yPosition)
  yPosition += 10

  // 2. 导出时间
  pdf.setFontSize(10)
  pdf.text(`导出时间: ${formatDateTime(new Date())}`, margin, yPosition)
  yPosition += 10

  // 3. 查询参数
  if (parameters.length > 0) {
    pdf.setFontSize(12)
    pdf.text('查询参数:', margin, yPosition)
    yPosition += 7

    pdf.setFontSize(10)
    parameters.forEach(param => {
      const paramText = `${param.label}: ${param.value || '-'}`
      pdf.text(paramText, margin + 5, yPosition)
      yPosition += 5
    })
    yPosition += 5
  }

  // 4. 表格数据
  if (tableElement) {
    // 检查是否需要新页面
    if (yPosition + 50 > pageHeight) {
      pdf.addPage()
      yPosition = margin
    }

    // 使用 html2canvas 捕获表格
    const tableCanvas = await html2canvas(tableElement, {
      scale: 2, // 提高清晰度
      useCORS: true,
      logging: false
    })

    const tableImgData = tableCanvas.toDataURL('image/png')

    // 计算图片尺寸（保持比例）
    const imgWidth = contentWidth
    const imgHeight = (tableCanvas.height * contentWidth) / tableCanvas.width

    // 检查是否需要分页
    if (yPosition + imgHeight > pageHeight - margin) {
      const remainingHeight = pageHeight - margin - yPosition
      const maxHeight = pageHeight - 2 * margin

      if (imgHeight > maxHeight) {
        // 表格太高，需要分页
        // 简化处理：如果表格太大，缩小到一页
        const scaleFactor = maxHeight / imgHeight
        const scaledWidth = imgWidth * scaleFactor
        const scaledHeight = maxHeight

        const centerX = (pageWidth - scaledWidth) / 2
        pdf.addImage(tableImgData, 'PNG', centerX, margin, scaledWidth, scaledHeight)
      } else {
        // 添加新页面
        pdf.addPage()
        pdf.addImage(tableImgData, 'PNG', margin, margin, imgWidth, imgHeight)
      }
    } else {
      pdf.addImage(tableImgData, 'PNG', margin, yPosition, imgWidth, imgHeight)
      yPosition += imgHeight + 10
    }
  }

  // 5. 图表
  if (chart && chart.enableChart && chart.chartRef) {
    // 检查是否需要新页面
    if (yPosition + 100 > pageHeight) {
      pdf.addPage()
      yPosition = margin
    }

    try {
      // 获取图表 DOM
      const chartElement = chart.chartRef
      if (chartElement) {
        const chartCanvas = await html2canvas(chartElement, {
          scale: 2,
          useCORS: true,
          logging: false,
          backgroundColor: '#ffffff'
        })

        const chartImgData = chartCanvas.toDataURL('image/png')

        // 计算图片尺寸
        const imgWidth = contentWidth
        const imgHeight = (chartCanvas.height * contentWidth) / chartCanvas.width
        const maxHeight = pageHeight - yPosition - margin

        let finalHeight = imgHeight
        let finalWidth = imgWidth

        // 如果图表太大，等比缩小
        if (imgHeight > maxHeight) {
          finalHeight = maxHeight
          finalWidth = (chartCanvas.width * maxHeight) / chartCanvas.height
        }

        const centerX = (pageWidth - finalWidth) / 2
        pdf.addImage(chartImgData, 'PNG', centerX, yPosition, finalWidth, finalHeight)
      }
    } catch (error) {
      console.warn('导出图表失败:', error)
    }
  }

  // 6. 页码
  const totalPages = pdf.internal.getNumberOfPages()
  for (let i = 1; i <= totalPages; i++) {
    pdf.setPage(i)
    pdf.setFontSize(9)
    pdf.text(`第 ${i} / ${totalPages} 页`, pageWidth / 2, pageHeight - 10, { align: 'center' })
  }

  // 保存 PDF
  pdf.save(`${filename}.pdf`)
}

/**
 * 格式化日期时间
 */
function formatDateTime(date) {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  const hours = String(date.getHours()).padStart(2, '0')
  const minutes = String(date.getMinutes()).padStart(2, '0')
  const seconds = String(date.getSeconds()).padStart(2, '0')
  return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`
}
