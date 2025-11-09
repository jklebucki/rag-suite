import { apiHttpClient } from '@/shared/services/api/httpClients'

export async function downloadFile(filePath: string): Promise<void> {
  const response = await apiHttpClient.get(`/filedownload/${encodeURIComponent(filePath)}`, {
    responseType: 'blob',
  })

  const url = window.URL.createObjectURL(new Blob([response.data]))
  const link = document.createElement('a')
  link.href = url
  link.setAttribute('download', filePath.split('/').pop() || 'download')
  document.body.appendChild(link)
  link.click()
  link.remove()
  window.URL.revokeObjectURL(url)
}

export async function downloadFileWithConversion(
  filePath: string,
  forceConvert = false
) {
  const params = new URLSearchParams()
  params.append('forceConvert', forceConvert.toString())

  return apiHttpClient.get(`/filedownload/convert/${encodeURIComponent(filePath)}?${params.toString()}`, {
    responseType: 'blob',
  })
}

const fileService = {
  downloadFile,
  downloadFileWithConversion,
}

export default fileService
